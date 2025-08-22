using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.Utilities;

namespace WMS.Data.Repositories
{
    public class InventoryRepository : Repository<Inventory>, IInventoryRepository
    {
        public InventoryRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Inventory>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(i => i.Item)
                .Include(i => i.Location)
                .OrderBy(i => i.Item.ItemCode)
                .ThenBy(i => i.Location.Code)
                .ToListAsync();
        }

        public async Task<Inventory?> GetByIdWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(i => i.Item)
                .Include(i => i.Location)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<IEnumerable<Inventory>> GetByItemAsync(int itemId)
        {
            return await _dbSet
                .Include(i => i.Location)
                .Where(i => i.ItemId == itemId)
                .OrderBy(i => i.Location.Code)
                .ToListAsync();
        }

        public async Task<IEnumerable<Inventory>> GetByLocationAsync(int locationId)
        {
            return await _dbSet
                .Include(i => i.Item)
                .Where(i => i.LocationId == locationId)
                .OrderBy(i => i.Item.ItemCode)
                .ToListAsync();
        }

        public async Task<IEnumerable<Inventory>> GetByStatusAsync(InventoryStatus status)
        {
            return await _dbSet
                .Include(i => i.Item)
                .Include(i => i.Location)
                .Where(i => i.Status == status.ToString())
                .ToListAsync();
        }

        public async Task<IEnumerable<Inventory>> GetAvailableInventoryAsync()
        {
            return await _dbSet
                .Include(i => i.Item)
                .Include(i => i.Location)
                .Where(i => i.Status == InventoryStatus.Available.ToString() && i.Quantity > 0)
                .OrderBy(i => i.Item.ItemCode)
                .ThenBy(i => i.Location.Code)
                .ToListAsync();
        }

        public async Task<IEnumerable<Inventory>> GetLowStockInventoryAsync(int threshold = 10)
        {
            return await _dbSet
                .Include(i => i.Item)
                .Include(i => i.Location)
                .Where(i => i.Quantity <= threshold && i.Status == InventoryStatus.Available.ToString())
                .OrderBy(i => i.Quantity)
                .ToListAsync();
        }

        public async Task<Inventory?> GetByItemAndLocationAsync(int itemId, int locationId)
        {
            return await _dbSet
                .Include(i => i.Item)
                .Include(i => i.Location)
                .FirstOrDefaultAsync(i => i.ItemId == itemId && i.LocationId == locationId);
        }

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            return await _dbSet
                .Where(i => i.Status == InventoryStatus.Available.ToString())
                .SumAsync(i => i.Quantity * i.LastCostPrice);
        }

        public async Task<Dictionary<string, int>> GetInventoryByStatusAsync()
        {
            return await _dbSet
                .GroupBy(i => i.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(i => i.Quantity));
        }

        public async Task<Dictionary<int, int>> GetItemStockSummaryAsync()
        {
            return await _dbSet
                .Where(i => i.Status == InventoryStatus.Available.ToString())
                .GroupBy(i => i.ItemId)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(i => i.Quantity));
        }

        public async Task UpdateStockAsync(int itemId, int locationId, int quantity, decimal costPrice)
        {
            var inventory = await GetByItemAndLocationAsync(itemId, locationId);

            if (inventory == null)
            {
                // Create new inventory record
                inventory = new Inventory
                {
                    ItemId = itemId,
                    LocationId = locationId,
                    Quantity = quantity,
                    LastCostPrice = costPrice,
                    Status = InventoryStatus.Available.ToString(),
                    LastUpdated = DateTime.Now
                };

                await AddAsync(inventory);
            }
            else
            {
                // Update existing inventory
                inventory.AddStock(quantity, costPrice);
                await UpdateAsync(inventory);
            }
        }

        public async Task<bool> CheckStockAvailabilityAsync(int itemId, int requiredQuantity)
        {
            var totalStock = await _dbSet
                .Where(i => i.ItemId == itemId && i.Status == InventoryStatus.Available.ToString())
                .SumAsync(i => i.Quantity);

            return totalStock >= requiredQuantity;
        }

        public async Task<IEnumerable<Inventory>> GetInventoryForPutawayAsync()
        {
            // Get inventory in receiving location or newly created
            return await _dbSet
                .Include(i => i.Item)
                .Include(i => i.Location)
                .Where(i => i.Location.Code == "RECEIVING" ||
                           i.CreatedDate >= DateTime.Today)
                .OrderBy(i => i.CreatedDate)
                .ToListAsync();
        }

        // Override the base DeleteAsync method to return bool indicating success/failure
        public new async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var entity = await GetByIdAsync(id);
                if (entity == null)
                    return false;

                _dbSet.Remove(entity);
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}