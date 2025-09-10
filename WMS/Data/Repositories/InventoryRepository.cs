using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.Services;
using WMS.Utilities;
using System.Linq.Expressions;

namespace WMS.Data.Repositories
{
    /// <summary>
    /// Repository implementation untuk Inventory management
    /// Menggunakan company filtering dan audit trail
    /// </summary>
    public class InventoryRepository : Repository<Inventory>, IInventoryRepository
    {
        public InventoryRepository(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<Repository<Inventory>> logger)
            : base(context, currentUserService, logger)
        {
        }

        #region Basic Operations with Details

        public async Task<IEnumerable<Inventory>> GetAllWithDetailsAsync()
        {
            return await GetBaseQuery()
                .Include(inv => inv.Item)
                    .ThenInclude(item => item.Supplier)
                .Include(inv => inv.Location)
                .OrderBy(inv => inv.Item.ItemCode)
                .ThenBy(inv => inv.Location.Code)
                .ToListAsync();
        }

        public async Task<Inventory?> GetByIdWithDetailsAsync(int id)
        {
            return await GetBaseQuery()
                .Include(inv => inv.Item)
                    .ThenInclude(item => item.Supplier)
                .Include(inv => inv.Location)
                .FirstOrDefaultAsync(inv => inv.Id == id);
        }

        #endregion

        #region Query by Relationships

        public async Task<Inventory?> GetByItemAndLocationAsync(int itemId, int locationId)
        {
            return await GetBaseQuery()
                .Include(inv => inv.Item)
                .Include(inv => inv.Location)
                .FirstOrDefaultAsync(inv => inv.ItemId == itemId && inv.LocationId == locationId);
        }

        public async Task<IEnumerable<Inventory>> GetByItemIdAsync(int itemId)
        {
            return await GetBaseQuery()
                .Include(inv => inv.Location)
                .Where(inv => inv.ItemId == itemId)
                .OrderBy(inv => inv.Location.Code)
                .ToListAsync();
        }

        public async Task<IEnumerable<Inventory>> GetByLocationIdAsync(int locationId)
        {
            return await GetBaseQuery()
                .Include(inv => inv.Item)
                .Where(inv => inv.LocationId == locationId)
                .OrderBy(inv => inv.Item.ItemCode)
                .ToListAsync();
        }

        public async Task<IEnumerable<Inventory>> GetByStatusAsync(string status)
        {
            return await GetBaseQuery()
                .Include(inv => inv.Item)
                .Include(inv => inv.Location)
                .Where(inv => inv.Status == status)
                .OrderBy(inv => inv.Item.ItemCode)
                .ToListAsync();
        }

        public async Task<IEnumerable<Inventory>> GetBySourceReferenceAsync(string sourceReference)
        {
            return await GetBaseQuery()
                .Include(inv => inv.Item)
                .Include(inv => inv.Location)
                .Where(inv => inv.SourceReference == sourceReference)
                .ToListAsync();
        }

        #endregion

        #region Stock and Quantity Operations

        public async Task<bool> ExistsAtLocationAsync(int itemId, int locationId)
        {
            return await GetBaseQuery()
                .AnyAsync(inv => inv.ItemId == itemId && inv.LocationId == locationId);
        }

        public async Task<int> GetTotalStockByItemAsync(int itemId)
        {
            return await GetBaseQuery()
                .Where(inv => inv.ItemId == itemId && inv.Status == Constants.INVENTORY_STATUS_AVAILABLE)
                .SumAsync(inv => inv.Quantity);
        }

        public async Task<IEnumerable<Inventory>> GetLowStockInventoriesAsync(int threshold = 10)
        {
            return await GetBaseQuery()
                .Include(inv => inv.Item)
                .Include(inv => inv.Location)
                .Where(inv => inv.Quantity <= threshold && inv.Quantity > 0)
                .OrderBy(inv => inv.Quantity)
                .ToListAsync();
        }

        public async Task<IEnumerable<Inventory>> GetEmptyLocationsAsync()
        {
            return await GetBaseQuery()
                .Include(inv => inv.Item)
                .Include(inv => inv.Location)
                .Where(inv => inv.Quantity == 0 || inv.Status == Constants.INVENTORY_STATUS_EMPTY)
                .OrderBy(inv => inv.Location.Code)
                .ToListAsync();
        }

        public async Task<bool> UpdateQuantityAsync(int inventoryId, int newQuantity)
        {
            try
            {
                var inventory = await GetByIdAsync(inventoryId);
                if (inventory == null) return false;

                inventory.Quantity = newQuantity;
                inventory.LastUpdated = DateTime.Now;

                // Update status based on quantity
                if (newQuantity == 0)
                {
                    inventory.Status = Constants.INVENTORY_STATUS_EMPTY;
                }
                else if (inventory.Status == Constants.INVENTORY_STATUS_EMPTY)
                {
                    inventory.Status = Constants.INVENTORY_STATUS_AVAILABLE;
                }

                await UpdateAsync(inventory);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory quantity for ID {InventoryId}", inventoryId);
                return false;
            }
        }

