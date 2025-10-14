using WMS.Data.Repositories;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WMS.Services
{
    /// <summary>
    /// Service implementation untuk Sales Order management
    /// "The Climax" - dimana warehouse fee diterapkan dan inventory dikurangi
    /// </summary>
    public class SalesOrderService : ISalesOrderService
    {
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<SalesOrderService> _logger;

        public SalesOrderService(
            ISalesOrderRepository salesOrderRepository,
            ICustomerRepository customerRepository,
            IItemRepository itemRepository,
            IInventoryRepository inventoryRepository,
            IInventoryService inventoryService,
            ILogger<SalesOrderService> logger)
        {
            _salesOrderRepository = salesOrderRepository;
            _customerRepository = customerRepository;
            _itemRepository = itemRepository;
            _inventoryRepository = inventoryRepository;
            _inventoryService = inventoryService;
            this._logger = logger;
        }

        #region Basic CRUD Operations

        public async Task<IEnumerable<SalesOrder>> GetAllSalesOrdersAsync()
        {
            return await _salesOrderRepository.GetAllWithDetailsAsync();
        }

        public async Task<SalesOrder?> GetSalesOrderByIdAsync(int id)
        {
            return await _salesOrderRepository.GetByIdWithDetailsAsync(id);
        }

        public async Task<SalesOrder> CreateSalesOrderAsync(SalesOrderViewModel viewModel)
        {
            if (!await ValidateSalesOrderAsync(viewModel))
                throw new InvalidOperationException("Sales Order validation failed");

            if (!await ValidateStockAvailabilityAsync(viewModel))
                throw new InvalidOperationException("Insufficient stock for some items");

            var salesOrder = new SalesOrder
            {
                SONumber = await GenerateNextSONumberAsync(),
                CustomerId = viewModel.CustomerId,
                OrderDate = viewModel.OrderDate,
                RequiredDate = viewModel.RequiredDate,
                Notes = viewModel.Notes,
                Status = Constants.SO_STATUS_DRAFT,
                CreatedDate = DateTime.Now
            };

            // Create details with warehouse fee calculation
            foreach (var detailVM in viewModel.Details)
            {
                var detail = new SalesOrderDetail
                {
                    ItemId = detailVM.ItemId,
                    Quantity = detailVM.Quantity,
                    UnitPrice = detailVM.UnitPrice,
                    Notes = detailVM.Notes,
                    CreatedDate = DateTime.Now
                };

                detail.CalculateTotalPrice();


                salesOrder.SalesOrderDetails.Add(detail);
            }

            // Calculate totals
            salesOrder.TotalAmount = salesOrder.SalesOrderDetails.Sum(d => d.TotalPrice);

            return await _salesOrderRepository.CreateWithDetailsAsync(salesOrder);
        }

        public async Task<SalesOrder> UpdateSalesOrderAsync(int id, SalesOrderViewModel viewModel)
        {
            var existingSO = await _salesOrderRepository.GetByIdWithDetailsAsync(id);
            if (existingSO == null)
                throw new ArgumentException($"Sales Order with ID {id} not found");

            if (!await CanEditSalesOrderAsync(id))
                throw new InvalidOperationException("Sales Order cannot be edited in current status");

            // Release existing reserved stock if any
            if (existingSO.Status == Constants.SO_STATUS_CONFIRMED)
            {
                await ReleaseReservedStockAsync(id);
            }

            // Update main properties
            existingSO.CustomerId = viewModel.CustomerId;
            existingSO.OrderDate = viewModel.OrderDate;
            existingSO.RequiredDate = viewModel.RequiredDate;
            existingSO.Notes = viewModel.Notes;
            existingSO.ModifiedDate = DateTime.Now;

            // Clear existing details and add new ones
            existingSO.SalesOrderDetails.Clear();

            foreach (var detailVM in viewModel.Details)
            {
                var detail = new SalesOrderDetail
                {
                    ItemId = detailVM.ItemId,
                    Quantity = detailVM.Quantity,
                    UnitPrice = detailVM.UnitPrice,
                    Notes = detailVM.Notes,
                    CreatedDate = DateTime.Now
                };

                detail.CalculateTotalPrice();
                existingSO.SalesOrderDetails.Add(detail);
            }


            await _salesOrderRepository.UpdateAsync(existingSO);
            return existingSO;
        }

        public async Task<bool> DeleteSalesOrderAsync(int id)
        {
            if (!await CanEditSalesOrderAsync(id))
                return false;

            // Release any reserved stock
            await ReleaseReservedStockAsync(id);

            return await _salesOrderRepository.DeleteAsync(id);
        }

        #endregion

        #region Business Logic Operations

        public async Task<bool> UpdateStatusAsync(int id, SalesOrderStatus status)
        {
            await _salesOrderRepository.UpdateStatusAsync(id, status);
            return true;
        }

        public async Task<bool> ConfirmSalesOrderAsync(int id)
        {
            var salesOrder = await GetSalesOrderByIdAsync(id);
            if (salesOrder == null || !await CanConfirmSalesOrderAsync(id))
                return false;

            // Final stock validation
            var stockValidation = await CheckItemStockAsync(salesOrder.SalesOrderDetails.Select(d =>
                new SalesOrderDetailViewModel
                {
                    ItemId = d.ItemId,
                    Quantity = d.Quantity
                }));

            var insufficientItems = stockValidation.Where(kv => kv.Value < 0).ToList();
            if (insufficientItems.Any())
                return false;

            // Reserve stock
            if (!await ReserveStockAsync(id))
                return false;

            return await UpdateStatusAsync(id, SalesOrderStatus.Confirmed);
        }

        public async Task<bool> ShipSalesOrderAsync(int id)
        {
            if (!await CanShipSalesOrderAsync(id))
                return false;

            // Process actual stock reduction
            if (!await ProcessStockReductionAsync(id))
                return false;

            return await UpdateStatusAsync(id, SalesOrderStatus.Shipped);
        }

        public async Task<bool> CompleteSalesOrderAsync(int id)
        {
            if (!await CanCompleteSalesOrderAsync(id))
                return false;

            return await UpdateStatusAsync(id, SalesOrderStatus.Completed);
        }

        public async Task<bool> CancelSalesOrderAsync(int id)
        {
            if (!await CanCancelSalesOrderAsync(id))
                return false;

            // Release any reserved stock
            await ReleaseReservedStockAsync(id);

            return await UpdateStatusAsync(id, SalesOrderStatus.Cancelled);
        }

        #endregion

        #region Stock Management Operations

        public async Task<bool> ValidateStockAvailabilityAsync(SalesOrderViewModel viewModel)
        {
            var stockCheck = await CheckItemStockAsync(viewModel.Details);
            return !stockCheck.Any(kv => kv.Value < 0); // No insufficient stock
        }

        public async Task<Dictionary<int, int>> CheckItemStockAsync(IEnumerable<SalesOrderDetailViewModel> details)
        {
            var result = new Dictionary<int, int>();

            foreach (var detail in details)
            {
                var availableInventories = await _inventoryRepository.GetByItemIdAsync(detail.ItemId);
                var totalAvailable = availableInventories
                    .Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE)
                    .Sum(inv => inv.Quantity);

                result[detail.ItemId] = totalAvailable - detail.Quantity; // Negative = insufficient
            }

            return result;
        }

        public async Task<bool> ReserveStockAsync(int salesOrderId)
        {
            var salesOrder = await GetSalesOrderByIdAsync(salesOrderId);
            if (salesOrder == null)
                return false;

            try
            {
                foreach (var detail in salesOrder.SalesOrderDetails)
                {
                    var availableInventories = await _inventoryRepository.GetByItemIdAsync(detail.ItemId);
                    var sortedInventories = availableInventories
                        .Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE && inv.Quantity > 0)
                        .OrderBy(inv => inv.LastUpdated) // FIFO
                        .ToList();

                    var remainingToReserve = detail.Quantity;

                    foreach (var inventory in sortedInventories)
                    {
                        if (remainingToReserve <= 0) break;

                        var quantityToReserve = Math.Min(inventory.Quantity, remainingToReserve);


                        remainingToReserve -= quantityToReserve;
                        await _inventoryRepository.UpdateAsync(inventory);
                    }

                    if (remainingToReserve > 0)
                        return false; // Could not reserve all required stock
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ReleaseReservedStockAsync(int salesOrderId)
        {
            // Implementation would depend on how reserved stock is tracked
            // For now, we'll assume it's handled by inventory status
            return await Task.FromResult(true);
        }

        public async Task<bool> ProcessStockReductionAsync(int salesOrderId)
        {
            var salesOrder = await GetSalesOrderByIdAsync(salesOrderId);
            if (salesOrder == null)
                return false;

            try
            {
                foreach (var detail in salesOrder.SalesOrderDetails)
                {
                    var availableInventories = await _inventoryRepository.GetByItemIdAsync(detail.ItemId);
                    var sortedInventories = availableInventories
                        .Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE && inv.Quantity > 0)
                        .OrderBy(inv => inv.LastUpdated) // FIFO
                        .ToList();

                    var remainingToReduce = detail.Quantity;

                    foreach (var inventory in sortedInventories)
                    {
                        if (remainingToReduce <= 0) break;

                        var quantityToReduce = Math.Min(inventory.Quantity, remainingToReduce);

                        if (!inventory.ReduceStock(quantityToReduce))
                            return false;

                        remainingToReduce -= quantityToReduce;
                        await _inventoryRepository.UpdateAsync(inventory);
                    }

                    if (remainingToReduce > 0)
                        return false; // Could not reduce all required stock
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Query Operations

        public async Task<IEnumerable<SalesOrder>> GetSalesOrdersByCustomerAsync(int customerId)
        {
            return await _salesOrderRepository.GetByCustomerAsync(customerId);
        }

        public async Task<IEnumerable<SalesOrder>> GetSalesOrdersByStatusAsync(SalesOrderStatus status)
        {
            return await _salesOrderRepository.GetByStatusAsync(status);
        }

        public async Task<IEnumerable<SalesOrder>> GetConfirmedSalesOrdersAsync()
        {
            return await _salesOrderRepository.GetConfirmedSalesOrdersAsync();
        }

        public async Task<IEnumerable<SalesOrder>> GetPendingSalesOrdersAsync()
        {
            return await GetSalesOrdersByStatusAsync(SalesOrderStatus.Draft);
        }

        #endregion

        #region Validation Operations

        public async Task<bool> ValidateSalesOrderAsync(SalesOrderViewModel viewModel)
        {
            // Validate customer exists and is active
            var customer = await _customerRepository.GetByIdAsync(viewModel.CustomerId);
            if (customer == null || !customer.IsActive)
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

        public async Task<string> GenerateNextSONumberAsync()
        {
            return await _salesOrderRepository.GenerateNextSONumberAsync();
        }

        public async Task<bool> IsSONumberUniqueAsync(string soNumber)
        {
            return !await _salesOrderRepository.ExistsBySONumberAsync(soNumber);
        }

        public async Task<bool> CanEditSalesOrderAsync(int id)
        {
            var salesOrder = await _salesOrderRepository.GetByIdAsync(id);
            return salesOrder != null && salesOrder.Status == Constants.SO_STATUS_DRAFT;
        }

        public async Task<bool> CanConfirmSalesOrderAsync(int id)
        {
            var salesOrder = await GetSalesOrderByIdAsync(id);
            return salesOrder != null &&
                   salesOrder.Status == Constants.SO_STATUS_DRAFT &&
                   salesOrder.SalesOrderDetails.Any();
        }

        public async Task<bool> CanShipSalesOrderAsync(int id)
        {
            var salesOrder = await _salesOrderRepository.GetByIdAsync(id);
            return salesOrder != null && salesOrder.Status == Constants.SO_STATUS_CONFIRMED;
        }

        public async Task<bool> CanCompleteSalesOrderAsync(int id)
        {
            var salesOrder = await _salesOrderRepository.GetByIdAsync(id);
            return salesOrder != null && salesOrder.Status == Constants.SO_STATUS_SHIPPED;
        }

        public async Task<bool> CanCancelSalesOrderAsync(int id)
        {
            var salesOrder = await _salesOrderRepository.GetByIdAsync(id);
            return salesOrder != null &&
                   (salesOrder.Status == Constants.SO_STATUS_DRAFT ||
                    salesOrder.Status == Constants.SO_STATUS_CONFIRMED);
        }

        #endregion

        #region ViewModel Operations

        public async Task<SalesOrderViewModel> GetSalesOrderViewModelAsync(int? id = null)
        {
            var viewModel = new SalesOrderViewModel
            {
                OrderDate = DateTime.Today,
                RequiredDate = DateTime.Today.AddDays(7)
            };

            if (id.HasValue)
            {
                var salesOrder = await GetSalesOrderByIdAsync(id.Value);
                if (salesOrder != null)
                {
                    viewModel.Id = salesOrder.Id;
                    viewModel.SONumber = salesOrder.SONumber;
                    viewModel.CustomerId = salesOrder.CustomerId;
                    viewModel.OrderDate = salesOrder.OrderDate;
                    viewModel.RequiredDate = salesOrder.RequiredDate;
                    viewModel.Notes = salesOrder.Notes;
                    viewModel.CustomerName = salesOrder.Customer.Name;
                    viewModel.TotalAmount = salesOrder.TotalAmount;
                    viewModel.GrandTotal = salesOrder.GrandTotal;
                    viewModel.Status = salesOrder.Status;

                    viewModel.Details = salesOrder.SalesOrderDetails.Select(d => new SalesOrderDetailViewModel
                    {
                        Id = d.Id,
                        ItemId = d.ItemId,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        TotalPrice = d.TotalPrice,
                        Notes = d.Notes,
                        ItemCode = d.Item.ItemCode,
                        ItemName = d.Item.Name,
                        ItemUnit = d.Item.Unit,
                    }).ToList();
                }
            }

            return await PopulateSalesOrderViewModelAsync(viewModel);
        }

        public async Task<SalesOrderViewModel> PopulateSalesOrderViewModelAsync(SalesOrderViewModel viewModel)
        {
            var customers = await _customerRepository.GetActiveCustomersAsync();
            viewModel.Customers = new SelectList(customers, "Id", "Name", viewModel.CustomerId);

            var items = await _itemRepository.GetActiveItemsAsync();
            viewModel.Items = new SelectList(items, "Id", "DisplayName");

            return viewModel;
        }

        public async Task<SalesOrderViewModel> ValidateAndPopulateStockInfoAsync(SalesOrderViewModel viewModel)
        {
            viewModel.StockWarnings.Clear();

            if (viewModel.Details.Any())
            {
                var stockCheck = await CheckItemStockAsync(viewModel.Details);

                foreach (var detail in viewModel.Details)
                {
                    if (stockCheck.ContainsKey(detail.ItemId))
                    {
                        var availableStock = stockCheck[detail.ItemId] + detail.Quantity; // Add back requested qty
                        detail.AvailableStock = Math.Max(0, availableStock);

                        if (!detail.IsStockSufficient)
                        {
                            viewModel.StockWarnings.Add($"{detail.ItemCode} - {detail.ItemName}: " +
                                $"Requested {detail.Quantity} {detail.ItemUnit}, but only {detail.AvailableStock} available");
                        }
                    }
                }
            }

            return viewModel;
        }

        #endregion

        #region Item Management

        public async Task<IEnumerable<object>> GetAvailableItemsAsync()
        {
            try
            {
                var items = await _itemRepository.GetAllAsync();
                var result = new List<object>();

                foreach (var item in items.Where(i => i.IsActive))
                {
                    var availableStock = await _inventoryService.GetTotalStockByItemAsync(item.Id);
                    if (availableStock > 0)
                    {
                        result.Add(new
                        {
                            id = item.Id,
                            itemCode = item.ItemCode,
                            itemName = item.Name,
                            unit = item.Unit,
                            availableStock = availableStock,
                            standardPrice = item.StandardPrice
                        });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available items");
                return new List<object>();
            }
        }

        #endregion

        #region Warehouse Fee Operations

        public Task<SalesOrder> CalculateWarehouseFeesAsync(SalesOrder salesOrder)
        {
            foreach (var detail in salesOrder.SalesOrderDetails)
            {
                // TODO: Implement warehouse fee calculation logic
            }

            return Task.FromResult(salesOrder);
        }




        #endregion

        #region Financial Operations

        public async Task<decimal> CalculateTotalAmountAsync(IEnumerable<SalesOrderDetailViewModel> details)
        {
            return await Task.FromResult(details.Sum(d => d.Quantity * d.UnitPrice));
        }



        #endregion

        #region Reporting Operations

        public async Task<Dictionary<string, object>> GetSalesOrderSummaryAsync(int salesOrderId)
        {
            var salesOrder = await GetSalesOrderByIdAsync(salesOrderId);
            if (salesOrder == null)
                return new Dictionary<string, object>();

            var summary = new Dictionary<string, object>
            {
                ["SONumber"] = salesOrder.SONumber,
                ["CustomerName"] = salesOrder.Customer.Name,
                ["OrderDate"] = salesOrder.OrderDate,
                ["RequiredDate"] = salesOrder.RequiredDate,
                ["Status"] = salesOrder.StatusIndonesia,
                ["TotalItems"] = salesOrder.TotalItemTypes,
                ["TotalQuantity"] = salesOrder.TotalQuantity,
                ["TotalAmount"] = salesOrder.TotalAmount,
                ["GrandTotal"] = salesOrder.GrandTotal,
                ["ItemDetails"] = salesOrder.SalesOrderDetails.Select(d => new
                {
                    ItemCode = d.Item.ItemCode,
                    ItemName = d.Item.Name,
                    Quantity = d.Quantity,
                    Unit = d.Item.Unit,
                    UnitPrice = d.UnitPrice,
                    TotalPrice = d.TotalPrice,
                }).ToList()
            };

            return await Task.FromResult(summary);
        }


        public async Task<IEnumerable<object>> GetTopSellingItemsAsync(int topCount = 10)
        {
            var allSalesOrders = await GetAllSalesOrdersAsync();
            var completedOrders = allSalesOrders
                .Where(so => so.Status == Constants.SO_STATUS_COMPLETED)
                .ToList();

            var itemSales = completedOrders
                .SelectMany(so => so.SalesOrderDetails)
                .GroupBy(d => new { d.ItemId, d.Item.ItemCode, d.Item.Name, d.Item.Unit })
                .Select(g => new
                {
                    ItemId = g.Key.ItemId,
                    ItemCode = g.Key.ItemCode,
                    ItemName = g.Key.Name,
                    Unit = g.Key.Unit,
                    TotalQuantitySold = g.Sum(d => d.Quantity),
                    TotalRevenue = g.Sum(d => d.TotalPrice),
                    OrderCount = g.Count(),
                    AverageUnitPrice = g.Average(d => d.UnitPrice)
                })
                .OrderByDescending(x => x.TotalQuantitySold)
                .Take(topCount)
                .ToList();

            return await Task.FromResult(itemSales.Cast<object>());
        }

        public async Task<Dictionary<string, object>> GetSalesStatisticsAsync()
        {
            var allSalesOrders = await GetAllSalesOrdersAsync();

            var stats = new Dictionary<string, object>
            {
                ["TotalSalesOrders"] = allSalesOrders.Count(),
                ["DraftOrders"] = allSalesOrders.Count(so => so.Status == Constants.SO_STATUS_DRAFT),
                ["ConfirmedOrders"] = allSalesOrders.Count(so => so.Status == Constants.SO_STATUS_CONFIRMED),
                ["ShippedOrders"] = allSalesOrders.Count(so => so.Status == Constants.SO_STATUS_SHIPPED),
                ["CompletedOrders"] = allSalesOrders.Count(so => so.Status == Constants.SO_STATUS_COMPLETED),
                ["CancelledOrders"] = allSalesOrders.Count(so => so.Status == Constants.SO_STATUS_CANCELLED),
                ["TotalRevenue"] = allSalesOrders
                    .Where(so => so.Status == Constants.SO_STATUS_COMPLETED)
                    .Sum(so => so.TotalAmount),
                ["AverageOrderValue"] = allSalesOrders
                    .Where(so => so.Status == Constants.SO_STATUS_COMPLETED)
                    .DefaultIfEmpty()
                    .Average(so => so?.GrandTotal ?? 0)
            };

            return await Task.FromResult(stats);
        }

        #endregion
    }
}