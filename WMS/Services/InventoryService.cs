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
    /// Service implementation untuk Inventory management dan Putaway operations
    /// Menangani business logic untuk inventory tracking dan warehouse operations
    /// </summary>
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IASNRepository _asnRepository;
        private readonly IItemRepository _itemRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<InventoryService> _logger;
        private readonly ApplicationDbContext _context;

        public InventoryService(
            IInventoryRepository inventoryRepository,
            IASNRepository asnRepository,
            IItemRepository itemRepository,
            ILocationRepository locationRepository,
            ICurrentUserService currentUserService,
            ILogger<InventoryService> logger,
            ApplicationDbContext context)
        {
            _inventoryRepository = inventoryRepository;
            _asnRepository = asnRepository;
            _itemRepository = itemRepository;
            _locationRepository = locationRepository;
            _currentUserService = currentUserService;
            _logger = logger;
            _context = context;
        }

        #region Basic CRUD Operations

        public async Task<IEnumerable<Inventory>> GetAllInventoriesAsync()
        {
            return await _inventoryRepository.GetAllWithDetailsAsync();
        }

        public async Task<Inventory?> GetInventoryByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting inventory by ID: {InventoryId}", id);
                
                var inventory = await _inventoryRepository.GetByIdWithDetailsAsync(id);
                
                if (inventory == null)
                {
                    _logger.LogWarning("Inventory not found for ID: {InventoryId}", id);
                }
                else
                {
                    _logger.LogInformation("Successfully retrieved inventory {InventoryId} for company {CompanyId}", 
                        id, inventory.CompanyId);
                }
                
                return inventory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory by ID: {InventoryId}", id);
                throw;
            }
        }

        public async Task<Inventory> CreateInventoryAsync(InventoryViewModel viewModel)
        {
            // Validate business rules
            if (!await ValidateInventoryAsync(viewModel))
                throw new InvalidOperationException("Inventory validation failed");

            // Check if inventory already exists at the location
            var existingInventory = await _inventoryRepository.GetByItemAndLocationAsync(viewModel.ItemId, viewModel.LocationId);
            if (existingInventory != null)
            {
                throw new InvalidOperationException("Inventory already exists for this item at this location. Use update instead.");
            }

            var inventory = new Inventory
            {
                ItemId = viewModel.ItemId,
                LocationId = viewModel.LocationId,
                Quantity = viewModel.Quantity,
                LastCostPrice = viewModel.LastCostPrice,
                Status = viewModel.Status,
                Notes = viewModel.Notes,
                SourceReference = viewModel.SourceReference,
                LastUpdated = DateTime.Now
            };

            var createdInventory = await _inventoryRepository.AddAsync(inventory);
            
            // Update location capacity after creating inventory
            await _locationRepository.UpdateCurrentCapacityAsync(createdInventory.LocationId);
            
            _logger.LogInformation("Created inventory {InventoryId} and updated location {LocationId} capacity", 
                createdInventory.Id, createdInventory.LocationId);

            return createdInventory;
        }

        public async Task<Inventory> UpdateInventoryAsync(int id, InventoryViewModel viewModel)
        {
            var existingInventory = await _inventoryRepository.GetByIdAsync(id);
            if (existingInventory == null)
                throw new ArgumentException($"Inventory with ID {id} not found");

            // Store original location for capacity update
            var originalLocationId = existingInventory.LocationId;

            // Update properties
            existingInventory.Quantity = viewModel.Quantity;
            existingInventory.LastCostPrice = viewModel.LastCostPrice;
            existingInventory.Status = viewModel.Status;
            existingInventory.Notes = viewModel.Notes;
            existingInventory.LastUpdated = DateTime.Now;

            var updatedInventory = await _inventoryRepository.UpdateAsync(existingInventory);
            
            // Update location capacity after updating inventory
            await _locationRepository.UpdateCurrentCapacityAsync(originalLocationId);
            
            _logger.LogInformation("Updated inventory {InventoryId} and updated location {LocationId} capacity", 
                updatedInventory.Id, originalLocationId);

            return updatedInventory;
        }

        public async Task<bool> DeleteInventoryAsync(int id)
        {
            try
            {
                // Get inventory details before deleting to update location capacity
                var inventory = await _inventoryRepository.GetByIdAsync(id);
                if (inventory == null)
                {
                    _logger.LogWarning("Inventory with ID {InventoryId} not found for deletion", id);
                    return false;
                }

                var locationId = inventory.LocationId;
                var result = await _inventoryRepository.DeleteAsync(id);
                
                if (result)
                {
                    // Update location capacity after deleting inventory
                    await _locationRepository.UpdateCurrentCapacityAsync(locationId);
                    
                    _logger.LogInformation("Deleted inventory {InventoryId} and updated location {LocationId} capacity", 
                        id, locationId);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting inventory with ID {InventoryId}", id);
                return false;
            }
        }

        #endregion

        #region Putaway Operations

        public async Task<IEnumerable<AdvancedShippingNotice>> GetASNsReadyForPutawayAsync()
        {
            return await _asnRepository.GetProcessedASNsAsync();
        }

        public async Task<IEnumerable<ASNDetail>> GetASNDetailsForPutawayAsync(int asnId)
        {
            return await _asnRepository.GetASNDetailsForPutawayAsync(asnId);
        }

        public async Task<PutawayViewModel> GetPutawayViewModelAsync(int asnId)
        {
            try
            {
                _logger.LogInformation("Getting putaway view model for ASN {ASNId}", asnId);

                var asn = await _asnRepository.GetByIdWithDetailsAsync(asnId);
                if (asn == null)
                {
                    _logger.LogError("ASN with ID {ASNId} not found", asnId);
                    throw new ArgumentException($"ASN with ID {asnId} not found");
                }

                _logger.LogInformation("Found ASN {ASNId}: {ASNNumber}", asnId, asn.ASNNumber);

                var asnDetails = await GetASNDetailsForPutawayAsync(asnId);
                _logger.LogInformation("Found {Count} ASN details for putaway", asnDetails.Count());

                if (!asnDetails.Any())
                {
                    _logger.LogWarning("No ASN details found for putaway for ASN {ASNId}", asnId);
                    throw new InvalidOperationException("No items available for putaway in this ASN");
                }

                var availableLocations = await GetAvailableLocationsForPutawayAsync();
                _logger.LogInformation("Found {Count} available locations for putaway", availableLocations.Count());

                if (!availableLocations.Any())
                {
                    _logger.LogWarning("No available locations found for putaway");
                    throw new InvalidOperationException("No available locations for putaway. Please create locations first.");
                }

                // Create enhanced location dropdown items
                var locationDropdownItems = availableLocations.Select(location => new LocationDropdownItem
                {
                    Id = location.Id,
                    Code = location.Code,
                    Name = location.Name,
                    MaxCapacity = location.MaxCapacity,
                    CurrentCapacity = location.CurrentCapacity,
                    AvailableCapacity = location.AvailableCapacity,
                    DisplayText = location.DropdownDisplayText,
                    CssClass = location.DropdownCssClass,
                    StatusText = location.DropdownStatusText,
                    CanAccommodate = true, // All locations in this list can accommodate
                    IsFull = location.IsFull,
                    CapacityPercentage = location.CapacityPercentage
                }).ToList();

                // Create tasks for parallel execution of suggested location lookups
                var suggestedLocationTasks = asnDetails.Select(async detail => 
                {
                    try
                    {
                        var suggestedLocationId = await GetSuggestedLocationIdAsync(detail.ItemId, detail.RemainingQuantity);
                        return new PutawayDetailViewModel
                        {
                            ASNDetailId = detail.Id,
                            ItemId = detail.ItemId,
                            ItemCode = detail.Item?.ItemCode ?? "",
                            ItemName = detail.Item?.Name ?? "",
                            ItemUnit = detail.Item?.Unit ?? "",
                            TotalQuantity = detail.ShippedQuantity,
                            RemainingQuantity = detail.RemainingQuantity,
                            ActualPricePerItem = detail.ActualPricePerItem,
                            SuggestedLocationId = suggestedLocationId
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating putaway detail view model for ASN detail {ASNDetailId}", detail.Id);
                        throw;
                    }
                });

                // Wait for all tasks to complete
                var putawayDetails = await Task.WhenAll(suggestedLocationTasks);
                _logger.LogInformation("Created {Count} putaway detail view models", putawayDetails.Length);

                var viewModel = new PutawayViewModel
                {
                    ASNId = asnId,
                    ASNNumber = asn.ASNNumber,
                    PONumber = asn.PurchaseOrder?.PONumber ?? "",
                    SupplierName = asn.PurchaseOrder?.Supplier?.Name ?? "",
                    ShipmentDate = asn.ShipmentDate,
                    ProcessedDate = asn.ActualArrivalDate,
                    AvailableLocations = new SelectList(availableLocations, "Id", "DisplayName"),
                    LocationDropdownItems = locationDropdownItems,
                    PutawayDetails = putawayDetails.ToList()
                };

                _logger.LogInformation("Successfully created putaway view model for ASN {ASNId}", asnId);
                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting putaway view model for ASN {ASNId}", asnId);
                throw;
            }
        }

        public async Task<bool> ProcessPutawayAsync(PutawayDetailViewModel putawayDetail)
        {
            try
            {
                _logger.LogInformation("Starting putaway process for ASN Detail {ASNDetailId}, ItemId: {ItemId}, Quantity: {Quantity}, Location: {LocationId}",
                    putawayDetail.ASNDetailId, putawayDetail.ItemId, putawayDetail.QuantityToPutaway, putawayDetail.LocationId);

                // Debug CompanyId
                var companyId = _currentUserService.CompanyId;
                _logger.LogInformation("Current CompanyId: {CompanyId}", companyId);
                if (!companyId.HasValue)
                {
                    _logger.LogError("CompanyId is null - this will cause validation to fail");
                    return false;
                }

                // Debug ASN Detail lookup
                _logger.LogInformation("Looking up ASN Detail {ASNDetailId} for company {CompanyId}", putawayDetail.ASNDetailId, companyId);
                var debugAsnDetail = await _asnRepository.GetASNDetailByIdAsync(putawayDetail.ASNDetailId);
                if (debugAsnDetail == null)
                {
                    _logger.LogError("ASN Detail {ASNDetailId} not found for company {CompanyId}. This will cause validation to fail.", 
                        putawayDetail.ASNDetailId, companyId);
                    
                    // Try to find without company filtering for debugging
                    var debugAsnDetailNoFilter = await _context.ASNDetails
                        .Include(d => d.Item)
                        .Include(d => d.ASN)
                        .FirstOrDefaultAsync(d => d.Id == putawayDetail.ASNDetailId);
                    
                    if (debugAsnDetailNoFilter != null)
                    {
                        _logger.LogWarning("ASN Detail found but different CompanyId - ASN Detail CompanyId: {ASNDetailCompanyId}, Current CompanyId: {CurrentCompanyId}", 
                            debugAsnDetailNoFilter.CompanyId, companyId);
                    }
                    else
                    {
                        _logger.LogWarning("ASN Detail not found in database at all - ASNDetailId: {ASNDetailId}", putawayDetail.ASNDetailId);
                    }
                    
                    return false;
                }
                _logger.LogInformation("ASN Detail found: CompanyId={CompanyId}, ShippedQuantity={ShippedQuantity}, RemainingQuantity={RemainingQuantity}", 
                    debugAsnDetail.CompanyId, debugAsnDetail.ShippedQuantity, debugAsnDetail.RemainingQuantity);

                // Validate putaway request
                _logger.LogInformation("Validating putaway request...");
                if (!await ValidatePutawayAsync(putawayDetail))
                {
                    _logger.LogWarning("Putaway validation failed for ASN Detail {ASNDetailId}", putawayDetail.ASNDetailId);
                    return false;
                }
                _logger.LogInformation("Putaway validation passed");

                // Get ASN detail information
                _logger.LogInformation("Getting ASN detail for ID {ASNDetailId}", putawayDetail.ASNDetailId);
                var asnDetail = await _asnRepository.GetASNDetailByIdAsync(putawayDetail.ASNDetailId);
                if (asnDetail == null)
                {
                    _logger.LogError("ASN Detail {ASNDetailId} not found", putawayDetail.ASNDetailId);
                    return false;
                }
                _logger.LogInformation("Found ASN Detail: ASNId={ASNId}, ItemId={ItemId}, RemainingQuantity={RemainingQuantity}", 
                    asnDetail.ASNId, asnDetail.ItemId, asnDetail.RemainingQuantity);

                // Create source reference for tracking
                var sourceReference = $"ASN-{asnDetail.ASNId}-{asnDetail.Id}";
                _logger.LogInformation("Created source reference: {SourceReference}", sourceReference);

                // Add stock to inventory
                _logger.LogInformation("Adding stock to inventory: ItemId={ItemId}, LocationId={LocationId}, Quantity={Quantity}, Price={Price}",
                    putawayDetail.ItemId, putawayDetail.LocationId, putawayDetail.QuantityToPutaway, asnDetail.ActualPricePerItem);
                
                await _inventoryRepository.AddOrUpdateStockAsync(
                    putawayDetail.ItemId,
                    putawayDetail.LocationId,
                    putawayDetail.QuantityToPutaway,
                    asnDetail.ActualPricePerItem,
                    sourceReference
                );
                _logger.LogInformation("Stock added to inventory successfully");

                // Update location current capacity
                _logger.LogInformation("Updating location current capacity for LocationId={LocationId}", putawayDetail.LocationId);
                await _locationRepository.UpdateCurrentCapacityAsync(putawayDetail.LocationId);
                _logger.LogInformation("Location current capacity updated successfully");

                // Update ASN detail with putaway quantity
                _logger.LogInformation("Updating ASN detail putaway quantity: {Quantity}", putawayDetail.QuantityToPutaway);
                asnDetail.AddPutawayQuantity(putawayDetail.QuantityToPutaway);
                
                // Update ASN detail in database
                _logger.LogInformation("Saving ASN detail changes to database");
                _context.ASNDetails.Update(asnDetail);
                await _context.SaveChangesAsync();
                _logger.LogInformation("ASN detail changes saved successfully");

                _logger.LogInformation("Putaway processed successfully for ASN Detail {ASNDetailId}, Quantity: {Quantity}, Location: {LocationId}",
                    putawayDetail.ASNDetailId, putawayDetail.QuantityToPutaway, putawayDetail.LocationId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing putaway for ASN Detail {ASNDetailId}", putawayDetail.ASNDetailId);
                return false;
            }
        }

        public async Task<bool> ProcessBulkPutawayAsync(IEnumerable<PutawayDetailViewModel> putawayDetails)
        {
            try
            {
                var allSuccess = true;

                foreach (var putawayDetail in putawayDetails)
                {
                    if (putawayDetail.QuantityToPutaway > 0)
                    {
                        var success = await ProcessPutawayAsync(putawayDetail);
                        if (!success)
                            allSuccess = false;
                    }
                }

                return allSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk putaway");
                return false;
            }
        }

        public async Task<bool> CompletePutawayAsync(int asnId)
        {
            try
            {
                // Check if all ASN details have been fully put away
                var asnDetails = await GetASNDetailsForPutawayAsync(asnId);
                var allPutAway = asnDetails.All(detail => detail.RemainingQuantity == 0);

                if (allPutAway)
                {
                    // Update ASN status to Completed
                    return await _asnRepository.UpdateStatusAsync(asnId, ASNStatus.Processed);
                }

                return true; // Partial putaway is still success
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing putaway for ASN {ASNId}", asnId);
                return false;
            }
        }

        public async Task<IEnumerable<Location>> GetAvailableLocationsForPutawayAsync()
        {
            return await _inventoryRepository.GetAvailableLocationsForPutawayAsync();
        }

        public async Task<bool> ValidatePutawayAsync(PutawayDetailViewModel putawayDetail)
        {
            try
            {
                _logger.LogInformation("Validating putaway: ASNDetailId={ASNDetailId}, ItemId={ItemId}, LocationId={LocationId}, Quantity={Quantity}",
                    putawayDetail.ASNDetailId, putawayDetail.ItemId, putawayDetail.LocationId, putawayDetail.QuantityToPutaway);

                // Check if quantity is valid
                if (putawayDetail.QuantityToPutaway <= 0)
                {
                    _logger.LogWarning("Invalid quantity: {Quantity}", putawayDetail.QuantityToPutaway);
                    return false;
                }
                _logger.LogInformation("Quantity validation passed: {Quantity}", putawayDetail.QuantityToPutaway);

                // Check if location has capacity
                _logger.LogInformation("Checking location capacity for LocationId={LocationId}, AdditionalQuantity={Quantity}", 
                    putawayDetail.LocationId, putawayDetail.QuantityToPutaway);
                if (!await _locationRepository.CheckCapacityForPutawayAsync(putawayDetail.LocationId, putawayDetail.QuantityToPutaway))
                {
                    _logger.LogWarning("Location capacity check failed for LocationId={LocationId}, AdditionalQuantity={Quantity}", 
                        putawayDetail.LocationId, putawayDetail.QuantityToPutaway);
                    return false;
                }
                _logger.LogInformation("Location capacity check passed");

                // Check if remaining quantity is sufficient
                _logger.LogInformation("Checking ASN detail remaining quantity for ASNDetailId={ASNDetailId}", putawayDetail.ASNDetailId);
                var asnDetail = await _asnRepository.GetASNDetailByIdAsync(putawayDetail.ASNDetailId);
                if (asnDetail == null)
                {
                    _logger.LogError("ASN Detail {ASNDetailId} not found during validation", putawayDetail.ASNDetailId);
                    return false;
                }
                
                _logger.LogInformation("ASN Detail found: ShippedQuantity={ShippedQuantity}, AlreadyPutAwayQuantity={AlreadyPutAwayQuantity}, RemainingQuantity={RemainingQuantity}, RequestedQuantity={RequestedQuantity}", 
                    asnDetail.ShippedQuantity, asnDetail.AlreadyPutAwayQuantity, asnDetail.RemainingQuantity, putawayDetail.QuantityToPutaway);
                
                // Check if remaining quantity is greater than 0
                if (asnDetail.RemainingQuantity <= 0)
                {
                    _logger.LogWarning("ASN Detail {ASNDetailId} has no remaining quantity: RemainingQuantity={RemainingQuantity}", 
                        putawayDetail.ASNDetailId, asnDetail.RemainingQuantity);
                    return false;
                }
                
                if (putawayDetail.QuantityToPutaway > asnDetail.RemainingQuantity)
                {
                    _logger.LogWarning("Insufficient remaining quantity: Requested={Requested}, Available={Available}", 
                        putawayDetail.QuantityToPutaway, asnDetail.RemainingQuantity);
                    return false;
                }
                _logger.LogInformation("Remaining quantity check passed");

                _logger.LogInformation("All putaway validations passed for ASNDetailId={ASNDetailId}", putawayDetail.ASNDetailId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating putaway for ASN Detail {ASNDetailId}", putawayDetail.ASNDetailId);
                return false;
            }
        }

        #endregion

        #region Stock Management

        public async Task<int> GetTotalStockByItemAsync(int itemId)
        {
            return await _inventoryRepository.GetTotalStockByItemAsync(itemId);
        }

        public async Task<IEnumerable<Inventory>> GetStockByItemAsync(int itemId)
        {
            return await _inventoryRepository.GetByItemIdAsync(itemId);
        }

        public async Task<Inventory> AddStockAsync(int itemId, int locationId, int quantity, decimal costPrice, string? sourceReference = null)
        {
            return await _inventoryRepository.AddOrUpdateStockAsync(itemId, locationId, quantity, costPrice, sourceReference);
        }

        public async Task<bool> ReduceStockAsync(int inventoryId, int quantity)
        {
            return await _inventoryRepository.ReduceStockAsync(inventoryId, quantity);
        }

        public async Task<bool> TransferStockAsync(int fromInventoryId, int toLocationId, int quantity)
        {
            try
            {
                var fromInventory = await _inventoryRepository.GetByIdWithDetailsAsync(fromInventoryId);
                if (fromInventory == null || fromInventory.Quantity < quantity)
                    return false;

                var fromLocationId = fromInventory.LocationId;

                // Reduce stock from source
                var reduceSuccess = await _inventoryRepository.ReduceStockAsync(fromInventoryId, quantity);
                if (!reduceSuccess)
                    return false;

                // Add stock to destination
                await _inventoryRepository.AddOrUpdateStockAsync(
                    fromInventory.ItemId,
                    toLocationId,
                    quantity,
                    fromInventory.LastCostPrice,
                    $"Transfer from {fromInventory.Location?.Code}"
                );

                // Update capacity for both source and target locations
                await _locationRepository.UpdateCurrentCapacityAsync(fromLocationId);
                await _locationRepository.UpdateCurrentCapacityAsync(toLocationId);
                
                _logger.LogInformation("Transferred {Quantity} units from inventory {FromInventoryId} (location {FromLocationId}) to location {ToLocationId} and updated both location capacities", 
                    quantity, fromInventoryId, fromLocationId, toLocationId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring stock from Inventory {FromInventoryId} to Location {ToLocationId}", fromInventoryId, toLocationId);
                return false;
            }
        }

        public async Task<bool> UpdateInventoryStatusAsync(int inventoryId, string status, string? notes = null)
        {
            try
            {
                var inventory = await _inventoryRepository.GetByIdAsync(inventoryId);
                if (inventory == null)
                    return false;

                inventory.UpdateStatus(status, notes);
                await _inventoryRepository.UpdateAsync(inventory);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory status for ID {InventoryId}", inventoryId);
                return false;
            }
        }

        public async Task<bool> UpdateQuantityAsync(int inventoryId, int newQuantity)
        {
            try
            {
                _logger.LogInformation("Starting quantity update for inventory {InventoryId} to {NewQuantity}", inventoryId, newQuantity);
                
                // Get inventory details before updating
                var inventory = await _inventoryRepository.GetByIdAsync(inventoryId);
                if (inventory == null)
                {
                    _logger.LogWarning("Inventory {InventoryId} not found for quantity update", inventoryId);
                    return false;
                }

                var locationId = inventory.LocationId;
                var oldQuantity = inventory.Quantity;
                
                _logger.LogInformation("Found inventory {InventoryId}: ItemId={ItemId}, LocationId={LocationId}, OldQuantity={OldQuantity}", 
                    inventoryId, inventory.ItemId, locationId, oldQuantity);

                // Update quantity
                var result = await _inventoryRepository.UpdateQuantityAsync(inventoryId, newQuantity);
                
                if (result)
                {
                    _logger.LogInformation("Successfully updated inventory {InventoryId} quantity in database", inventoryId);
                    
                    // Update location capacity after quantity change
                    _logger.LogInformation("Updating location {LocationId} capacity after quantity change", locationId);
                    await _locationRepository.UpdateCurrentCapacityAsync(locationId);
                    
                    _logger.LogInformation("Successfully updated inventory {InventoryId} quantity from {OldQuantity} to {NewQuantity} and updated location {LocationId} capacity", 
                        inventoryId, oldQuantity, newQuantity, locationId);
                }
                else
                {
                    _logger.LogError("Failed to update inventory {InventoryId} quantity in database", inventoryId);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity for inventory {InventoryId}", inventoryId);
                return false;
            }
        }

        #endregion

        #region Tracking and Analytics

        public async Task<IEnumerable<Inventory>> GetInventoryMovementsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            return await _inventoryRepository.GetInventoryMovementsAsync(fromDate, toDate);
        }

        public async Task<IEnumerable<Inventory>> GetLowStockInventoriesAsync(int threshold = 10)
        {
            return await _inventoryRepository.GetLowStockInventoriesAsync(threshold);
        }

        public async Task<IEnumerable<Inventory>> GetEmptyLocationsAsync()
        {
            return await _inventoryRepository.GetEmptyLocationsAsync();
        }

        public async Task<Dictionary<string, object>> GetInventoryStatisticsAsync()
        {
            return await _inventoryRepository.GetInventoryStatisticsAsync();
        }

        public async Task<Dictionary<string, object>> GetInventoryValuationAsync()
        {
            var allInventories = await _inventoryRepository.GetAllWithDetailsAsync();

            var valuation = new Dictionary<string, object>
            {
                ["TotalValue"] = allInventories.Sum(inv => inv.TotalValue),
                ["AvailableValue"] = allInventories.Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE).Sum(inv => inv.TotalValue),
                ["ReservedValue"] = allInventories.Where(inv => inv.Status == Constants.INVENTORY_STATUS_RESERVED).Sum(inv => inv.TotalValue),
                ["DamagedValue"] = allInventories.Where(inv => inv.Status == Constants.INVENTORY_STATUS_DAMAGED).Sum(inv => inv.TotalValue),
                ["TotalItems"] = allInventories.Sum(inv => inv.Quantity),
                ["UniqueItems"] = allInventories.Select(inv => inv.ItemId).Distinct().Count(),
                ["AverageCostPrice"] = allInventories.Any() ? allInventories.Average(inv => inv.LastCostPrice) : 0,
                ["HighestValueItem"] = allInventories.OrderByDescending(inv => inv.TotalValue).FirstOrDefault()?.ItemDisplay ?? "N/A",
                ["LowestValueItem"] = allInventories.Where(inv => inv.Quantity > 0).OrderBy(inv => inv.TotalValue).FirstOrDefault()?.ItemDisplay ?? "N/A"
            };

            return valuation;
        }

        public async Task<IEnumerable<Inventory>> TrackInventoryBySourceAsync(string sourceReference)
        {
            return await _inventoryRepository.GetBySourceReferenceAsync(sourceReference);
        }

        #endregion

        #region Location Management

        public async Task<IEnumerable<Inventory>> GetInventoriesByLocationAsync(int locationId)
        {
            return await _inventoryRepository.GetByLocationIdAsync(locationId);
        }

        public async Task<Dictionary<string, object>> GetLocationUtilizationAsync()
        {
            var allLocations = await _locationRepository.GetAllAsync();
            var allInventories = await _inventoryRepository.GetAllWithDetailsAsync();

            var utilization = new Dictionary<string, object>
            {
                ["TotalLocations"] = allLocations.Count(),
                ["UsedLocations"] = allInventories.Where(inv => inv.Quantity > 0).Select(inv => inv.LocationId).Distinct().Count(),
                ["EmptyLocations"] = allInventories.Count(inv => inv.Quantity == 0),
                ["AverageUtilization"] = allLocations.Any() ? allLocations.Average(loc => loc.CapacityPercentage) : 0,
                ["FullLocations"] = allLocations.Count(loc => loc.IsFull),
                ["NearFullLocations"] = allLocations.Count(loc => loc.CapacityPercentage >= 80 && !loc.IsFull)
            };

            return utilization;
        }

        public async Task<Location?> SuggestOptimalLocationAsync(int itemId, int quantity)
        {
            try
            {
                _logger.LogInformation("Suggesting optimal location for item {ItemId} with quantity {Quantity}", itemId, quantity);
                
                var availableLocations = await GetAvailableLocationsForPutawayAsync();
                _logger.LogInformation("Found {Count} available locations for suggestion", availableLocations.Count());

                if (!availableLocations.Any())
                {
                    _logger.LogWarning("No available locations found for item {ItemId}", itemId);
                    return null;
                }

                // Simple logic: find location with enough capacity and lowest current utilization
                var suitableLocations = availableLocations
                    .Where(loc => loc.MaxCapacity > 0 && loc.AvailableCapacity >= quantity)
                    .ToList();

                _logger.LogInformation("Found {Count} suitable locations with enough capacity", suitableLocations.Count);

                var suggestedLocation = suitableLocations
                    .OrderBy(loc => loc.CapacityPercentage)
                    .FirstOrDefault();

                if (suggestedLocation != null)
                {
                    _logger.LogInformation("Suggested location: {LocationCode} with {AvailableCapacity} available capacity", 
                        suggestedLocation.Code, suggestedLocation.AvailableCapacity);
                }
                else
                {
                    _logger.LogWarning("No suitable location found for item {ItemId} with quantity {Quantity}", itemId, quantity);
                }

                return suggestedLocation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suggesting optimal location for item {ItemId} with quantity {Quantity}", itemId, quantity);
                return null;
            }
        }

        public async Task<bool> CheckLocationCapacityAsync(int locationId, int additionalQuantity)
        {
            _logger.LogInformation("Checking location capacity: LocationId={LocationId}, AdditionalQuantity={AdditionalQuantity}", 
                locationId, additionalQuantity);
            
            var location = await _locationRepository.GetByIdAsync(locationId);
            if (location == null)
            {
                _logger.LogError("Location {LocationId} not found", locationId);
                return false;
            }
            
            // Check if location is active
            if (!location.IsActive)
            {
                _logger.LogWarning("Location {LocationId} is not active", locationId);
                return false;
            }
            
            // Use CurrentCapacity from location (should be updated by UpdateCurrentCapacityAsync)
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue)
            {
                _logger.LogError("CompanyId not available for capacity check");
                return false;
            }
            
            // Handle MaxCapacity = 0 or negative values
            if (location.MaxCapacity <= 0)
            {
                _logger.LogWarning("Location {LocationId} has invalid MaxCapacity {MaxCapacity}, setting to 1000", 
                    locationId, location.MaxCapacity);
                location.MaxCapacity = 1000; // Set default capacity
            }
            
            var availableCapacity = location.MaxCapacity - location.CurrentCapacity;
            
            _logger.LogInformation("Location found: Code={Code}, MaxCapacity={MaxCapacity}, CurrentCapacity={CurrentCapacity}, AvailableCapacity={AvailableCapacity}", 
                location.Code, location.MaxCapacity, location.CurrentCapacity, availableCapacity);
            
            var hasCapacity = availableCapacity >= additionalQuantity;
            _logger.LogInformation("Location capacity check result: {HasCapacity} (Available: {Available}, Required: {Required})", 
                hasCapacity, availableCapacity, additionalQuantity);
            
            return hasCapacity;
        }

        public async Task<IEnumerable<Location>> GetAllLocationsAsync()
        {
            return await _locationRepository.GetAllAsync();
        }

        private async Task<int?> GetSuggestedLocationIdAsync(int itemId, int quantity)
        {
            var suggestedLocation = await SuggestOptimalLocationAsync(itemId, quantity);
            return suggestedLocation?.Id;
        }

        #endregion

        #region Search and Filter

        public async Task<IEnumerable<Inventory>> SearchInventoryAsync(string searchTerm)
        {
            return await _inventoryRepository.SearchInventoryAsync(searchTerm);
        }

        public async Task<IEnumerable<Inventory>> GetInventoriesByStatusAsync(string status)
        {
            return await _inventoryRepository.GetByStatusAsync(status);
        }

        public async Task<IEnumerable<Inventory>> GetAvailableInventoryForSalesAsync()
        {
            return await _inventoryRepository.GetAvailableForSaleAsync();
        }

        #endregion

        #region ViewModel Operations

        public async Task<InventoryViewModel> GetInventoryViewModelAsync(int? id = null)
        {
            var viewModel = new InventoryViewModel();

            if (id.HasValue)
            {
                var inventory = await GetInventoryByIdAsync(id.Value);
                if (inventory != null)
                {
                    viewModel.Id = inventory.Id;
                    viewModel.ItemId = inventory.ItemId;
                    viewModel.LocationId = inventory.LocationId;
                    viewModel.Quantity = inventory.Quantity;
                    viewModel.LastCostPrice = inventory.LastCostPrice;
                    viewModel.Status = inventory.Status;
                    viewModel.Notes = inventory.Notes;
                    viewModel.SourceReference = inventory.SourceReference;
                    viewModel.ItemDisplay = inventory.ItemDisplay;
                    viewModel.LocationDisplay = inventory.LocationDisplay;
                }
            }

            return await PopulateInventoryViewModelAsync(viewModel);
        }

        public async Task<InventoryViewModel> PopulateInventoryViewModelAsync(InventoryViewModel viewModel)
        {
            var items = await _itemRepository.GetAllAsync();
            var locations = await _locationRepository.GetAllAsync();

            viewModel.Items = new SelectList(items, "Id", "DisplayName", viewModel.ItemId);
            viewModel.Locations = new SelectList(locations, "Id", "DisplayName", viewModel.LocationId);

            // Create enhanced location dropdown items
            var locationDropdownItems = locations.Select(location => new LocationDropdownItem
            {
                Id = location.Id,
                Code = location.Code,
                Name = location.Name,
                MaxCapacity = location.MaxCapacity,
                CurrentCapacity = location.CurrentCapacity,
                AvailableCapacity = location.AvailableCapacity,
                DisplayText = location.DropdownDisplayText,
                CssClass = location.DropdownCssClass,
                StatusText = location.DropdownStatusText,
                CanAccommodate = true, // For manual inventory creation, allow all locations
                IsFull = location.IsFull,
                CapacityPercentage = location.CapacityPercentage
            }).ToList();

            viewModel.LocationDropdownItems = locationDropdownItems;

            return viewModel;
        }

        public async Task<bool> ValidateInventoryAsync(InventoryViewModel viewModel)
        {
            // Validate item exists
            var item = await _itemRepository.GetByIdAsync(viewModel.ItemId);
            if (item == null)
                return false;

            // Validate location exists
            var location = await _locationRepository.GetByIdAsync(viewModel.LocationId);
            if (location == null)
                return false;

            // Validate location capacity
            if (!await CheckLocationCapacityAsync(viewModel.LocationId, viewModel.Quantity))
            {
                _logger.LogWarning("Location {LocationId} cannot accommodate quantity {Quantity}", viewModel.LocationId, viewModel.Quantity);
                return false;
            }

            return true;
        }

        #endregion

        #region Reporting

        public async Task<IEnumerable<object>> GetInventoryAgingReportAsync()
        {
            var inventories = await _inventoryRepository.GetAllWithDetailsAsync();

            return inventories.Select(inv => new
            {
                ItemCode = inv.Item?.ItemCode,
                ItemName = inv.Item?.Name,
                LocationCode = inv.Location?.Code,
                Quantity = inv.Quantity,
                LastCostPrice = inv.LastCostPrice,
                TotalValue = inv.TotalValue,
                LastUpdated = inv.LastUpdated,
                DaysOld = (DateTime.Now - inv.LastUpdated).Days,
                AgeCategory = (DateTime.Now - inv.LastUpdated).Days switch
                {
                    <= 30 => "Fresh (≤ 30 days)",
                    <= 90 => "Moderate (31-90 days)",
                    <= 180 => "Old (91-180 days)",
                    _ => "Very Old (> 180 days)"
                }
            }).OrderByDescending(x => x.DaysOld);
        }

        public async Task<Dictionary<string, object>> GetABCAnalysisAsync()
        {
            var inventories = await _inventoryRepository.GetAllWithDetailsAsync();
            var sortedByValue = inventories.OrderByDescending(inv => inv.TotalValue).ToList();

            var totalValue = sortedByValue.Sum(inv => inv.TotalValue);
            var cumulativeValue = 0m;
            var analysis = new List<object>();

            foreach (var inventory in sortedByValue)
            {
                cumulativeValue += inventory.TotalValue;
                var cumulativePercentage = totalValue > 0 ? (cumulativeValue / totalValue) * 100 : 0;

                var category = cumulativePercentage switch
                {
                    <= 80 => "A",
                    <= 95 => "B",
                    _ => "C"
                };

                analysis.Add(new
                {
                    ItemCode = inventory.Item?.ItemCode,
                    ItemName = inventory.Item?.Name,
                    TotalValue = inventory.TotalValue,
                    CumulativeValue = cumulativeValue,
                    CumulativePercentage = cumulativePercentage,
                    Category = category
                });
            }

            return new Dictionary<string, object>
            {
                ["Analysis"] = analysis,
                ["CategoryA_Count"] = analysis.Count(a => ((dynamic)a).Category == "A"),
                ["CategoryB_Count"] = analysis.Count(a => ((dynamic)a).Category == "B"),
                ["CategoryC_Count"] = analysis.Count(a => ((dynamic)a).Category == "C"),
                ["CategoryA_Value"] = analysis.Where(a => ((dynamic)a).Category == "A").Sum(a => ((dynamic)a).TotalValue),
                ["CategoryB_Value"] = analysis.Where(a => ((dynamic)a).Category == "B").Sum(a => ((dynamic)a).TotalValue),
                ["CategoryC_Value"] = analysis.Where(a => ((dynamic)a).Category == "C").Sum(a => ((dynamic)a).TotalValue)
            };
        }

        public async Task<Dictionary<string, object>> GetInventoryTurnoverAnalysisAsync()
        {
            // This would require sales data to calculate proper turnover
            // For now, return basic metrics
            var inventories = await _inventoryRepository.GetAllWithDetailsAsync();

            return new Dictionary<string, object>
            {
                ["TotalItems"] = inventories.Sum(inv => inv.Quantity),
                ["TotalValue"] = inventories.Sum(inv => inv.TotalValue),
                ["AverageAge"] = inventories.Any() ? inventories.Average(inv => (DateTime.Now - inv.LastUpdated).Days) : 0,
                ["SlowMovingItems"] = inventories.Count(inv => (DateTime.Now - inv.LastUpdated).Days > 90),
                ["FastMovingItems"] = inventories.Count(inv => (DateTime.Now - inv.LastUpdated).Days <= 30)
            };
        }

        /// <summary>
        /// Move stock between locations using simplified pattern
        /// </summary>
        public async Task<bool> MoveStockAsync(int itemId, int fromLocationId, int toLocationId, int quantity, string sourceReference)
        {
            try
            {
                _logger.LogInformation("Moving stock: ItemId={ItemId}, From={FromLocationId}, To={ToLocationId}, Quantity={Quantity}, SourceReference={SourceReference}",
                    itemId, fromLocationId, toLocationId, quantity, sourceReference);

                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    _logger.LogError("CompanyId is null - cannot move stock");
                    return false;
                }

                // Get source inventory
                var sourceInventory = await _inventoryRepository.GetByItemAndLocationAsync(itemId, fromLocationId);
                if (sourceInventory == null)
                {
                    _logger.LogError("Source inventory not found for ItemId: {ItemId}, LocationId: {LocationId}", itemId, fromLocationId);
                    return false;
                }

                if (sourceInventory.Quantity < quantity)
                {
                    _logger.LogError("Insufficient stock. Available: {Available}, Required: {Required}", sourceInventory.Quantity, quantity);
                    return false;
                }

                // Reduce from source
                sourceInventory.Quantity -= quantity;
                sourceInventory.LastUpdated = DateTime.Now;
                if (sourceInventory.Quantity == 0)
                {
                    sourceInventory.Status = Constants.INVENTORY_STATUS_EMPTY;
                }

                // Get or create destination inventory
                var destInventory = await _inventoryRepository.GetByItemAndLocationAsync(itemId, toLocationId);
                if (destInventory == null)
                {
                    // Create new inventory record
                    destInventory = new Inventory
                    {
                        ItemId = itemId,
                        LocationId = toLocationId,
                        Quantity = quantity,
                        Status = Constants.INVENTORY_STATUS_AVAILABLE,
                        SourceReference = sourceReference,
                        CompanyId = companyId.Value,
                        CreatedBy = _currentUserService.UserId?.ToString() ?? "0",
                        CreatedDate = DateTime.Now,
                        LastUpdated = DateTime.Now
                    };
                    _context.Inventories.Add(destInventory);
                }
                else
                {
                    // Update existing inventory
                    destInventory.Quantity += quantity;
                    
                    // Auto-update status berdasarkan quantity
                    if (destInventory.Quantity > 0)
                    {
                        destInventory.Status = Constants.INVENTORY_STATUS_AVAILABLE;
                    }
                    
                    destInventory.LastUpdated = DateTime.Now;
                }

                // Update both inventories
                _context.Inventories.Update(sourceInventory);
                _context.Inventories.Update(destInventory);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Stock moved successfully: ItemId={ItemId}, From={FromLocationId}, To={ToLocationId}, Quantity={Quantity}",
                    itemId, fromLocationId, toLocationId, quantity);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving stock: ItemId={ItemId}, From={FromLocationId}, To={ToLocationId}, Quantity={Quantity}",
                    itemId, fromLocationId, toLocationId, quantity);
                return false;
            }
        }

        #endregion
    }
}