        public async Task<Inventory> AddOrUpdateStockAsync(int itemId, int locationId, int quantity, decimal costPrice, string? sourceReference = null)
        {
            try
            {
                var existingInventory = await GetByItemAndLocationAsync(itemId, locationId);

                if (existingInventory != null)
                {
                    // Update existing inventory dengan weighted average cost
                    existingInventory.AddStock(quantity, costPrice);

                    if (!string.IsNullOrEmpty(sourceReference))
                    {
                        existingInventory.SourceReference = sourceReference;
                    }

                    await UpdateAsync(existingInventory);
                    return existingInventory;
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
                        SourceReference = sourceReference,
                        Notes = "Putaway from ASN"
                    };

                    return await AddAsync(newInventory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding/updating stock for Item {ItemId} at Location {LocationId}", itemId, locationId);
                throw;
            }
        }

        public async Task<bool> ReduceStockAsync(int inventoryId, int quantity)
        {
            try
            {
                var inventory = await GetByIdAsync(inventoryId);
                if (inventory == null || !inventory.ReduceStock(quantity))
                    return false;

                await UpdateAsync(inventory);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reducing stock for Inventory ID {InventoryId}", inventoryId);
                return false;
            }
        }

        #endregion

        #region Statistics and Analytics

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            return await GetBaseQuery()
                .SumAsync(inv => inv.Quantity * inv.LastCostPrice);
        }

        public async Task<Dictionary<string, object>> GetInventoryStatisticsAsync()
        {
            var allInventories = await GetBaseQuery()
                .Include(inv => inv.Item)
                .Include(inv => inv.Location)
                .ToListAsync();

            var stats = new Dictionary<string, object>
            {
                ["TotalItems"] = allInventories.Sum(inv => inv.Quantity),
                ["TotalValue"] = allInventories.Sum(inv => inv.TotalValue),
                ["TotalUniqueItems"] = allInventories.Select(inv => inv.ItemId).Distinct().Count(),
                ["TotalUsedLocations"] = allInventories.Where(inv => inv.Quantity > 0).Select(inv => inv.LocationId).Distinct().Count(),
                ["AvailableStock"] = allInventories.Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE).Sum(inv => inv.Quantity),
                ["ReservedStock"] = allInventories.Where(inv => inv.Status == Constants.INVENTORY_STATUS_RESERVED).Sum(inv => inv.Quantity),
                ["DamagedStock"] = allInventories.Where(inv => inv.Status == Constants.INVENTORY_STATUS_DAMAGED).Sum(inv => inv.Quantity),
                ["LowStockItems"] = allInventories.Count(inv => inv.NeedsReorder),
                ["EmptyLocations"] = allInventories.Count(inv => inv.Quantity == 0)
            };

            return stats;
        }

        #endregion

        #region Location and Putaway Operations

