using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Data.Repositories;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Service implementation untuk Picking management
    /// </summary>
    public class PickingService : IPickingService
    {
        private readonly IPickingRepository _pickingRepository;
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IItemRepository _itemRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PickingService> _logger;

        public PickingService(
            IPickingRepository pickingRepository,
            ISalesOrderRepository salesOrderRepository,
            IInventoryRepository inventoryRepository,
            ILocationRepository locationRepository,
            IItemRepository itemRepository,
            ApplicationDbContext context,
            ILogger<PickingService> logger)
        {
            _pickingRepository = pickingRepository;
            _salesOrderRepository = salesOrderRepository;
            _inventoryRepository = inventoryRepository;
            _locationRepository = locationRepository;
            _itemRepository = itemRepository;
            _context = context;
            _logger = logger;
        }

        #region Basic CRUD Operations

        public async Task<IEnumerable<Picking>> GetAllPickingsAsync()
        {
            return await _pickingRepository.GetAllWithDetailsAsync();
        }

        public async Task<Picking?> GetPickingByIdAsync(int id)
        {
            return await _pickingRepository.GetByIdWithDetailsAsync(id);
        }

        public async Task<Picking?> GetPickingByNumberAsync(string pickingNumber)
        {
            return await _pickingRepository.GetByPickingNumberAsync(pickingNumber);
        }

        public async Task<bool> DeletePickingAsync(int id)
        {
            var picking = await GetPickingByIdAsync(id);
            if (picking == null || picking.Status == Constants.PICKING_STATUS_COMPLETED)
                return false;

            // Delete picking details first (because of Restrict delete behavior)
            foreach (var detail in picking.PickingDetails.ToList())
            {
                _context.PickingDetails.Remove(detail);
            }

            await _context.SaveChangesAsync();

            // Then delete the picking
            return await _pickingRepository.DeleteAsync(id);
        }

        #endregion

        #region Generate & Create Picking

        public async Task<Picking> GeneratePickingListAsync(int salesOrderId)
        {
            _logger.LogInformation("Generating picking list for Sales Order {SalesOrderId}", salesOrderId);

            // Validate Sales Order
            var salesOrder = await _salesOrderRepository.GetByIdWithDetailsAsync(salesOrderId);
            if (salesOrder == null)
                throw new ArgumentException($"Sales Order {salesOrderId} not found");

            if (salesOrder.Status != Constants.SO_STATUS_CONFIRMED)
                throw new InvalidOperationException($"Sales Order must be Confirmed to generate picking list. Current status: {salesOrder.Status}");

            // Check if picking already exists
            var existingPicking = await _pickingRepository.GetBySalesOrderIdAsync(salesOrderId);
            if (existingPicking.Any(p => p.Status != Constants.PICKING_STATUS_CANCELLED))
                throw new InvalidOperationException("Active picking list already exists for this Sales Order");

            // Pre-validate inventory availability for all items
            foreach (var soDetail in salesOrder.SalesOrderDetails)
            {
                var inventories = await _inventoryRepository.GetByItemIdAsync(soDetail.ItemId);
                var availableInventories = inventories
                    .Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE && inv.Quantity > 0)
                    .ToList();

                var totalAvailable = availableInventories.Sum(inv => inv.Quantity);
                if (totalAvailable < soDetail.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient inventory for Item {soDetail.Item?.ItemCode} ({soDetail.Item?.Name}). " +
                        $"Required: {soDetail.Quantity}, Available: {totalAvailable}");
                }

                _logger.LogInformation("Inventory validation passed for Item {ItemId}: Required={Required}, Available={Available}",
                    soDetail.ItemId, soDetail.Quantity, totalAvailable);
            }

            // Create Picking document
            var picking = new Picking
            {
                PickingNumber = await _pickingRepository.GeneratePickingNumberAsync(),
                SalesOrderId = salesOrderId,
                PickingDate = DateTime.Today,
                Status = Constants.PICKING_STATUS_PENDING,
                CreatedDate = DateTime.Now
            };

            // Generate picking details with location suggestions
            var pickingDetails = await GeneratePickingDetailsAsync(salesOrderId);
            
            foreach (var detailVM in pickingDetails)
            {
                var detail = new PickingDetail
                {
                    SalesOrderDetailId = detailVM.SalesOrderDetailId,
                    ItemId = detailVM.ItemId,
                    LocationId = detailVM.LocationId, // Suggested location (FIFO)
                    QuantityRequired = detailVM.QuantityRequired,
                    QuantityPicked = 0,
                    RemainingQuantity = detailVM.QuantityRequired, // Initially same as required
                    Status = Constants.PICKING_DETAIL_STATUS_PENDING,
                    CreatedDate = DateTime.Now
                };
                
                // Ensure RemainingQuantity is calculated correctly
                detail.CalculateRemaining();

                picking.PickingDetails.Add(detail);
            }

            // Save picking
            var createdPicking = await _pickingRepository.CreateWithDetailsAsync(picking);

            // Update SO status to Picking
            await _salesOrderRepository.UpdateStatusAsync(salesOrderId, SalesOrderStatus.Picking);

            _logger.LogInformation("Picking list generated: {PickingNumber} for SO: {SONumber}", 
                picking.PickingNumber, salesOrder.SONumber);

            return createdPicking;
        }

        public async Task<List<PickingDetailViewModel>> GeneratePickingDetailsAsync(int salesOrderId)
        {
            var salesOrder = await _salesOrderRepository.GetByIdWithDetailsAsync(salesOrderId);
            if (salesOrder == null)
                return new List<PickingDetailViewModel>();

            var details = new List<PickingDetailViewModel>();

            foreach (var soDetail in salesOrder.SalesOrderDetails)
            {
                _logger.LogInformation("Generating picking details for Item {ItemId} ({ItemName}), Required Qty: {Quantity}",
                    soDetail.ItemId, soDetail.Item?.Name, soDetail.Quantity);

                // Get location suggestions (FIFO)
                var suggestions = await GetPickingSuggestionsAsync(soDetail.ItemId, soDetail.Quantity);
                var suggestedLocations = suggestions.ToList();

                if (!suggestedLocations.Any())
                {
                    _logger.LogWarning("No available locations found for Item {ItemId} ({ItemName})", 
                        soDetail.ItemId, soDetail.Item?.Name);
                    continue;
                }

                _logger.LogInformation("Found {Count} location suggestions for Item {ItemId}: {Locations}",
                    suggestedLocations.Count, soDetail.ItemId, 
                    string.Join(", ", suggestedLocations.Select(s => $"{s.LocationCode}({s.AvailableQuantity})")));

                // Create picking details for each suggested location
                var remainingQty = soDetail.Quantity;
                
                foreach (var suggestion in suggestedLocations.OrderBy(s => s.SuggestionOrder))
                {
                    if (remainingQty <= 0) break;

                    var qtyFromThisLocation = Math.Min(remainingQty, suggestion.AvailableQuantity);

                    details.Add(new PickingDetailViewModel
                    {
                        SalesOrderDetailId = soDetail.Id,
                        ItemId = soDetail.ItemId,
                        ItemCode = soDetail.Item.ItemCode,
                        ItemName = soDetail.Item.Name,
                        ItemUnit = soDetail.Item.Unit,
                        LocationId = suggestion.LocationId,
                        LocationCode = suggestion.LocationCode,
                        LocationName = suggestion.LocationName,
                        QuantityRequired = qtyFromThisLocation,
                        QuantityToPick = 0,
                        QuantityPicked = 0,
                        RemainingQuantity = qtyFromThisLocation,
                        AvailableQuantity = suggestion.AvailableQuantity,
                        Status = Constants.PICKING_DETAIL_STATUS_PENDING
                    });

                    remainingQty -= qtyFromThisLocation;
                }

                if (remainingQty > 0)
                {
                    _logger.LogWarning("Insufficient stock for Item {ItemId}. Short by {Quantity}", 
                        soDetail.ItemId, remainingQty);
                }
            }

            return details;
        }

        #endregion

        #region Location Suggestions (FIFO)

        public async Task<IEnumerable<LocationSuggestion>> GetPickingSuggestionsAsync(int itemId, int quantityRequired)
        {
            _logger.LogInformation("Getting picking suggestions for Item {ItemId}, Required Qty: {Quantity}", 
                itemId, quantityRequired);

            // Get all available inventory for this item, ordered by LastUpdated (FIFO)
            var inventories = await _inventoryRepository.GetByItemIdAsync(itemId);
            
            var availableInventories = inventories
                .Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE && inv.Quantity > 0)
                .OrderBy(inv => inv.LastUpdated) // FIFO - oldest first
                .ToList();

            _logger.LogInformation("Found {Count} available inventories for Item {ItemId}: {Inventories}",
                availableInventories.Count, itemId,
                string.Join(", ", availableInventories.Select(inv => $"{inv.Location.Code}({inv.Quantity})")));

            var suggestions = new List<LocationSuggestion>();
            var suggestionOrder = 1;

            foreach (var inventory in availableInventories)
            {
                suggestions.Add(new LocationSuggestion
                {
                    LocationId = inventory.LocationId,
                    LocationCode = inventory.Location.Code,
                    LocationName = inventory.Location.Name,
                    LocationDisplay = inventory.Location.DisplayName,
                    AvailableQuantity = inventory.Quantity,
                    LastUpdated = inventory.LastUpdated,
                    IsSuggested = true,
                    SuggestionOrder = suggestionOrder++
                });

                // Stop suggesting when we have enough locations to fulfill the quantity
                var totalSuggested = suggestions.Sum(s => s.AvailableQuantity);
                if (totalSuggested >= quantityRequired)
                    break;
            }

            _logger.LogInformation("Generated {Count} location suggestions for Item {ItemId}, Total Available: {TotalQty}",
                suggestions.Count, itemId, suggestions.Sum(s => s.AvailableQuantity));

            return suggestions;
        }

        #endregion

        #region Process Picking Operations

        public async Task<(bool Success, string ErrorMessage)> ProcessPickingAsync(PickingDetailViewModel detail)
        {
            try
            {
                _logger.LogInformation("Processing picking for PickingDetail {Id}, Item {ItemId}, Location {LocationId}, Qty: {Qty}",
                    detail.Id, detail.ItemId, detail.LocationId, detail.QuantityToPick);

                // Get complete picking detail from database to ensure we have the latest data
                var pickingDetail = await _context.PickingDetails
                    .FirstOrDefaultAsync(pd => pd.Id == detail.Id);
                    
                if (pickingDetail == null)
                {
                    var errorMsg = $"Picking detail {detail.Id} not found in database";
                    _logger.LogError("DATABASE CHECK FAILED: {ErrorMessage}", errorMsg);
                    return (false, errorMsg);
                }

                // Update detail with latest data from database
                detail.QuantityRequired = pickingDetail.QuantityRequired;
                detail.QuantityPicked = pickingDetail.QuantityPicked;
                detail.RemainingQuantity = pickingDetail.RemainingQuantity;
                detail.Status = pickingDetail.Status;

                _logger.LogInformation("UPDATED DETAIL FROM DB - Required: {Required}, Picked: {Picked}, Remaining: {Remaining}, Status: {Status}",
                    detail.QuantityRequired, detail.QuantityPicked, detail.RemainingQuantity, detail.Status);

                // Pre-check inventory availability before validation
                var preCheckInventory = await _inventoryRepository.GetByItemAndLocationAsync(detail.ItemId, detail.LocationId);
                if (preCheckInventory == null)
                {
                    var errorMsg = $"No inventory found for Item {detail.ItemId} at Location {detail.LocationId}";
                    _logger.LogError("PRE-CHECK FAILED: {ErrorMessage}", errorMsg);
                    return (false, errorMsg);
                }

                _logger.LogInformation("PRE-CHECK: Found inventory - ItemId={ItemId}, LocationId={LocationId}, Qty={Qty}, Status={Status}",
                    preCheckInventory.ItemId, preCheckInventory.LocationId, preCheckInventory.Quantity, preCheckInventory.Status);

                // Validate with detailed error message
                var validationResult = await ValidatePickingWithDetailsAsync(detail);
                if (!validationResult.Success)
                {
                    _logger.LogWarning("Picking validation failed: {ErrorMessage}", validationResult.ErrorMessage);
                    return (false, validationResult.ErrorMessage);
                }

                // Get picking detail with Picking relationship
                var pickingDetailWithPicking = await _context.PickingDetails
                    .Include(pd => pd.Picking)
                    .FirstOrDefaultAsync(pd => pd.Id == detail.Id);

                if (pickingDetailWithPicking == null)
                {
                    _logger.LogError("Picking detail {Id} not found", detail.Id);
                    return (false, "Picking detail not found");
                }

                // Update picking detail
                pickingDetailWithPicking.UpdatePickedQuantity(detail.QuantityToPick);
                pickingDetailWithPicking.Notes = detail.Notes;
                
                _context.PickingDetails.Update(pickingDetailWithPicking);

                // Reduce inventory with detailed error handling
                var inventory = await _inventoryRepository.GetByItemAndLocationAsync(detail.ItemId, detail.LocationId);
                if (inventory == null)
                {
                    _logger.LogError("Inventory not found for Item {ItemId} at Location {LocationId} during processing",
                        detail.ItemId, detail.LocationId);
                    return (false, "Inventory not found during processing");
                }

                _logger.LogInformation("Attempting to reduce stock: ItemId={ItemId}, LocationId={LocationId}, CurrentQty={CurrentQty}, ReduceBy={ReduceBy}",
                    inventory.ItemId, inventory.LocationId, inventory.Quantity, detail.QuantityToPick);

                if (!inventory.ReduceStock(detail.QuantityToPick))
                {
                    _logger.LogError("Failed to reduce stock: ItemId={ItemId}, LocationId={LocationId}, CurrentQty={CurrentQty}, ReduceBy={ReduceBy}",
                        inventory.ItemId, inventory.LocationId, inventory.Quantity, detail.QuantityToPick);
                    return (false, "Failed to reduce stock");
                }

                await _inventoryRepository.UpdateAsync(inventory);

                // Update location capacity
                await _locationRepository.UpdateCurrentCapacityAsync(detail.LocationId);

                // Update picking status to InProgress if it was Pending
                if (pickingDetailWithPicking.Picking.Status == Constants.PICKING_STATUS_PENDING)
                {
                    await _pickingRepository.UpdateStatusAsync(pickingDetailWithPicking.PickingId, Constants.PICKING_STATUS_IN_PROGRESS);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Picking processed successfully for PickingDetail {Id}", detail.Id);
                return (true, "Picking processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing picking for PickingDetail {Id}", detail.Id);
                return (false, $"Error processing picking: {ex.Message}");
            }
        }

        public async Task<bool> ProcessBulkPickingAsync(IEnumerable<PickingDetailViewModel> details)
        {
            var allSuccess = true;

            foreach (var detail in details)
            {
                if (detail.QuantityToPick > 0)
                {
                    var result = await ProcessPickingAsync(detail);
                    if (!result.Success)
                    {
                        allSuccess = false;
                        _logger.LogWarning("Failed to process picking for detail {Id}: {ErrorMessage}", detail.Id, result.ErrorMessage);
                    }
                }
            }

            return allSuccess;
        }

        public async Task<bool> CompletePickingAsync(int pickingId)
        {
            try
            {
                var picking = await GetPickingByIdAsync(pickingId);
                if (picking == null)
                    return false;

                if (!await CanCompletePickingAsync(pickingId))
                {
                    _logger.LogWarning("Cannot complete picking {PickingId} - validation failed", pickingId);
                    return false;
                }

                // Update picking status
                await _pickingRepository.CompletePickingAsync(pickingId);

                // Update Sales Order status to ReadyToShip if all picked, or keep as Picking if partial
                var isFullyPicked = picking.IsFullyPicked;
                var newSOStatus = isFullyPicked 
                    ? SalesOrderStatus.ReadyToShip 
                    : SalesOrderStatus.Picking;

                await _salesOrderRepository.UpdateStatusAsync(picking.SalesOrderId, newSOStatus);

                _logger.LogInformation("Picking {PickingNumber} completed. SO status updated to {Status}",
                    picking.PickingNumber, newSOStatus);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing picking {PickingId}", pickingId);
                return false;
            }
        }

        public async Task<bool> CancelPickingAsync(int pickingId)
        {
            try
            {
                var picking = await GetPickingByIdAsync(pickingId);
                if (picking == null)
                    return false;

                // Reverse any picked inventory (return stock)
                foreach (var detail in picking.PickingDetails.Where(d => d.QuantityPicked > 0))
                {
                    var inventory = await _inventoryRepository.GetByItemAndLocationAsync(detail.ItemId, detail.LocationId);
                    if (inventory != null)
                    {
                        inventory.AddStock(detail.QuantityPicked, inventory.LastCostPrice);
                        await _inventoryRepository.UpdateAsync(inventory);
                        await _locationRepository.UpdateCurrentCapacityAsync(detail.LocationId);
                    }
                }

                // Update picking status
                await _pickingRepository.CancelPickingAsync(pickingId);

                // Update SO status back to Confirmed
                await _salesOrderRepository.UpdateStatusAsync(picking.SalesOrderId, SalesOrderStatus.Confirmed);

                _logger.LogInformation("Picking {PickingNumber} cancelled and stock restored", picking.PickingNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling picking {PickingId}", pickingId);
                return false;
            }
        }

        #endregion

        #region Query Operations

        public async Task<IEnumerable<Picking>> GetPickingsBySalesOrderAsync(int salesOrderId)
        {
            return await _pickingRepository.GetBySalesOrderIdAsync(salesOrderId);
        }

        public async Task<IEnumerable<Picking>> GetPickingsByStatusAsync(string status)
        {
            return await _pickingRepository.GetByStatusAsync(status);
        }

        public async Task<IEnumerable<Picking>> GetPendingPickingsAsync()
        {
            return await _pickingRepository.GetPendingPickingsAsync();
        }

        public async Task<IEnumerable<Picking>> GetInProgressPickingsAsync()
        {
            return await _pickingRepository.GetInProgressPickingsAsync();
        }

        public async Task<IEnumerable<PickingListViewModel>> GetPickingListSummaryAsync()
        {
            var pickings = await GetAllPickingsAsync();

            return pickings.Select(p => new PickingListViewModel
            {
                Id = p.Id,
                PickingNumber = p.PickingNumber,
                SONumber = p.SONumber,
                CustomerName = p.CustomerName,
                PickingDate = p.PickingDate,
                Status = p.Status,
                StatusIndonesia = p.StatusIndonesia,
                StatusCssClass = p.StatusCssClass,
                CompletionPercentage = p.CompletionPercentage,
                TotalQuantityRequired = p.TotalQuantityRequired,
                TotalQuantityPicked = p.TotalQuantityPicked,
                TotalItemTypes = p.TotalItemTypes
            }).ToList();
        }

        #endregion

        #region ViewModel Operations

        public async Task<PickingViewModel> GetPickingViewModelAsync(int? pickingId = null)
        {
            var viewModel = new PickingViewModel
            {
                PickingDate = DateTime.Today,
                Status = Constants.PICKING_STATUS_PENDING
            };

            if (pickingId.HasValue)
            {
                var picking = await GetPickingByIdAsync(pickingId.Value);
                if (picking != null)
                {
                    viewModel.Id = picking.Id;
                    viewModel.PickingNumber = picking.PickingNumber;
                    viewModel.SalesOrderId = picking.SalesOrderId;
                    viewModel.SONumber = picking.SONumber;
                    viewModel.CustomerName = picking.CustomerName;
                    viewModel.PickingDate = picking.PickingDate;
                    viewModel.CompletedDate = picking.CompletedDate;
                    viewModel.Status = picking.Status;
                    viewModel.StatusIndonesia = picking.StatusIndonesia;
                    viewModel.Notes = picking.Notes;
                    viewModel.TotalQuantityRequired = picking.TotalQuantityRequired;
                    viewModel.TotalQuantityPicked = picking.TotalQuantityPicked;
                    viewModel.TotalQuantityRemaining = picking.TotalQuantityRemaining;
                    viewModel.CompletionPercentage = picking.CompletionPercentage;
                    viewModel.TotalItemTypes = picking.TotalItemTypes;
                    viewModel.TotalLocationsUsed = picking.TotalLocationsUsed;
                    viewModel.IsFullyPicked = picking.IsFullyPicked;
                    viewModel.HasShortItems = picking.HasShortItems;
                    viewModel.CanBeEdited = picking.CanBeEdited;
                    viewModel.CanBeCompleted = picking.CanBeCompleted;
                    viewModel.CanBeCancelled = picking.CanBeCancelled;

                    // Ensure all RemainingQuantity are calculated correctly before mapping
                    foreach (var detail in picking.PickingDetails)
                    {
                        detail.CalculateRemaining();
                    }

                    viewModel.PickingDetails = picking.PickingDetails.Select(d => new PickingDetailViewModel
                    {
                        Id = d.Id,
                        PickingId = d.PickingId,
                        SalesOrderDetailId = d.SalesOrderDetailId,
                        ItemId = d.ItemId,
                        ItemCode = d.ItemCode,
                        ItemName = d.ItemName,
                        ItemUnit = d.ItemUnit,
                        LocationId = d.LocationId,
                        LocationCode = d.LocationCode,
                        QuantityRequired = d.QuantityRequired,
                        QuantityPicked = d.QuantityPicked,
                        QuantityToPick = 0, // User input field, starts at 0
                        RemainingQuantity = d.QuantityRequired - d.QuantityPicked, // Recalculate to ensure accuracy
                        Status = d.Status,
                        StatusIndonesia = d.StatusIndonesia,
                        Notes = d.Notes,
                        PickedPercentage = d.PickedPercentage,
                        IsFullyPicked = d.IsFullyPicked,
                        IsPartialPicked = d.IsPartialPicked
                    }).ToList();
                }
            }

            return await PopulatePickingViewModelAsync(viewModel);
        }

        public async Task<PickingViewModel> PopulatePickingViewModelAsync(PickingViewModel viewModel)
        {
            // Get confirmed sales orders
            var confirmedSOs = await _salesOrderRepository.GetByStatusAsync(SalesOrderStatus.Confirmed);
            viewModel.SalesOrders = new SelectList(confirmedSOs, "Id", "SONumber", viewModel.SalesOrderId);

            // Get all active locations
            var locations = await _locationRepository.GetActiveLocationsAsync();
            viewModel.Locations = new SelectList(locations, "Id", "DisplayName");

            return viewModel;
        }

        #endregion

        #region Validation

        public async Task<bool> ValidatePickingAsync(PickingDetailViewModel detail)
        {
            var result = await ValidatePickingWithDetailsAsync(detail);
            return result.Success;
        }

        public async Task<(bool Success, string ErrorMessage)> ValidatePickingWithDetailsAsync(PickingDetailViewModel detail)
        {
            _logger.LogInformation("=== PICKING VALIDATION DEBUG ===");
            _logger.LogInformation("Input Detail - ItemId: {ItemId}, LocationId: {LocationId}", detail.ItemId, detail.LocationId);
            _logger.LogInformation("Input Detail - QtyToPick: {QtyToPick}, QtyRequired: {QtyRequired}, QtyPicked: {QtyPicked}", 
                detail.QuantityToPick, detail.QuantityRequired, detail.QuantityPicked);
            _logger.LogInformation("Input Detail - RemainingQuantity: {RemainingQty}", detail.RemainingQuantity);

            if (detail.QuantityToPick <= 0)
            {
                var errorMsg = $"Invalid quantity to pick: {detail.QuantityToPick}";
                _logger.LogWarning(errorMsg);
                return (false, errorMsg);
            }

            // Recalculate RemainingQuantity to ensure it's correct
            var calculatedRemaining = detail.QuantityRequired - detail.QuantityPicked;
            _logger.LogInformation("CALCULATED RemainingQuantity: {CalculatedRemaining} (Required: {Required}, Picked: {Picked})", 
                calculatedRemaining, detail.QuantityRequired, detail.QuantityPicked);

            // Check if there's a discrepancy
            if (detail.RemainingQuantity != calculatedRemaining)
            {
                _logger.LogWarning("DISCREPANCY DETECTED - UI Remaining: {UIRemaining}, Calculated: {Calculated}", 
                    detail.RemainingQuantity, calculatedRemaining);
            }

            if (detail.QuantityToPick > calculatedRemaining)
            {
                var errorMsg = $"Quantity to pick ({detail.QuantityToPick}) exceeds remaining quantity ({calculatedRemaining})";
                _logger.LogError("VALIDATION FAILED: {ErrorMessage}", errorMsg);
                return (false, errorMsg);
            }

            // Check inventory availability with detailed logging
            var inventory = await _inventoryRepository.GetByItemAndLocationAsync(detail.ItemId, detail.LocationId);
            if (inventory == null)
            {
                var errorMsg = $"No inventory found for Item {detail.ItemId} at Location {detail.LocationId}";
                _logger.LogError(errorMsg);
                return (false, errorMsg);
            }

            _logger.LogInformation("Found inventory: ItemId={ItemId}, LocationId={LocationId}, AvailableQty={AvailableQty}, Status={Status}",
                inventory.ItemId, inventory.LocationId, inventory.Quantity, inventory.Status);

            if (inventory.Quantity < detail.QuantityToPick)
            {
                var errorMsg = $"Insufficient inventory: Available {inventory.Quantity}, Required {detail.QuantityToPick}";
                _logger.LogError(errorMsg);
                return (false, errorMsg);
            }

            if (inventory.Status != Constants.INVENTORY_STATUS_AVAILABLE)
            {
                var errorMsg = $"Inventory not available for picking: Status is '{inventory.Status}', expected 'Available'";
                _logger.LogError(errorMsg);
                return (false, errorMsg);
            }

            _logger.LogInformation("Picking validation passed for Item {ItemId} at Location {LocationId}", 
                detail.ItemId, detail.LocationId);
            return (true, "Validation passed");
        }

        public async Task<bool> CanGeneratePickingAsync(int salesOrderId)
        {
            var salesOrder = await _salesOrderRepository.GetByIdAsync(salesOrderId);
            if (salesOrder == null)
                return false;

            if (salesOrder.Status != Constants.SO_STATUS_CONFIRMED)
                return false;

            // Check if picking already exists
            var existingPicking = await _pickingRepository.ExistsForSalesOrderAsync(salesOrderId);
            if (existingPicking)
                return false;

            return true;
        }

        public async Task<bool> CanCompletePickingAsync(int pickingId)
        {
            var picking = await GetPickingByIdAsync(pickingId);
            if (picking == null)
                return false;

            // Must have at least some items picked
            if (picking.TotalQuantityPicked == 0)
                return false;

            // Cannot complete if cancelled or already completed
            if (picking.Status == Constants.PICKING_STATUS_CANCELLED 
                || picking.Status == Constants.PICKING_STATUS_COMPLETED)
                return false;

            return true;
        }

        #endregion

        #region Status Helpers

        public async Task<bool> UpdatePickingStatusAsync(int pickingId, string status)
        {
            return await _pickingRepository.UpdateStatusAsync(pickingId, status);
        }

        #endregion
    }
}
