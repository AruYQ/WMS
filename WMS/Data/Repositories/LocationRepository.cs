using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.Services;

namespace WMS.Data.Repositories
{
    public class LocationRepository : Repository<Location>, ILocationRepository
    {
        public LocationRepository(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<LocationRepository> logger)
            : base(context, currentUserService, logger)
        {
        }

        protected override IQueryable<Location> GetBaseQuery()
        {
            return base.GetBaseQuery().Include(l => l.Inventories);
        }

        // Basic location queries
        public async Task<Location?> GetByCodeAsync(string code)
        {
            return await GetBaseQuery()
                .FirstOrDefaultAsync(l => l.Code == code);
        }

        public async Task<IEnumerable<Location>> GetByZoneAsync(string zone)
        {
            // Assuming zone is part of Code format (e.g., A-01-01 where A is zone)
            return await GetBaseQuery()
                .Where(l => l.Code.StartsWith(zone))
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetByLocationTypeAsync(string locationType)
        {
            // Since the model doesn't have LocationType field, we'll use Description or Name
            return await GetBaseQuery()
                .Where(l => l.Description != null && l.Description.Contains(locationType))
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetByStatusAsync(string status)
        {
            var isActive = status.ToLower() == "active";
            return await GetBaseQuery()
                .Where(l => l.IsActive == isActive)
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        // Capacity-based queries
        public async Task<IEnumerable<Location>> GetAvailableLocationsAsync()
        {
            return await GetBaseQuery()
                .Where(l => l.IsActive && !l.IsFull && l.AvailableCapacity > 0)
                .OrderByDescending(l => l.AvailableCapacity)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetLocationsByMinCapacityAsync(int minCapacity)
        {
            return await GetBaseQuery()
                .Where(l => l.IsActive && l.AvailableCapacity >= minCapacity)
                .OrderByDescending(l => l.AvailableCapacity)
                .ToListAsync();
        }

        public async Task<Location?> GetBestLocationForItemAsync(int itemId, int requiredCapacity)
        {
            // First try to find existing location with same item
            var existingLocation = await GetBaseQuery()
                .Where(l => l.IsActive &&
                           l.AvailableCapacity >= requiredCapacity &&
                           l.Inventories.Any(i => i.ItemId == itemId))
                .OrderByDescending(l => l.AvailableCapacity)
                .FirstOrDefaultAsync();

            if (existingLocation != null)
                return existingLocation;

            // If not found, get location with most available space
            return await GetBaseQuery()
                .Where(l => l.IsActive && l.AvailableCapacity >= requiredCapacity)
                .OrderByDescending(l => l.AvailableCapacity)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Location>> GetSuggestedPutawayLocationsAsync(int itemId)
        {
            // Get all active locations that can accommodate at least 1 item
            var locations = await GetBaseQuery()
                .Where(l => l.IsActive && l.AvailableCapacity > 0)
                .ToListAsync();

            // Sort by priority: existing item locations first, then by available capacity
            return locations
                .OrderByDescending(l => l.Inventories.Any(i => i.ItemId == itemId))
                .ThenByDescending(l => l.AvailableCapacity)
                .Take(10) // Return top 10 suggestions
                .ToList();
        }

        // Location hierarchy queries (simplified since model doesn't have hierarchy fields)
        public async Task<IEnumerable<Location>> GetByAisleAsync(string aisle)
        {
            // Assuming aisle is first part of Code (e.g., A in A-01-01)
            return await GetBaseQuery()
                .Where(l => l.Code.StartsWith(aisle + "-"))
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetByRackAsync(string aisle, string rack)
        {
            var prefix = $"{aisle}-{rack}";
            return await GetBaseQuery()
                .Where(l => l.Code.StartsWith(prefix))
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetByLevelAsync(string aisle, string rack, string level)
        {
            var codePattern = $"{aisle}-{rack}-{level}";
            return await GetBaseQuery()
                .Where(l => l.Code == codePattern)
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        // Temperature controlled locations (simplified - model doesn't have temperature fields)
        public async Task<IEnumerable<Location>> GetTemperatureControlledLocationsAsync()
        {
            // Since model doesn't have temperature fields, return locations with "COLD" or "TEMP" in description
            return await GetBaseQuery()
                .Where(l => l.IsActive &&
                           (l.Description != null &&
                            (l.Description.Contains("COLD") || l.Description.Contains("TEMP"))))
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetLocationsByTemperatureRangeAsync(decimal minTemp, decimal maxTemp)
        {
            // Simplified implementation - return temperature controlled locations
            return await GetTemperatureControlledLocationsAsync();
        }

        // Capacity management
        public async Task<bool> UpdateCapacityAsync(int locationId, int newCurrentCapacity)
        {
            try
            {
                var location = await GetByIdAsync(locationId);
                if (location == null) return false;

                location.CurrentCapacity = newCurrentCapacity;
                location.IsFull = newCurrentCapacity >= location.MaxCapacity;
                location.ModifiedDate = DateTime.Now;
                location.ModifiedBy = _currentUserService.Username;

                await UpdateAsync(location);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating capacity for location {LocationId}", locationId);
                return false;
            }
        }

        public async Task<bool> AddCapacityAsync(int locationId, int additionalCapacity)
        {
            try
            {
                var location = await GetByIdAsync(locationId);
                if (location == null) return false;

                location.CurrentCapacity += additionalCapacity;
                if (location.CurrentCapacity > location.MaxCapacity)
                    location.CurrentCapacity = location.MaxCapacity;

                location.IsFull = location.CurrentCapacity >= location.MaxCapacity;
                location.ModifiedDate = DateTime.Now;
                location.ModifiedBy = _currentUserService.Username;

                await UpdateAsync(location);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding capacity to location {LocationId}", locationId);
                return false;
            }
        }

        public async Task<bool> RemoveCapacityAsync(int locationId, int capacityToRemove)
        {
            try
            {
                var location = await GetByIdAsync(locationId);
                if (location == null) return false;

                location.CurrentCapacity -= capacityToRemove;
                if (location.CurrentCapacity < 0)
                    location.CurrentCapacity = 0;

                location.IsFull = location.CurrentCapacity >= location.MaxCapacity;
                location.ModifiedDate = DateTime.Now;
                location.ModifiedBy = _currentUserService.Username;

                await UpdateAsync(location);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing capacity from location {LocationId}", locationId);
                return false;
            }
        }

        // Location utilization
        public async Task<IEnumerable<Location>> GetFullLocationsAsync()
        {
            return await GetBaseQuery()
                .Where(l => l.IsFull)
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetEmptyLocationsAsync()
        {
            return await GetBaseQuery()
                .Where(l => l.CurrentCapacity == 0)
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetNearFullLocationsAsync(int threshold = 90)
        {
            var locations = await GetBaseQuery()
                .Where(l => l.MaxCapacity > 0)
                .ToListAsync();

            return locations
                .Where(l => l.CapacityPercentage >= threshold)
                .OrderByDescending(l => l.CapacityPercentage)
                .ToList();
        }

        // Inventory-related queries
        public async Task<IEnumerable<Location>> GetLocationsWithInventoryAsync()
        {
            return await GetBaseQuery()
                .Where(l => l.Inventories.Any())
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetLocationsByItemAsync(int itemId)
        {
            return await GetBaseQuery()
                .Where(l => l.Inventories.Any(i => i.ItemId == itemId))
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        public async Task<Location?> GetLocationWithMostAvailableSpaceAsync()
        {
            return await GetBaseQuery()
                .Where(l => l.IsActive && l.AvailableCapacity > 0)
                .OrderByDescending(l => l.AvailableCapacity)
                .FirstOrDefaultAsync();
        }

        // Active locations only
        public async Task<IEnumerable<Location>> GetActiveLocationsAsync()
        {
            return await GetBaseQuery()
                .Where(l => l.IsActive)
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetActiveLocationsByCapacityAsync(int minCapacity)
        {
            return await GetBaseQuery()
                .Where(l => l.IsActive && l.AvailableCapacity >= minCapacity)
                .OrderByDescending(l => l.AvailableCapacity)
                .ToListAsync();
        }

        /// <summary>
        /// Calculate current capacity from inventories for all locations
        /// </summary>
        public async Task UpdateAllCurrentCapacitiesAsync()
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    _logger.LogWarning("No company ID found for current user, skipping capacity update");
                    return;
                }

                var locations = await GetBaseQuery().ToListAsync();
                
                foreach (var location in locations)
                {
                    var currentCapacity = await _context.Inventories
                        .Where(inv => inv.LocationId == location.Id && inv.CompanyId == companyId.Value)
                        .SumAsync(inv => inv.Quantity);

                    location.CurrentCapacity = currentCapacity;
                    location.IsFull = currentCapacity >= location.MaxCapacity;
                    location.ModifiedDate = DateTime.Now;
                    location.ModifiedBy = _currentUserService.Username;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating current capacities for all locations");
                throw;
            }
        }

        /// <summary>
        /// Calculate current capacity from inventories for a specific location
        /// </summary>
        public async Task UpdateCurrentCapacityAsync(int locationId)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    _logger.LogWarning("No company ID found for current user, skipping capacity update");
                    return;
                }

                var location = await GetByIdAsync(locationId);
                if (location == null) return;

                var currentCapacity = await _context.Inventories
                    .Where(inv => inv.LocationId == locationId && inv.CompanyId == companyId.Value)
                    .SumAsync(inv => inv.Quantity);

                location.CurrentCapacity = currentCapacity;
                location.IsFull = currentCapacity >= location.MaxCapacity;
                location.ModifiedDate = DateTime.Now;
                location.ModifiedBy = _currentUserService.Username;

                await UpdateAsync(location);
                
                _logger.LogInformation("Updated location {LocationId} capacity: {Current}/{Max} (Available: {Available})", 
                    locationId, currentCapacity, location.MaxCapacity, location.AvailableCapacity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating current capacity for location {LocationId}", locationId);
                throw;
            }
        }

        /// <summary>
        /// Check if location has enough capacity for additional quantity
        /// </summary>
        public async Task<bool> CheckCapacityForPutawayAsync(int locationId, int additionalQuantity)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue) 
                {
                    _logger.LogWarning("No company ID found for current user, capacity check failed");
                    return false;
                }

                var location = await GetByIdAsync(locationId);
                if (location == null) 
                {
                    _logger.LogWarning("Location {LocationId} not found for capacity check", locationId);
                    return false;
                }

                var currentCapacity = await _context.Inventories
                    .Where(inv => inv.LocationId == locationId && inv.CompanyId == companyId.Value)
                    .SumAsync(inv => inv.Quantity);

                var availableCapacity = location.MaxCapacity - currentCapacity;
                var hasCapacity = availableCapacity >= additionalQuantity;

                _logger.LogInformation("Capacity check for LocationId={LocationId}: Current={Current}, Max={Max}, Available={Available}, Requested={Requested}, HasCapacity={HasCapacity}", 
                    locationId, currentCapacity, location.MaxCapacity, availableCapacity, additionalQuantity, hasCapacity);

                return hasCapacity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking capacity for putaway, location {LocationId}", locationId);
                return false;
            }
        }

        public async Task<IEnumerable<Location>> SearchAsync(LocationSearchRequest request)
        {
            var query = GetBaseQuery();

            // Apply filters
            if (!string.IsNullOrEmpty(request.SearchText))
            {
                query = query.Where(l => l.Name.Contains(request.SearchText) ||
                                       l.Code.Contains(request.SearchText) ||
                                       l.Description.Contains(request.SearchText));
            }

            if (!string.IsNullOrEmpty(request.StatusFilter))
            {
                if (request.StatusFilter == "active")
                    query = query.Where(l => l.IsActive);
                else if (request.StatusFilter == "inactive")
                    query = query.Where(l => !l.IsActive);
            }

            if (request.CapacityFrom.HasValue)
            {
                query = query.Where(l => l.MaxCapacity >= request.CapacityFrom.Value);
            }

            if (request.CapacityTo.HasValue)
            {
                query = query.Where(l => l.MaxCapacity <= request.CapacityTo.Value);
            }

            if (!string.IsNullOrEmpty(request.CapacityStatusFilter))
            {
                switch (request.CapacityStatusFilter)
                {
                    case "available":
                        query = query.Where(l => l.CurrentCapacity < l.MaxCapacity);
                        break;
                    case "full":
                        query = query.Where(l => l.IsFull);
                        break;
                    case "nearly-full":
                        query = query.Where(l => l.CurrentCapacity >= l.MaxCapacity * 0.8 && !l.IsFull);
                        break;
                }
            }

            if (request.DateFrom.HasValue)
            {
                query = query.Where(l => l.CreatedDate >= request.DateFrom.Value);
            }

            if (request.DateTo.HasValue)
            {
                query = query.Where(l => l.CreatedDate <= request.DateTo.Value);
            }

            // Apply pagination
            return await query
                .OrderBy(l => l.Code)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> QuickSearchAsync(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return await GetBaseQuery()
                    .Where(l => l.IsActive)
                    .OrderBy(l => l.Code)
                    .Take(10)
                    .ToListAsync();
            }

            return await GetBaseQuery()
                .Where(l => l.IsActive &&
                           (l.Name.Contains(query) ||
                            l.Code.Contains(query) ||
                            l.Description.Contains(query)))
                .OrderBy(l => l.Code)
                .Take(10)
                .ToListAsync();
        }
    }
}