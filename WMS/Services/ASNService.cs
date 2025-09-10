using WMS.Data.Repositories;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;
using Microsoft.AspNetCore.Mvc.Rendering;
using WMS.Controllers;

namespace WMS.Services
{
    /// <summary>
    /// Service implementation untuk Advanced Shipping Notice management
    /// "The Plot Twist" - menangani actual price dan warehouse fee calculation
    /// </summary>
    public class ASNService : IASNService
    {
        private readonly IASNRepository _asnRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IInventoryService _inventoryService;
        private readonly IWarehouseFeeCalculator _warehouseFeeCalculator;
        private readonly ILogger<ASNService> _logger;

        public ASNService(
            IASNRepository asnRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            IItemRepository itemRepository,
            IInventoryService inventoryService,
            IWarehouseFeeCalculator warehouseFeeCalculator,
             ILogger<ASNService> logger)
        {
            _asnRepository = asnRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _itemRepository = itemRepository;
            _inventoryService = inventoryService;
            _warehouseFeeCalculator = warehouseFeeCalculator;
            _logger = logger;
        }

        #region Basic CRUD Operations

        public async Task<IEnumerable<AdvancedShippingNotice>> GetAllASNsAsync()
        {
            return await _asnRepository.GetAllWithDetailsAsync();
        }

        public async Task<AdvancedShippingNotice?> GetASNByIdAsync(int id)
        {
            return await _asnRepository.GetByIdWithDetailsAsync(id);
        }

        public async Task<AdvancedShippingNotice> CreateASNAsync(ASNViewModel viewModel)
        {
            if (!await ValidateASNAsync(viewModel))
                throw new InvalidOperationException("ASN validation failed");

            var asn = new AdvancedShippingNotice
            {
                ASNNumber = await GenerateNextASNNumberAsync(),
                PurchaseOrderId = viewModel.PurchaseOrderId,
                ShipmentDate = viewModel.ShipmentDate,
                ExpectedArrivalDate = viewModel.ExpectedArrivalDate,
                Status = Constants.ASN_STATUS_IN_TRANSIT,
                CarrierName = viewModel.CarrierName,
                TrackingNumber = viewModel.TrackingNumber,
                Notes = viewModel.Notes,
                CreatedDate = DateTime.Now
            };

            // Create ASN details with warehouse fee calculation
            foreach (var detailVM in viewModel.Details)
            {
                var detail = new ASNDetail
                {
                    ItemId = detailVM.ItemId,
                    ShippedQuantity = detailVM.ShippedQuantity,
                    ActualPricePerItem = detailVM.ActualPricePerItem,
                    Notes = detailVM.Notes,
                    CreatedDate = DateTime.Now
                };

                // Initialize remaining quantity
                detail.InitializeRemainingQuantity();
                
                // Calculate warehouse fee
                await CalculateWarehouseFeeForDetailAsync(detail);
                asn.ASNDetails.Add(detail);
            }

            // Create ASN first
            var createdASN = await _asnRepository.CreateWithDetailsAsync(asn);

            // UPDATE: Mark the related Purchase Order as Closed
            await _purchaseOrderRepository.UpdateStatusAsync(viewModel.PurchaseOrderId, PurchaseOrderStatus.Closed);

            return createdASN;
        }

        public async Task<AdvancedShippingNotice> UpdateASNAsync(int id, ASNViewModel viewModel)
        {
            var existingASN = await _asnRepository.GetByIdWithDetailsAsync(id);
            if (existingASN == null)
                throw new ArgumentException($"ASN with ID {id} not found");

            if (!await CanEditASNAsync(id))
                throw new InvalidOperationException("ASN cannot be edited in current status");

            // Update main properties
            existingASN.ShipmentDate = viewModel.ShipmentDate;
            existingASN.ExpectedArrivalDate = viewModel.ExpectedArrivalDate;
            existingASN.CarrierName = viewModel.CarrierName;
            existingASN.TrackingNumber = viewModel.TrackingNumber;
            existingASN.Notes = viewModel.Notes;
            existingASN.ModifiedDate = DateTime.Now;

            // Clear existing details and add new ones
            existingASN.ASNDetails.Clear();

            foreach (var detailVM in viewModel.Details)
            {
                var detail = new ASNDetail
                {
                    ItemId = detailVM.ItemId,
                    ShippedQuantity = detailVM.ShippedQuantity,
                    ActualPricePerItem = detailVM.ActualPricePerItem,
                    Notes = detailVM.Notes,
                    CreatedDate = DateTime.Now
                };

                // Calculate warehouse fee
                await CalculateWarehouseFeeForDetailAsync(detail);
                existingASN.ASNDetails.Add(detail);
            }

            await _asnRepository.UpdateAsync(existingASN);
            return existingASN;
        }

