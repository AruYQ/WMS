using Microsoft.EntityFrameworkCore;
using WMS.Models;

namespace WMS.Data.Repositories
{
    public class LocationRepository : Repository<Location>, ILocationRepository
    {
        public LocationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Location>> GetAllWithInventoryAsync()
        {
            return await _dbSet
                .Include(l => l.Inventories)
                    .ThenInclude(i => i.Item)
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        public async Task<Location?> GetByIdWithInventoryAsync(int id)
        {
            return await _dbSet
                .Include(l => l.Inventories)
                    .ThenInclude(i => i.Item)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<Location?> GetByCodeAsync(string code)
        {
            return await _dbSet
                .FirstOrDefaultAsync(l => l.Code == code);
        }

        public async Task<IEnumerable<Location>> GetActiveLocationsAsync()
        {
            return await _dbSet
                .Where(l => l.IsActive)
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetAvailableLocationsAsync()
        {
            return await _dbSet
                .Where(l => l.IsActive && !l.IsFull)
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        public async Task<bool> ExistsByCodeAsync(string code)
        {
            return await _dbSet.AnyAsync(l => l.Code == code);
        }

        public async Task<IEnumerable<Location>> SearchLocationsAsync(string searchTerm)
        {
            return await _dbSet
                .Where(l => l.IsActive &&
                           (l.Code.Contains(searchTerm) ||
                            l.Name.Contains(searchTerm)))
                .OrderBy(l => l.Code)
                .ToListAsync();
        }

        public async Task UpdateCapacityAsync(int locationId)
        {
            var location = await GetByIdAsync(locationId);
            if (location != null)
            {
                var currentCapacity = await _context.Inventories
                    .Where(i => i.LocationId == locationId)
                    .SumAsync(i => i.Quantity);

                location.CurrentCapacity = currentCapacity;
                location.IsFull = currentCapacity >= location.MaxCapacity;

                await UpdateAsync(location);
            }
        }

        public async Task<Dictionary<string, object>> GetLocationStatisticsAsync()
        {
            var totalLocations = await _dbSet.CountAsync();
            var activeLocations = await _dbSet.CountAsync(l => l.IsActive);
            var fullLocations = await _dbSet.CountAsync(l => l.IsFull);
            var totalCapacity = await _dbSet.SumAsync(l => l.MaxCapacity);
            var usedCapacity = await _dbSet.SumAsync(l => l.CurrentCapacity);

            return new Dictionary<string, object>
            {
                ["TotalLocations"] = totalLocations,
                ["ActiveLocations"] = activeLocations,
                ["FullLocations"] = fullLocations,
                ["TotalCapacity"] = totalCapacity,
                ["UsedCapacity"] = usedCapacity,
                ["AvailableCapacity"] = totalCapacity - usedCapacity,
                ["CapacityUtilization"] = totalCapacity > 0 ? (double)usedCapacity / totalCapacity * 100 : 0
            };
        }
    }
}