        public async Task<IEnumerable<Location>> GetAvailableLocationsForPutawayAsync()
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue)
            {
                _logger.LogWarning("No company ID found for current user, returning empty locations");
                return new List<Location>();
            }

            _logger.LogInformation("Getting available locations for company {CompanyId}", companyId.Value);

            // Get locations yang belum penuh atau masih bisa menampung stock
            var availableLocations = await _context.Locations
                .Include(loc => loc.Inventories)
                .Where(loc => loc.CompanyId == companyId.Value &&
                             loc.IsActive)
                .OrderBy(loc => loc.Code)
                .ToListAsync();

            _logger.LogInformation("Found {Count} active locations for company {CompanyId}", availableLocations.Count, companyId.Value);

            // Calculate current capacity from inventories for each location
            foreach (var location in availableLocations)
            {
                var currentCapacity = location.Inventories
                    .Where(inv => inv.CompanyId == companyId.Value)
                    .Sum(inv => inv.Quantity);

                location.CurrentCapacity = currentCapacity;
                
                // Ensure MaxCapacity is valid (greater than 0)
                if (location.MaxCapacity <= 0)
                {
                    _logger.LogWarning("Location {LocationCode} has invalid MaxCapacity {MaxCapacity}, setting to 1000", location.Code, location.MaxCapacity);
                    location.MaxCapacity = 1000; // Default capacity
                }
                
                location.IsFull = currentCapacity >= location.MaxCapacity;

                _logger.LogDebug("Location {LocationCode}: CurrentCapacity={CurrentCapacity}, MaxCapacity={MaxCapacity}, IsFull={IsFull}", 
                    location.Code, currentCapacity, location.MaxCapacity, location.IsFull);
            }

            // Filter out full locations
            var nonFullLocations = availableLocations.Where(loc => !loc.IsFull).ToList();
            _logger.LogInformation("Found {Count} available (non-full) locations for putaway", nonFullLocations.Count);

            // If no locations found, create a default location for testing
            if (!nonFullLocations.Any())
            {
                _logger.LogWarning("No available locations found. This might cause putaway to fail.");
                
                // Check if there are any locations at all
                var allLocations = await _context.Locations
                    .Where(loc => loc.CompanyId == companyId.Value)
                    .ToListAsync();
                
                _logger.LogInformation("Total locations in database for company {CompanyId}: {Count}", companyId.Value, allLocations.Count);
            }

            return nonFullLocations;
        }

        /// <summary>
        /// Get available locations for putaway with capacity filtering
        /// </summary>
        public async Task<IEnumerable<Location>> GetAvailableLocationsForQuantityAsync(int requiredQuantity)
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue)
            {
                _logger.LogWarning("No company ID found for current user, returning empty locations");
                return new List<Location>();
            }

            _logger.LogInformation("Getting available locations for quantity {Quantity} for company {CompanyId}", requiredQuantity, companyId.Value);

            // Get all active locations
            var allLocations = await _context.Locations
                .Include(loc => loc.Inventories)
                .Where(loc => loc.CompanyId == companyId.Value && loc.IsActive)
                .OrderBy(loc => loc.Code)
                .ToListAsync();

            // Calculate current capacity and filter by required quantity
            var availableLocations = new List<Location>();
            
            foreach (var location in allLocations)
            {
                var currentCapacity = location.Inventories
                    .Where(inv => inv.CompanyId == companyId.Value)
                    .Sum(inv => inv.Quantity);

                location.CurrentCapacity = currentCapacity;
                
                // Ensure MaxCapacity is valid
                if (location.MaxCapacity <= 0)
                {
                    location.MaxCapacity = 1000; // Default capacity
                }
                
                location.IsFull = currentCapacity >= location.MaxCapacity;
                
                // Check if location can accommodate the required quantity
                var availableCapacity = location.MaxCapacity - currentCapacity;
                if (availableCapacity >= requiredQuantity)
                {
                    availableLocations.Add(location);
                    _logger.LogDebug("Location {LocationCode} can accommodate {RequiredQuantity} (Available: {AvailableCapacity})", 
                        location.Code, requiredQuantity, availableCapacity);
                }
                else
                {
                    _logger.LogDebug("Location {LocationCode} cannot accommodate {RequiredQuantity} (Available: {AvailableCapacity})", 
                        location.Code, requiredQuantity, availableCapacity);
                }
            }

            _logger.LogInformation("Found {Count} locations that can accommodate quantity {Quantity}", availableLocations.Count, requiredQuantity);
            return availableLocations;
        }

        public async Task<bool> BulkUpdateStatusAsync(IEnumerable<int> inventoryIds, string status)
        {
            try
            {
                var inventories = await GetBaseQuery()
                    .Where(inv => inventoryIds.Contains(inv.Id))
                    .ToListAsync();

                foreach (var inventory in inventories)
                {
                    inventory.UpdateStatus(status, $"Bulk update on {DateTime.Now:yyyy-MM-dd HH:mm}");
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating inventory status");
                return false;
            }
        }

        #endregion

        #region Search and Filter Operations

        public async Task<IEnumerable<Inventory>> GetInventoryMovementsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = GetBaseQuery()
                .Include(inv => inv.Item)
                .Include(inv => inv.Location)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(inv => inv.ModifiedDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(inv => inv.ModifiedDate <= toDate.Value);
            }

            return await query
                .OrderByDescending(inv => inv.ModifiedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Inventory>> GetAvailableForSaleAsync()
        {
            return await GetBaseQuery()
                .Include(inv => inv.Item)
                .Include(inv => inv.Location)
                .Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE && inv.Quantity > 0)
                .OrderBy(inv => inv.Item.ItemCode)
                .ToListAsync();
        }

        public async Task<IEnumerable<Inventory>> SearchInventoryAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllWithDetailsAsync();

            var lowerSearchTerm = searchTerm.ToLower();

            return await GetBaseQuery()
                .Include(inv => inv.Item)
                    .ThenInclude(item => item.Supplier)
                .Include(inv => inv.Location)
                .Where(inv => inv.Item.ItemCode.ToLower().Contains(lowerSearchTerm) ||
                             inv.Item.Name.ToLower().Contains(lowerSearchTerm) ||
                             inv.Location.Code.ToLower().Contains(lowerSearchTerm) ||
                             inv.Location.Name.ToLower().Contains(lowerSearchTerm))
                .OrderBy(inv => inv.Item.ItemCode)
                .ToListAsync();
        }

        #endregion
    }
}