        public async Task<bool> DeleteASNAsync(int id)
        {
            if (!await CanEditASNAsync(id))
                return false;

            try
            {
                await _asnRepository.DeleteAsync(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Business Logic Operations

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            try
            {
                var asn = await _asnRepository.GetByIdAsync(id);
                if (asn == null)
                    return false;

                asn.Status = status;
                asn.ModifiedDate = DateTime.Now;

                await _asnRepository.UpdateAsync(asn);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Overload for ASNStatus enum
        public async Task<bool> UpdateStatusAsync(int id, ASNStatus status)
        {
            string statusString = status switch
            {
                ASNStatus.InTransit => Constants.ASN_STATUS_IN_TRANSIT,
                ASNStatus.Arrived => Constants.ASN_STATUS_ARRIVED,
                ASNStatus.Processed => Constants.ASN_STATUS_PROCESSED,
                ASNStatus.Cancelled => Constants.ASN_STATUS_CANCELLED,
                _ => Constants.ASN_STATUS_IN_TRANSIT
            };

            return await UpdateStatusAsync(id, statusString);
        }

        public async Task<bool> ProcessASNAsync(int id)
        {
            try
            {
                var asn = await GetASNByIdAsync(id);
                if (asn == null || !await CanProcessASNAsync(id))
                    return false;

                // Process each ASN detail into inventory
                foreach (var detail in asn.ASNDetails)
                {
                    // This will be handled by inventory service for putaway process
                    // For now, we just mark it as available for putaway
                    // await _inventoryService.ProcessASNDetailAsync(detail);
                }

                // Update ASN status to processed
                return await UpdateStatusAsync(id, Constants.ASN_STATUS_PROCESSED);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> MarkAsArrivedAsync(int id)
        {
            // Use the new method with automatic date setting
            return await MarkAsArrivedWithActualDateAsync(id);
        }

        public async Task<bool> MarkAsArrivedWithActualDateAsync(int id, DateTime? actualArrivalDate = null)
        {
            try
            {
                var asn = await _asnRepository.GetByIdAsync(id);
                if (asn == null)
                {
                    return false;
                }

                // Check if ASN can be marked as arrived (should be In Transit)
                if (asn.Status != Constants.ASN_STATUS_IN_TRANSIT)
                {
                    return false;
                }

                // Update status to Arrived and set actual arrival date
                asn.Status = Constants.ASN_STATUS_ARRIVED;
                asn.ActualArrivalDate = actualArrivalDate ?? DateTime.Now; // Auto-set current time if not provided
                asn.ModifiedDate = DateTime.Now;

                await _asnRepository.UpdateAsync(asn);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> MarkAsProcessedAsync(int id)
        {
            try
            {
                var asn = await GetASNByIdAsync(id);
                if (asn == null || asn.Status != Constants.ASN_STATUS_ARRIVED)
                    return false;

                return await UpdateStatusAsync(id, Constants.ASN_STATUS_PROCESSED);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CancelASNAsync(int id)
        {
            try
            {
                if (!await CanCancelASNAsync(id))
                    return false;

                return await UpdateStatusAsync(id, Constants.ASN_STATUS_CANCELLED);
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Query Operations

        public async Task<IEnumerable<AdvancedShippingNotice>> GetASNsByPurchaseOrderAsync(int purchaseOrderId)
        {
            return await _asnRepository.GetByPurchaseOrderAsync(purchaseOrderId);
        }
        public async Task<IEnumerable<AdvancedShippingNotice>> GetProcessedASNsAsync()
        {
            return await _asnRepository.GetProcessedASNsAsync();
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetASNsByStatusAsync(ASNStatus status)
        {
            return await _asnRepository.GetByStatusAsync(status);
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetArrivedASNsAsync()
        {
            return await _asnRepository.GetArrivedASNsAsync();
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetInTransitASNsAsync()
        {
            return await GetASNsByStatusAsync(ASNStatus.InTransit);
        }
        public async Task<IEnumerable<ASNDetail>> GetASNDetailsForPutawayAsync(int asnId)
        {
            return await _asnRepository.GetASNDetailsForPutawayAsync(asnId);
        }
        #endregion

        #region Validation Operations

        public async Task<bool> ValidateASNAsync(ASNViewModel viewModel)
        {
            // Validate PO exists and is sent
            var po = await _purchaseOrderRepository.GetByIdAsync(viewModel.PurchaseOrderId);
            if (po == null || po.Status != Constants.PO_STATUS_SENT)
                return false;

            // Validate against PO
            return await ValidateASNAgainstPOAsync(viewModel);
        }

        public async Task<string> GenerateNextASNNumberAsync()
        {
            return await _asnRepository.GenerateNextASNNumberAsync();
        }

        public async Task<bool> IsASNNumberUniqueAsync(string asnNumber)
        {
            return !await _asnRepository.ExistsByASNNumberAsync(asnNumber);
        }

        public async Task<bool> CanEditASNAsync(int id)
        {
            try
            {
                var asn = await _asnRepository.GetByIdAsync(id);
                return asn != null && asn.Status == Constants.ASN_STATUS_IN_TRANSIT;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CanProcessASNAsync(int id)
        {
            try
            {
                var asn = await _asnRepository.GetByIdAsync(id);
                return asn != null && asn.Status == Constants.ASN_STATUS_ARRIVED;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<ASNDetail?> GetASNDetailByIdAsync(int asnDetailId)
        {
            try
            {
                return await _asnRepository.GetASNDetailByIdAsync(asnDetailId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ASN detail by ID {AsnDetailId}", asnDetailId);
                throw;
            }
        }
        public async Task<bool> CanCancelASNAsync(int id)
        {
            try
            {
                var asn = await _asnRepository.GetByIdAsync(id);
                return asn != null &&
                       (asn.Status == Constants.ASN_STATUS_IN_TRANSIT ||
                        asn.Status == Constants.ASN_STATUS_ARRIVED);
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region ViewModel Operations

        public async Task<ASNViewModel> GetASNViewModelAsync(int? id = null)
        {
            var viewModel = new ASNViewModel
            {
                ShipmentDate = DateTime.Today,
                ExpectedArrivalDate = DateTime.Today.AddDays(3)
            };

            if (id.HasValue)
            {
                var asn = await GetASNByIdAsync(id.Value);
                if (asn != null)
                {
                    viewModel.Id = asn.Id;
                    viewModel.ASNNumber = asn.ASNNumber;
                    viewModel.PurchaseOrderId = asn.PurchaseOrderId;
                    viewModel.ShipmentDate = asn.ShipmentDate;
                    viewModel.ExpectedArrivalDate = asn.ExpectedArrivalDate;
                    viewModel.ActualArrivalDate = asn.ActualArrivalDate; // NEW: Include actual arrival date
                    viewModel.CarrierName = asn.CarrierName;
                    viewModel.TrackingNumber = asn.TrackingNumber;
                    viewModel.Notes = asn.Notes;
                    viewModel.PONumber = asn.PurchaseOrder.PONumber;
                    viewModel.SupplierName = asn.PurchaseOrder.Supplier.Name;
                    viewModel.Status = asn.Status;
                    viewModel.TotalWarehouseFee = asn.TotalWarehouseFee;

                    viewModel.Details = asn.ASNDetails.Select(d => new ASNDetailViewModel
                    {
                        Id = d.Id,
                        ItemId = d.ItemId,
                        ShippedQuantity = d.ShippedQuantity,
                        RemainingQuantity = d.RemainingQuantity,
                        AlreadyPutAwayQuantity = d.AlreadyPutAwayQuantity,
                        ActualPricePerItem = d.ActualPricePerItem,
                        WarehouseFeeRate = d.WarehouseFeeRate,
                        WarehouseFeeAmount = d.WarehouseFeeAmount,
                        Notes = d.Notes,
                        ItemCode = d.Item.ItemCode,
                        ItemName = d.Item.Name,
                        ItemUnit = d.Item.Unit
                    }).ToList();

                    // Get PO details for reference
                    var poDetails = await GetPODetailsForASNAsync(asn.PurchaseOrderId);
                    viewModel.PODetails = poDetails.ToList();
                }
            }

            return await PopulateASNViewModelAsync(viewModel);
        }

        public async Task<ASNViewModel> PopulateASNViewModelAsync(ASNViewModel viewModel)
        {
            var availablePOs = await GetAvailablePurchaseOrdersAsync();
            viewModel.PurchaseOrders = new SelectList(availablePOs, "Id", "PONumber", viewModel.PurchaseOrderId);

            var items = await _itemRepository.GetActiveItemsAsync();
            viewModel.Items = new SelectList(items, "Id", "DisplayName");

            return viewModel;
        }

        #endregion

        #region Warehouse Fee Calculation

        public async Task<decimal> CalculateWarehouseFeeRateAsync(decimal actualPrice)
        {
            return await Task.FromResult(_warehouseFeeCalculator.CalculateFeeRate(actualPrice));
        }

        public async Task<decimal> CalculateWarehouseFeeAmountAsync(decimal actualPrice)
        {
            return await Task.FromResult(_warehouseFeeCalculator.CalculateFeeAmount(actualPrice));
        }

        public async Task<ASNDetail> CalculateWarehouseFeeForDetailAsync(ASNDetail detail)
        {
            detail.WarehouseFeeRate = await CalculateWarehouseFeeRateAsync(detail.ActualPricePerItem);
            detail.WarehouseFeeAmount = await CalculateWarehouseFeeAmountAsync(detail.ActualPricePerItem);
            return detail;
        }

        public async Task<AdvancedShippingNotice> RecalculateWarehouseFeesAsync(AdvancedShippingNotice asn)
        {
            foreach (var detail in asn.ASNDetails)
            {
                await CalculateWarehouseFeeForDetailAsync(detail);
            }

            await _asnRepository.UpdateAsync(asn);
            return asn;
        }

        #endregion

        #region Price Analysis

        public async Task<Dictionary<string, object>> GetPriceVarianceAnalysisAsync(int asnId)
        {
            var asn = await GetASNByIdAsync(asnId);
            if (asn == null)
                return new Dictionary<string, object>();

            var analysis = new Dictionary<string, object>();
            var totalVariance = 0m;
            var itemAnalysis = new List<object>();

            foreach (var detail in asn.ASNDetails)
            {
                var poDetail = asn.PurchaseOrder.PurchaseOrderDetails
                    .FirstOrDefault(pd => pd.ItemId == detail.ItemId);

                if (poDetail != null)
                {
                    var priceVariance = detail.ActualPricePerItem - poDetail.UnitPrice;
                    var variancePercentage = poDetail.UnitPrice != 0
                        ? (priceVariance / poDetail.UnitPrice) * 100
                        : 0;

                    totalVariance += priceVariance * detail.ShippedQuantity;

                    itemAnalysis.Add(new
                    {
                        ItemCode = detail.Item.ItemCode,
                        ItemName = detail.Item.Name,
                        POPrice = poDetail.UnitPrice,
                        ActualPrice = detail.ActualPricePerItem,
                        PriceVariance = priceVariance,
                        VariancePercentage = variancePercentage,
                        Quantity = detail.ShippedQuantity,
                        TotalVariance = priceVariance * detail.ShippedQuantity,
                        WarehouseFeeRate = detail.WarehouseFeeRate,
                        WarehouseFeeAmount = detail.TotalWarehouseFee
                    });
                }
            }

            analysis["TotalVariance"] = totalVariance;
            analysis["ItemAnalysis"] = itemAnalysis;
            analysis["ASNNumber"] = asn.ASNNumber;
            analysis["PONumber"] = asn.PurchaseOrder.PONumber;
            analysis["SupplierName"] = asn.PurchaseOrder.Supplier.Name;

            return await Task.FromResult(analysis);
        }

        public async Task<IEnumerable<ASNDetail>> GetHighWarehouseFeeItemsAsync(decimal threshold = 0.05m)
        {
            var allASNs = await GetAllASNsAsync();
            var highFeeDetails = allASNs
                .SelectMany(asn => asn.ASNDetails)
                .Where(detail => detail.WarehouseFeeRate >= threshold)
                .OrderByDescending(detail => detail.WarehouseFeeRate)
                .ToList();

            return highFeeDetails;
        }

        public async Task<Dictionary<string, decimal>> GetWarehouseFeeStatisticsAsync()
        {
            var allASNs = await GetAllASNsAsync();
            var allDetails = allASNs.SelectMany(asn => asn.ASNDetails).ToList();

            var stats = new Dictionary<string, decimal>
            {
                ["TotalWarehouseFeeCollected"] = allDetails.Sum(d => d.TotalWarehouseFee),
                ["AverageWarehouseFeeRate"] = allDetails.Any() ? allDetails.Average(d => d.WarehouseFeeRate) : 0,
                ["HighFeeItemsCount"] = allDetails.Count(d => d.WarehouseFeeRate >= 0.05m),
                ["MediumFeeItemsCount"] = allDetails.Count(d => d.WarehouseFeeRate >= 0.03m && d.WarehouseFeeRate < 0.05m),
                ["LowFeeItemsCount"] = allDetails.Count(d => d.WarehouseFeeRate < 0.03m)
            };

            return await Task.FromResult(stats);
        }

        #endregion

        #region Arrival Tracking Analysis

        public async Task<Dictionary<string, object>> GetArrivalPerformanceAnalysisAsync()
        {
            var allASNs = await GetAllASNsAsync();
            var arrivedASNs = allASNs
                .Where(asn => asn.ActualArrivalDate.HasValue && asn.ExpectedArrivalDate.HasValue)
                .ToList();

            if (!arrivedASNs.Any())
                return new Dictionary<string, object>();

            var onTimeCount = arrivedASNs.Count(asn => asn.IsOnTime == true);
            var delayedCount = arrivedASNs.Count(asn => asn.IsOnTime == false);
            var totalASNs = arrivedASNs.Count;

            var analysis = new Dictionary<string, object>
            {
                ["TotalArrivedASNs"] = totalASNs,
                ["OnTimeASNs"] = onTimeCount,
                ["DelayedASNs"] = delayedCount,
                ["OnTimePercentage"] = totalASNs > 0 ? (decimal)onTimeCount / totalASNs * 100 : 0,
                ["DelayedPercentage"] = totalASNs > 0 ? (decimal)delayedCount / totalASNs * 100 : 0,
                ["AverageDeliveryDays"] = arrivedASNs.Where(asn => asn.ShipmentDurationDays.HasValue)
                    .Average(asn => asn.ShipmentDurationDays.Value),
                ["MaxDelayDays"] = arrivedASNs.Where(asn => asn.DelayDays.HasValue && asn.DelayDays > 0)
                    .DefaultIfEmpty()
                    .Max(asn => asn?.DelayDays ?? 0),
                ["AverageDelayDays"] = arrivedASNs.Where(asn => asn.DelayDays.HasValue && asn.DelayDays > 0)
                    .DefaultIfEmpty()
                    .Average(asn => asn?.DelayDays ?? 0)
            };

            return await Task.FromResult(analysis);
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetDelayedASNsAsync()
        {
            var allASNs = await GetAllASNsAsync();
            return allASNs
                .Where(asn => asn.ActualArrivalDate.HasValue &&
                            asn.ExpectedArrivalDate.HasValue &&
                            asn.IsOnTime == false)
                .OrderByDescending(asn => asn.DelayDays)
                .ToList();
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetOnTimeASNsAsync()
        {
            var allASNs = await GetAllASNsAsync();
            return allASNs
                .Where(asn => asn.ActualArrivalDate.HasValue &&
                            asn.ExpectedArrivalDate.HasValue &&
                            asn.IsOnTime == true)
                .OrderBy(asn => asn.ActualArrivalDate)
                .ToList();
        }

        #endregion

        #region Purchase Order Integration

        public async Task<IEnumerable<PurchaseOrder>> GetAvailablePurchaseOrdersAsync()
        {
            return await _purchaseOrderRepository.GetByStatusAsync(PurchaseOrderStatus.Sent);
        }

        public async Task<PurchaseOrder?> GetPurchaseOrderForASNAsync(int purchaseOrderId)
        {
            return await _purchaseOrderRepository.GetByIdWithDetailsAsync(purchaseOrderId);
        }

        public async Task<IEnumerable<PurchaseOrderDetailViewModel>> GetPODetailsForASNAsync(int purchaseOrderId)
        {
            var po = await GetPurchaseOrderForASNAsync(purchaseOrderId);
            if (po == null)
                return new List<PurchaseOrderDetailViewModel>();

            return po.PurchaseOrderDetails.Select(d => new PurchaseOrderDetailViewModel
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
            });
        }

        public async Task<bool> ValidateASNAgainstPOAsync(ASNViewModel viewModel)
        {
            var po = await GetPurchaseOrderForASNAsync(viewModel.PurchaseOrderId);
            if (po == null)
                return false;

            // Validate that all ASN items exist in PO
            foreach (var asnDetail in viewModel.Details)
            {
                var poDetail = po.PurchaseOrderDetails
                    .FirstOrDefault(pd => pd.ItemId == asnDetail.ItemId);

                if (poDetail == null)
                    return false;

                // Check if shipped quantity is reasonable (allow up to 10% variance)
                if (asnDetail.ShippedQuantity > poDetail.Quantity * 1.1m)
                    return false;
            }

            return true;
        }

        #endregion
    }
}