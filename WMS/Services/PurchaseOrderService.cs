using WMS.Data.Repositories;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WMS.Services
{
    /// <summary>
    /// Service implementation untuk Purchase Order management
    /// "The Opening Act" - mengelola pemesanan barang ke supplier
    /// </summary>
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly ISupplierRepository _supplierRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IEmailService _emailService;

        public PurchaseOrderService(
            IPurchaseOrderRepository purchaseOrderRepository,
            ISupplierRepository supplierRepository,
            IItemRepository itemRepository,
            IEmailService emailService)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
            _supplierRepository = supplierRepository;
            _itemRepository = itemRepository;
            _emailService = emailService;
        }

        #region Basic CRUD Operations

        public async Task<IEnumerable<PurchaseOrder>> GetAllPurchaseOrdersAsync()
        {
            return await _purchaseOrderRepository.GetAllWithDetailsAsync();
        }

        public async Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int id)
        {
            return await _purchaseOrderRepository.GetByIdWithDetailsAsync(id);
        }

        public async Task<PurchaseOrder> CreatePurchaseOrderAsync(PurchaseOrderViewModel viewModel)
        {
            // Validate input
            if (!await ValidatePurchaseOrderAsync(viewModel))
                throw new InvalidOperationException("Purchase Order validation failed");

            // Create PurchaseOrder entity
            var purchaseOrder = new PurchaseOrder
            {
                PONumber = await GenerateNextPONumberAsync(),
                SupplierId = viewModel.SupplierId,
                OrderDate = viewModel.OrderDate,
                ExpectedDeliveryDate = viewModel.ExpectedDeliveryDate,
                Notes = viewModel.Notes,
                Status = Constants.PO_STATUS_DRAFT,
                CreatedDate = DateTime.Now
            };

            // Create details
            foreach (var detailVM in viewModel.Details)
            {
                var detail = new PurchaseOrderDetail
                {
                    ItemId = detailVM.ItemId,
                    Quantity = detailVM.Quantity,
                    UnitPrice = detailVM.UnitPrice,
                    Notes = detailVM.Notes,
                    CreatedDate = DateTime.Now
                };
                
                detail.CalculateTotalPrice();
                purchaseOrder.PurchaseOrderDetails.Add(detail);
            }

            // Calculate total amount
            purchaseOrder.TotalAmount = purchaseOrder.PurchaseOrderDetails.Sum(d => d.TotalPrice);

            // Save to database
            return await _purchaseOrderRepository.CreateWithDetailsAsync(purchaseOrder);
        }

        public async Task<PurchaseOrder> UpdatePurchaseOrderAsync(int id, PurchaseOrderViewModel viewModel)
        {
            var existingPO = await _purchaseOrderRepository.GetByIdWithDetailsAsync(id);
            if (existingPO == null)
                throw new ArgumentException($"Purchase Order with ID {id} not found");

            if (!await CanEditPurchaseOrderAsync(id))
                throw new InvalidOperationException("Purchase Order cannot be edited in current status");

            // Update main properties
            existingPO.SupplierId = viewModel.SupplierId;
            existingPO.OrderDate = viewModel.OrderDate;
            existingPO.ExpectedDeliveryDate = viewModel.ExpectedDeliveryDate;
            existingPO.Notes = viewModel.Notes;
            existingPO.ModifiedDate = DateTime.Now;

            // Clear existing details and add new ones
            existingPO.PurchaseOrderDetails.Clear();
            
            foreach (var detailVM in viewModel.Details)
            {
                var detail = new PurchaseOrderDetail
                {
                    ItemId = detailVM.ItemId,
                    Quantity = detailVM.Quantity,
                    UnitPrice = detailVM.UnitPrice,
                    Notes = detailVM.Notes,
                    CreatedDate = DateTime.Now
                };
                
                detail.CalculateTotalPrice();
                existingPO.PurchaseOrderDetails.Add(detail);
            }

            // Recalculate total amount
            existingPO.TotalAmount = existingPO.PurchaseOrderDetails.Sum(d => d.TotalPrice);

            await _purchaseOrderRepository.UpdateAsync(existingPO);
            return existingPO;
        }

        public async Task<bool> DeletePurchaseOrderAsync(int id)
        {
            if (!await CanEditPurchaseOrderAsync(id))
                return false;

            return await _purchaseOrderRepository.DeleteAsync(id);
        }

        #endregion

        #region Business Logic Operations

        public async Task<bool> SendPurchaseOrderEmailAsync(int id)
        {
            try
            {
                var purchaseOrder = await GetPurchaseOrderByIdAsync(id);
                if (purchaseOrder == null || string.IsNullOrEmpty(purchaseOrder.Supplier?.Email))
                {
                    return false;
                }

                if (!await CanSendPurchaseOrderAsync(id))
                {
                    return false;
                }

                // Generate email content
                var emailContent = await GeneratePurchaseOrderEmailContentAsync(purchaseOrder);
                var subject = $"Purchase Order {purchaseOrder.PONumber}";

                // Send email directly - no retries or complex logic
                var success = await _emailService.SendEmailAsync(
                    purchaseOrder.Supplier.Email,
                    subject,
                    emailContent
                );

                return success;
            }
            catch (Exception)
            {
                // Log the error but don't throw - let controller handle it
                return false;
            }
        }
        public async Task<bool> CancelPurchaseOrderAsync(int id)
        {
            if (!await CanCancelPurchaseOrderAsync(id))
                return false;

            return await UpdateStatusAsync(id, PurchaseOrderStatus.Cancelled);
        }

        public async Task<bool> UpdateStatusAsync(int id, PurchaseOrderStatus status)
        {
            await _purchaseOrderRepository.UpdateStatusAsync(id, status);
            return true;
        }

        #endregion

        #region Query Operations

        public async Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersBySupplierAsync(int supplierId)
        {
            return await _purchaseOrderRepository.GetBySupplierAsync(supplierId);
        }

        public async Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersByStatusAsync(PurchaseOrderStatus status)
        {
            return await _purchaseOrderRepository.GetByStatusAsync(status);
        }

        public async Task<IEnumerable<PurchaseOrder>> GetPendingPurchaseOrdersAsync()
        {
            return await _purchaseOrderRepository.GetPendingPurchaseOrdersAsync();
        }

        #endregion

        #region Validation Operations

        public async Task<bool> ValidatePurchaseOrderAsync(PurchaseOrderViewModel viewModel)
        {
            // Validate supplier exists and is active
            var supplier = await _supplierRepository.GetByIdAsync(viewModel.SupplierId);
            if (supplier == null || !supplier.IsActive)
                return false;

            // Validate all items exist and are active
            foreach (var detail in viewModel.Details)
            {
                var item = await _itemRepository.GetByIdAsync(detail.ItemId);
                if (item == null || !item.IsActive)
                    return false;
            }

            // Validate business rules
            if (viewModel.Details.Count == 0)
                return false;

            if (viewModel.Details.Any(d => d.Quantity <= 0 || d.UnitPrice <= 0))
                return false;

            return true;
        }

        public async Task<string> GenerateNextPONumberAsync()
        {
            return await _purchaseOrderRepository.GenerateNextPONumberAsync();
        }

        public async Task<bool> IsPONumberUniqueAsync(string poNumber)
        {
            return !await _purchaseOrderRepository.ExistsByPONumberAsync(poNumber);
        }

        public async Task<bool> CanEditPurchaseOrderAsync(int id)
        {
            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(id);
            return purchaseOrder != null && purchaseOrder.Status == Constants.PO_STATUS_DRAFT;
        }

        public async Task<bool> CanSendPurchaseOrderAsync(int id)
        {
            var purchaseOrder = await GetPurchaseOrderByIdAsync(id);
            return purchaseOrder != null && 
                   purchaseOrder.Status == Constants.PO_STATUS_DRAFT && 
                   purchaseOrder.PurchaseOrderDetails.Any();
        }

        public async Task<bool> CanCancelPurchaseOrderAsync(int id)
        {
            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(id);
            return purchaseOrder != null && 
                   (purchaseOrder.Status == Constants.PO_STATUS_DRAFT || 
                    purchaseOrder.Status == Constants.PO_STATUS_SENT);
        }

        #endregion

        #region ViewModel Operations

        public async Task<PurchaseOrderViewModel> GetPurchaseOrderViewModelAsync(int? id = null)
        {
            var viewModel = new PurchaseOrderViewModel
            {
                OrderDate = DateTime.Today,
                ExpectedDeliveryDate = DateTime.Today.AddDays(7)
            };

            if (id.HasValue)
            {
                var purchaseOrder = await GetPurchaseOrderByIdAsync(id.Value);
                if (purchaseOrder != null)
                {
                    viewModel.Id = purchaseOrder.Id;
                    viewModel.PONumber = purchaseOrder.PONumber;
                    viewModel.SupplierId = purchaseOrder.SupplierId;
                    viewModel.OrderDate = purchaseOrder.OrderDate;
                    viewModel.ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate;
                    viewModel.Notes = purchaseOrder.Notes;
                    viewModel.SupplierName = purchaseOrder.Supplier.Name;
                    viewModel.TotalAmount = purchaseOrder.TotalAmount;
                    viewModel.Status = purchaseOrder.Status;

                    viewModel.Details = purchaseOrder.PurchaseOrderDetails.Select(d => new PurchaseOrderDetailViewModel
                    {
                        Id = d.Id,
                        ItemId = d.ItemId,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        TotalPrice = d.TotalPrice,
                        Notes = d.Notes,
                        ItemCode = d.Item.ItemCode,
                        ItemName = d.Item.Name,
                        ItemUnit = d.Item.Unit
                    }).ToList();
                }
            }

            return await PopulatePurchaseOrderViewModelAsync(viewModel);
        }

        public async Task<PurchaseOrderViewModel> PopulatePurchaseOrderViewModelAsync(PurchaseOrderViewModel viewModel)
        {
            var suppliers = await _supplierRepository.GetActiveSuppliers();
            viewModel.Suppliers = new SelectList(suppliers, "Id", "Name", viewModel.SupplierId);

            var items = await _itemRepository.GetActiveItemsAsync();
            viewModel.Items = new SelectList(items, "Id", "DisplayName");

            return viewModel;
        }

        #endregion

        #region Calculation Operations

        public async Task<decimal> CalculateTotalAmountAsync(IEnumerable<PurchaseOrderDetailViewModel> details)
        {
            return await Task.FromResult(details.Sum(d => d.Quantity * d.UnitPrice));
        }

        public async Task<PurchaseOrder> RecalculateTotalAsync(PurchaseOrder purchaseOrder)
        {
            purchaseOrder.TotalAmount = purchaseOrder.PurchaseOrderDetails.Sum(d => d.TotalPrice);
            await _purchaseOrderRepository.UpdateAsync(purchaseOrder);
            return purchaseOrder;
        }

        #endregion

        #region Email Operations

        public async Task<string> GeneratePurchaseOrderEmailContentAsync(PurchaseOrder purchaseOrder)
        {
            var content = $@"Dear {purchaseOrder.Supplier.Name},

Please find below our Purchase Order:

Purchase Order Number: {purchaseOrder.PONumber}
Order Date: {purchaseOrder.OrderDate:dd/MM/yyyy}
Expected Delivery Date: {purchaseOrder.ExpectedDeliveryDate?.ToString("dd/MM/yyyy") ?? "TBD"}

Items Ordered:
";

            foreach (var detail in purchaseOrder.PurchaseOrderDetails)
            {
                content += $@"
- {detail.Item.ItemCode} - {detail.Item.Name}
  Quantity: {detail.Quantity} {detail.Item.Unit}
  Unit Price: {detail.UnitPrice:C}
  Total: {detail.TotalPrice:C}
";
            }

            content += $@"

Total Amount: {purchaseOrder.TotalAmount:C}

{(string.IsNullOrEmpty(purchaseOrder.Notes) ? "" : $"Notes: {purchaseOrder.Notes}")}

Please confirm receipt of this order and provide estimated delivery date.

Best regards,
Warehouse Management System";

            return await Task.FromResult(content);
        }

        public async Task<bool> IsEmailSentAsync(int id)
        {
            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(id);
            return purchaseOrder?.EmailSent ?? false;
        }

        public async Task MarkEmailAsSentAsync(int id)
        {
            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(id);
            if (purchaseOrder != null)
            {
                purchaseOrder.EmailSent = true;
                purchaseOrder.EmailSentDate = DateTime.Now;
                await _purchaseOrderRepository.UpdateAsync(purchaseOrder);
            }
        }

        #endregion
    }
}