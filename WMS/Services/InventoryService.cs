using WMS.Data.Repositories;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Service implementation untuk Inventory management
    /// "The Stage Design" - mengatur lokasi penyimpanan dan tracking item
    /// </summary>
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IItemRepository _itemRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IASNRepository _asnRepository;

        public InventoryService(
            IInventoryRepository inventoryRepository,
            IItemRepository itemRepository,
            ILocationRepository locationRepository,
            IASNRepository asnRepository)
        {
            _inventoryRepository = inventoryRepository;
            _itemRepository = itemRepository;
            _locationRepository = locationRepository;
            _asnRepository = asnRepository;
        }

        #region Basic CRUD Operations

        public async Task<IEnumerable<Inventory>> GetAllInventoryAsync()
        {
            return await _inventoryRepository.GetAllWithDetailsAsync();
        }

        public async Task<Inventory?> GetInventoryByIdAsync(int id)
        {
            return await _inventoryRepository.GetByIdWithDetailsAsync(id);
        }

        public async Task<Inventory> CreateInventoryAsync(InventoryViewModel viewModel)
        {
            var inventory = new Inventory
            {
                ItemId = viewModel.ItemId,
                LocationId = viewModel.LocationId,
                Quantity = viewModel.Quantity,
                LastCostPrice = viewModel.LastCostPrice,
                Status = viewModel.Status,
                Notes = viewModel.Notes,
                LastUpdated = DateTime.Now,
                CreatedDate = DateTime.Now
            };

            var result = await _inventoryRepository.AddAsync(inventory);

            // Update location capacity
            await UpdateLocationCapacityAsync(viewModel.LocationId);

            return result;
        }

        public async Task<Inventory> UpdateInventoryAsync(int id, InventoryViewModel viewModel)
        {
            var existingInventory = await _inventoryRepository.GetByIdAsync(id);
            if (existingInventory == null)
                throw new ArgumentException($"Inventory with ID {id} not found");

            var oldLocationId = existingInventory.LocationId;

            existingInventory.ItemId = viewModel.ItemId;
            existingInventory.LocationId = viewModel.LocationId;
            existingInventory.Quantity = viewModel.Quantity;
            existingInventory.LastCostPrice = viewModel.LastCostPrice;
            existingInventory.Status = viewModel.Status;
            existingInventory.Notes = viewModel.Notes;
            existingInventory.LastUpdated = DateTime.Now;
            existingInventory.ModifiedDate = DateTime.Now;

            await _inventoryRepository.UpdateAsync(existingInventory);

            // Update location capacities if location changed
            await UpdateLocationCapacityAsync(viewModel.LocationId);
            if (oldLocationId != viewModel.LocationId)
            {
                await UpdateLocationCapacityAsync(oldLocationId);
            }

            return existingInventory;
        }

        public async Task<bool> DeleteInventoryAsync(int id)
        {
            var inventory = await _inventoryRepository.GetByIdAsync(id);
            if (inventory == null)
                return false;

            var locationId = inventory.LocationId;
            bool result = await _inventoryRepository.DeleteAsync(id);

            if (result)
            {
                await UpdateLocationCapacityAsync(locationId);
            }

            return result;
        }

        #endregion

        #region Query Operations

        public async Task<IEnumerable<Inventory>> GetInventoryByItemAsync(int itemId)
        {
            return await _inventoryRepository.GetByItemAsync(itemId);
        }

        public async Task<IEnumerable<Inventory>> GetInventoryByLocationAsync(int locationId)
        {
            return await _inventoryRepository.GetByLocationAsync(locationId);
        }

        public async Task<IEnumerable<Inventory>> GetInventoryByStatusAsync(InventoryStatus status)
        {
            return await _inventoryRepository.GetByStatusAsync(status);
        }

        public async Task<IEnumerable<Inventory>> GetAvailableInventoryAsync()
        {
            return await _inventoryRepository.GetAvailableInventoryAsync();
        }

        public async Task<IEnumerable<Inventory>> GetLowStockInventoryAsync(int threshold = 10)
        {
            return await _inventoryRepository.GetLowStockInventoryAsync(threshold);
        }

        public async Task<Inventory?> GetInventoryByItemAndLocationAsync(int itemId, int locationId)
        {
            return await _inventoryRepository.GetByItemAndLocationAsync(itemId, locationId);
        }

        #endregion

        #region Putaway Operations

        public async Task<bool> ProcessPutawayAsync(PutawayViewModel viewModel)
        {
            if (!await ValidatePutawayAsync(viewModel))
                return false;

            try
            {
                // Get ASN with details to get cost price - FIXED
                var asn = await _asnRepository.GetByIdWithDetailsAsync(viewModel.ASNDetailId);
                if (asn == null)
                    return false;

                // Get the specific ASNDetail item - FIXED
                var asnDetail = asn.ASNDetails?.FirstOrDefault(d => d.Id == viewModel.ASNDetailId);
                if (asnDetail == null)
                    return false;

                var costPrice = asnDetail.ActualPricePerItem;

                // Add stock to the specified location
                var success = await AddStockAsync(
                    asnDetail.ItemId,
                    viewModel.LocationId,
                    viewModel.QuantityToPutaway,
                    costPrice
                );

                if (success)
                {
                    // Update ASN detail to track putaway progress
                    // This would require additional fields in ASNDetail or a separate tracking table
                    // For now, we'll assume it's tracked elsewhere
                }

                return success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetASNsForPutawayAsync()
        {
            return await _asnRepository.GetArrivedASNsAsync();
        }

        public async Task<IEnumerable<ASNDetail>> GetASNDetailsForPutawayAsync(int asnId)
        {
            var asn = await _asnRepository.GetByIdWithDetailsAsync(asnId);
            return asn?.ASNDetails ?? new List<ASNDetail>();
        }

        public async Task<PutawayViewModel> GetPutawayViewModelAsync(int? asnId = null, int? asnDetailId = null)
        {
            var viewModel = new PutawayViewModel();

            if (asnId.HasValue)
            {
                var asn = await _asnRepository.GetByIdWithDetailsAsync(asnId.Value);
                if (asn != null)
                {
                    viewModel.ASNId = asn.Id;
                    viewModel.ASNNumber = asn.ASNNumber;
                    viewModel.SupplierName = asn.PurchaseOrder.Supplier.Name;
                    viewModel.ASNDate = asn.ShipmentDate;
                }

                if (asnDetailId.HasValue)
                {
                    var asnDetail = asn?.ASNDetails?.FirstOrDefault(d => d.Id == asnDetailId.Value);
                    if (asnDetail != null)
                    {
                        viewModel.ASNDetailId = asnDetail.Id;
                        viewModel.ItemCode = asnDetail.Item.ItemCode;
                        viewModel.ItemName = asnDetail.Item.Name;
                        viewModel.ItemUnit = asnDetail.Item.Unit;
                        viewModel.ReceivedQuantity = asnDetail.ShippedQuantity;
                        viewModel.CostPrice = asnDetail.ActualPricePerItem;
                        // Note: You'll need to calculate AlreadyPutawayQuantity from your inventory records
                        // viewModel.AlreadyPutawayQuantity = await GetAlreadyPutawayQuantity(asnDetailId.Value);
                    }
                }
            }

            // FIXED: Convert IEnumerable to List using ToList()
            viewModel.AvailableASNs = (await GetASNsForPutawayAsync()).ToList();

            if (asnId.HasValue)
            {
                viewModel.AvailableASNDetails = (await GetASNDetailsForPutawayAsync(asnId.Value)).ToList();
            }
            else
            {
                viewModel.AvailableASNDetails = new List<ASNDetail>();
            }

            viewModel.AvailableLocations = (await _locationRepository.GetAvailableLocationsAsync()).ToList();

            return viewModel;
        }

        public async Task<bool> ValidatePutawayAsync(PutawayViewModel viewModel)
        {
            // Check if location has enough capacity
            if (!await IsLocationSuitableForPutawayAsync(viewModel.LocationId, viewModel.QuantityToPutaway))
                return false;

            // Check if quantity doesn't exceed remaining to putaway
            if (viewModel.QuantityToPutaway > viewModel.RemainingQuantity)
                return false;

            return true;
        }

        #endregion

        #region Stock Management Operations

        public async Task<bool> AddStockAsync(int itemId, int locationId, int quantity, decimal costPrice)
        {
            try
            {
                var existingInventory = await GetInventoryByItemAndLocationAsync(itemId, locationId);

                if (existingInventory != null)
                {
                    // Update existing inventory
                    existingInventory.AddStock(quantity, costPrice);
                    await _inventoryRepository.UpdateAsync(existingInventory);
                }
                else
                {
                    // Create new inventory record
                    var newInventory = new Inventory
                    {
                        ItemId = itemId,
                        LocationId = locationId,
                        Quantity = quantity,
                        LastCostPrice = costPrice,
                        Status = Constants.INVENTORY_STATUS_AVAILABLE,
                        LastUpdated = DateTime.Now,
                        CreatedDate = DateTime.Now
                    };

                    await _inventoryRepository.AddAsync(newInventory);
                }

                // Update location capacity
                await UpdateLocationCapacityAsync(locationId);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ReduceStockAsync(int itemId, int locationId, int quantity)
        {
            try
            {
                var inventory = await GetInventoryByItemAndLocationAsync(itemId, locationId);
                if (inventory == null)
                    return false;

                if (!inventory.ReduceStock(quantity))
                    return false;

                await _inventoryRepository.UpdateAsync(inventory);
                await UpdateLocationCapacityAsync(locationId);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> TransferStockAsync(StockTransferViewModel viewModel)
        {
            if (!viewModel.IsValid)
                return false;

            try
            {
                var fromInventory = await GetInventoryByIdAsync(viewModel.FromInventoryId);
                if (fromInventory == null)
                    return false;

                // Reduce stock from source
                if (!fromInventory.ReduceStock(viewModel.TransferQuantity))
                    return false;

                await _inventoryRepository.UpdateAsync(fromInventory);

                // Add stock to destination
                await AddStockAsync(
                    fromInventory.ItemId,
                    viewModel.ToLocationId,
                    viewModel.TransferQuantity,
                    fromInventory.LastCostPrice
                );

                // Update location capacities
                await UpdateLocationCapacityAsync(fromInventory.LocationId);
                await UpdateLocationCapacityAsync(viewModel.ToLocationId);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> AdjustStockAsync(InventoryAdjustmentViewModel viewModel)
        {
            try
            {
                var inventory = await GetInventoryByIdAsync(viewModel.InventoryId);
                if (inventory == null)
                    return false;

                inventory.Quantity = viewModel.NewQuantity;
                inventory.LastUpdated = DateTime.Now;
                inventory.Notes = $"Adjusted: {viewModel.AdjustmentReason}";

                await _inventoryRepository.UpdateAsync(inventory);
                await UpdateLocationCapacityAsync(inventory.LocationId);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateStockStatusAsync(int inventoryId, InventoryStatus status, string? notes = null)
        {
            try
            {
                var inventory = await GetInventoryByIdAsync(inventoryId);
                if (inventory == null)
                    return false;

                inventory.UpdateStatus(status.ToString(), notes);
                await _inventoryRepository.UpdateAsync(inventory);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Stock Validation Operations

        public async Task<bool> CheckStockAvailabilityAsync(int itemId, int requiredQuantity)
        {
            return await _inventoryRepository.CheckStockAvailabilityAsync(itemId, requiredQuantity);
        }

        public async Task<Dictionary<int, int>> GetAvailableStockByItemsAsync(IEnumerable<int> itemIds)
        {
            var result = new Dictionary<int, int>();

            foreach (var itemId in itemIds)
            {
                var inventories = await GetInventoryByItemAsync(itemId);
                var totalAvailable = inventories
                    .Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE)
                    .Sum(inv => inv.Quantity);

                result[itemId] = totalAvailable;
            }

            return result;
        }

        public async Task<bool> IsLocationSuitableForPutawayAsync(int locationId, int quantity)
        {
            var location = await _locationRepository.GetByIdAsync(locationId);
            if (location == null || !location.IsActive)
                return false;

            return location.AvailableCapacity >= quantity;
        }

        public async Task<IEnumerable<Location>> GetAvailableLocationsForPutawayAsync(int requiredCapacity)
        {
            var availableLocations = await _locationRepository.GetAvailableLocationsAsync();
            return availableLocations.Where(loc => loc.AvailableCapacity >= requiredCapacity);
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
                    viewModel.LastUpdated = inventory.LastUpdated;

                    // Populate display properties
                    viewModel.ItemCode = inventory.Item.ItemCode;
                    viewModel.ItemName = inventory.Item.Name;
                    viewModel.ItemUnit = inventory.Item.Unit;
                    viewModel.LocationCode = inventory.Location.Code;
                    viewModel.LocationName = inventory.Location.Name;
                    viewModel.LocationMaxCapacity = inventory.Location.MaxCapacity;
                    viewModel.LocationCurrentCapacity = inventory.Location.CurrentCapacity;
                }
            }

            return await PopulateInventoryViewModelAsync(viewModel);
        }

        public async Task<InventoryViewModel> PopulateInventoryViewModelAsync(InventoryViewModel viewModel)
        {
            viewModel.AvailableItems = (await _itemRepository.GetActiveItemsAsync()).ToList();
            viewModel.AvailableLocations = (await _locationRepository.GetActiveLocationsAsync()).ToList();

            return viewModel;
        }

        public async Task<StockTransferViewModel> GetStockTransferViewModelAsync(int? fromInventoryId = null)
        {
            var viewModel = new StockTransferViewModel();

            if (fromInventoryId.HasValue)
            {
                var inventory = await GetInventoryByIdAsync(fromInventoryId.Value);
                if (inventory != null)
                {
                    viewModel.FromInventoryId = inventory.Id;
                    viewModel.ItemCode = inventory.Item.ItemCode;
                    viewModel.ItemName = inventory.Item.Name;
                    viewModel.ItemUnit = inventory.Item.Unit;
                    viewModel.AvailableQuantity = inventory.Quantity;
                    viewModel.FromLocationCode = inventory.Location.Code;
                    viewModel.FromLocationName = inventory.Location.Name;
                }
            }

            return await PopulateStockTransferViewModelAsync(viewModel);
        }

        public async Task<StockTransferViewModel> PopulateStockTransferViewModelAsync(StockTransferViewModel viewModel)
        {
            viewModel.AvailableInventories = (await GetAvailableInventoryAsync()).ToList();
            viewModel.AvailableLocations = (await _locationRepository.GetActiveLocationsAsync()).ToList();

            return viewModel;
        }

        public async Task<InventoryAdjustmentViewModel> GetInventoryAdjustmentViewModelAsync(int inventoryId)
        {
            var inventory = await GetInventoryByIdAsync(inventoryId);
            if (inventory == null)
                throw new ArgumentException($"Inventory with ID {inventoryId} not found");

            return new InventoryAdjustmentViewModel
            {
                InventoryId = inventory.Id,
                CurrentQuantity = inventory.Quantity,
                NewQuantity = inventory.Quantity,
                ItemCode = inventory.Item.ItemCode,
                ItemName = inventory.Item.Name,
                ItemUnit = inventory.Item.Unit,
                LocationCode = inventory.Location.Code,
                LocationName = inventory.Location.Name,
                CurrentStatus = inventory.Status
            };
        }

        #endregion

        #region Reporting Operations

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            return await _inventoryRepository.GetTotalInventoryValueAsync();
        }

        public async Task<Dictionary<string, int>> GetInventoryByStatusSummaryAsync()
        {
            return await _inventoryRepository.GetInventoryByStatusAsync();
        }

        public async Task<Dictionary<int, int>> GetItemStockSummaryAsync()
        {
            return await _inventoryRepository.GetItemStockSummaryAsync();
        }

        public async Task<IEnumerable<object>> GetLowStockReportAsync(int threshold = 10)
        {
            var lowStockInventories = await GetLowStockInventoryAsync(threshold);

            return lowStockInventories.Select(inv => new
            {
                ItemId = inv.ItemId,
                ItemCode = inv.Item.ItemCode,
                ItemName = inv.Item.Name,
                Unit = inv.Item.Unit,
                CurrentStock = inv.Quantity,
                LocationCode = inv.Location.Code,
                LocationName = inv.Location.Name,
                LastCostPrice = inv.LastCostPrice,
                TotalValue = inv.TotalValue,
                LastUpdated = inv.LastUpdated,
                StockLevel = inv.StockLevel,
                Status = inv.StatusIndonesia
            }).ToList();
        }

        public async Task<IEnumerable<object>> GetInventoryMovementReportAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            // This would require an inventory movement/transaction table to track historical data
            // For now, return current inventory snapshot
            var allInventory = await GetAllInventoryAsync();

            var filteredInventory = allInventory.AsQueryable();

            if (fromDate.HasValue)
                filteredInventory = filteredInventory.Where(inv => inv.LastUpdated >= fromDate.Value);

            if (toDate.HasValue)
                filteredInventory = filteredInventory.Where(inv => inv.LastUpdated <= toDate.Value);

            return filteredInventory.Select(inv => new
            {
                Date = inv.LastUpdated,
                ItemCode = inv.Item.ItemCode,
                ItemName = inv.Item.Name,
                LocationCode = inv.Location.Code,
                Quantity = inv.Quantity,
                CostPrice = inv.LastCostPrice,
                TotalValue = inv.TotalValue,
                Status = inv.StatusIndonesia,
                Notes = inv.Notes
            }).OrderByDescending(x => x.Date);
        }

        public async Task<Dictionary<string, object>> GetInventoryStatisticsAsync()
        {
            var allInventory = await GetAllInventoryAsync();
            var availableInventory = allInventory.Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE);

            var stats = new Dictionary<string, object>
            {
                ["TotalInventoryRecords"] = allInventory.Count(),
                ["TotalItems"] = allInventory.Select(inv => inv.ItemId).Distinct().Count(),
                ["TotalLocations"] = allInventory.Select(inv => inv.LocationId).Distinct().Count(),
                ["TotalQuantity"] = allInventory.Sum(inv => inv.Quantity),
                ["AvailableQuantity"] = availableInventory.Sum(inv => inv.Quantity),
                ["TotalValue"] = allInventory.Sum(inv => inv.TotalValue),
                ["AvailableValue"] = availableInventory.Sum(inv => inv.TotalValue),
                ["LowStockItems"] = allInventory.Count(inv => inv.Quantity <= Constants.LOW_STOCK_THRESHOLD),
                ["EmptyLocations"] = allInventory.Count(inv => inv.Quantity == 0),
                ["AverageCostPrice"] = availableInventory.Any() ? availableInventory.Average(inv => inv.LastCostPrice) : 0
            };

            return stats;
        }

        #endregion

        #region Location Management

        public async Task<bool> UpdateLocationCapacityAsync(int locationId)
        {
            await _locationRepository.UpdateCapacityAsync(locationId);
            return true;
        }

        public async Task<Dictionary<string, object>> GetLocationUtilizationAsync()
        {
            return await _locationRepository.GetLocationStatisticsAsync();
        }

        public async Task<IEnumerable<Location>> GetOverCapacityLocationsAsync()
        {
            var allLocations = await _locationRepository.GetAllWithInventoryAsync();
            return allLocations.Where(loc => loc.IsFull || loc.CapacityPercentage > 100);
        }

        #endregion

        #region Item Tracking Operations

        public async Task<IEnumerable<object>> GetItemLocationHistoryAsync(int itemId)
        {
            var inventories = await GetInventoryByItemAsync(itemId);

            return inventories.Select(inv => new
            {
                LocationCode = inv.Location.Code,
                LocationName = inv.Location.Name,
                Quantity = inv.Quantity,
                Status = inv.StatusIndonesia,
                LastCostPrice = inv.LastCostPrice,
                TotalValue = inv.TotalValue,
                LastUpdated = inv.LastUpdated,
                Notes = inv.Notes
            }).OrderByDescending(x => x.LastUpdated);
        }

        public async Task<IEnumerable<object>> GetLocationInventoryDetailsAsync(int locationId)
        {
            var inventories = await GetInventoryByLocationAsync(locationId);

            return inventories.Select(inv => new
            {
                ItemCode = inv.Item.ItemCode,
                ItemName = inv.Item.Name,
                Unit = inv.Item.Unit,
                Quantity = inv.Quantity,
                Status = inv.StatusIndonesia,
                LastCostPrice = inv.LastCostPrice,
                TotalValue = inv.TotalValue,
                LastUpdated = inv.LastUpdated,
                StockLevel = inv.StockLevel
            }).OrderBy(x => x.ItemCode);
        }

        public async Task<object> GetItemCurrentLocationsAsync(int itemId)
        {
            var inventories = await GetInventoryByItemAsync(itemId);

            return new
            {
                ItemId = itemId,
                TotalQuantity = inventories.Sum(inv => inv.Quantity),
                AvailableQuantity = inventories.Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE).Sum(inv => inv.Quantity),
                TotalValue = inventories.Sum(inv => inv.TotalValue),
                LocationCount = inventories.Count(),
                Locations = inventories.Select(inv => new
                {
                    LocationCode = inv.Location.Code,
                    LocationName = inv.Location.Name,
                    Quantity = inv.Quantity,
                    Status = inv.StatusIndonesia,
                    LastUpdated = inv.LastUpdated
                }).OrderByDescending(x => x.Quantity)
            };
        }

        #endregion

        #region Business Logic Operations

        public async Task<bool> ProcessASNReceiptAsync(int asnDetailId, int locationId, int quantity)
        {
            // This would integrate with ASN processing
            return await ProcessPutawayAsync(new PutawayViewModel
            {
                ASNDetailId = asnDetailId,
                LocationId = locationId,
                QuantityToPutaway = quantity
            });
        }

        public async Task<bool> ProcessSalesOrderPickingAsync(int salesOrderId)
        {
            // This would integrate with sales order processing
            // Implementation would pick items from inventory for shipping
            return await Task.FromResult(true);
        }

        public async Task<IEnumerable<object>> GetPickingListAsync(int salesOrderId)
        {
            // Generate picking list for warehouse staff
            // This would show which locations to pick from for each item
            return await Task.FromResult(new List<object>());
        }

        public async Task<bool> ValidatePickingCapabilityAsync(int salesOrderId)
        {
            // Validate that all items in the sales order can be picked
            return await Task.FromResult(true);
        }

        #endregion

        #region Inventory Optimization

        public async Task<IEnumerable<object>> GetInventoryOptimizationSuggestionsAsync()
        {
            var allInventory = await GetAllInventoryAsync();
            var suggestions = new List<object>();

            // Find items that could be consolidated
            var itemGroups = allInventory
                .Where(inv => inv.Quantity > 0)
                .GroupBy(inv => inv.ItemId)
                .Where(g => g.Count() > 1) // Multiple locations for same item
                .ToList();

            foreach (var group in itemGroups)
            {
                var locations = group.OrderBy(inv => inv.Quantity).ToList();
                if (locations.Count > 2) // Could consolidate
                {
                    suggestions.Add(new
                    {
                        Type = "Consolidation",
                        ItemCode = locations.First().Item.ItemCode,
                        ItemName = locations.First().Item.Name,
                        CurrentLocations = locations.Count,
                        TotalQuantity = locations.Sum(l => l.Quantity),
                        SuggestedAction = $"Consider consolidating {locations.Count} locations into fewer locations"
                    });
                }
            }

            return suggestions;
        }

        public async Task<IEnumerable<object>> GetSlowMovingInventoryAsync(int daysThreshold = 90)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysThreshold);
            var allInventory = await GetAllInventoryAsync();

            return allInventory
                .Where(inv => inv.LastUpdated < cutoffDate && inv.Quantity > 0)
                .Select(inv => new
                {
                    ItemCode = inv.Item.ItemCode,
                    ItemName = inv.Item.Name,
                    LocationCode = inv.Location.Code,
                    Quantity = inv.Quantity,
                    Unit = inv.Item.Unit,
                    LastUpdated = inv.LastUpdated,
                    DaysSinceLastUpdate = (DateTime.Now - inv.LastUpdated).Days,
                    TotalValue = inv.TotalValue,
                    Suggestion = "Consider promotional pricing or alternative uses"
                })
                .OrderBy(x => x.LastUpdated);
        }

        public async Task<IEnumerable<object>> GetFastMovingInventoryAsync(int daysThreshold = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysThreshold);
            var allInventory = await GetAllInventoryAsync();

            return allInventory
                .Where(inv => inv.LastUpdated >= cutoffDate && inv.Quantity <= Constants.LOW_STOCK_THRESHOLD)
                .Select(inv => new
                {
                    ItemCode = inv.Item.ItemCode,
                    ItemName = inv.Item.Name,
                    LocationCode = inv.Location.Code,
                    Quantity = inv.Quantity,
                    Unit = inv.Item.Unit,
                    LastUpdated = inv.LastUpdated,
                    TotalValue = inv.TotalValue,
                    Suggestion = "Consider increasing reorder point or quantity"
                })
                .OrderBy(x => x.Quantity);
        }

        #endregion
    }
}