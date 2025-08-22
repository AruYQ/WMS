using Microsoft.EntityFrameworkCore;
using WMS.Models;

namespace WMS.Data.Repositories
{
    public class ItemRepository : Repository<Item>, IItemRepository
    {
        public ItemRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Item>> GetAllWithInventoryAsync()
        {
            return await _dbSet
                .Include(i => i.Inventories)
                    .ThenInclude(inv => inv.Location)
                .OrderBy(i => i.ItemCode)
                .ToListAsync();
        }

        public async Task<Item?> GetByIdWithInventoryAsync(int id)
        {
            return await _dbSet
                .Include(i => i.Inventories)
                    .ThenInclude(inv => inv.Location)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Item?> GetByItemCodeAsync(string itemCode)
        {
            return await _dbSet
                .FirstOrDefaultAsync(i => i.ItemCode == itemCode);
        }

        public async Task<IEnumerable<Item>> GetActiveItemsAsync()
        {
            return await _dbSet
                .Where(i => i.IsActive)
                .OrderBy(i => i.ItemCode)
                .ToListAsync();
        }

        public async Task<bool> ExistsByItemCodeAsync(string itemCode)
        {
            return await _dbSet.AnyAsync(i => i.ItemCode == itemCode);
        }

        public async Task<IEnumerable<Item>> SearchItemsAsync(string searchTerm)
        {
            return await _dbSet
                .Where(i => i.IsActive &&
                           (i.ItemCode.Contains(searchTerm) ||
                            i.Name.Contains(searchTerm) ||
                            (i.Description != null && i.Description.Contains(searchTerm))))
                .OrderBy(i => i.ItemCode)
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetItemStockSummaryAsync()
        {
            return await _context.Inventories
                .Where(i => i.Status == "Available")
                .GroupBy(i => i.ItemId)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(i => i.Quantity));
        }

        public async Task<IEnumerable<Item>> GetItemsWithLowStockAsync(int threshold = 10)
        {
            return await _dbSet
                .Include(i => i.Inventories)
                .Where(i => i.IsActive &&
                           i.Inventories.Where(inv => inv.Status == "Available")
                                       .Sum(inv => inv.Quantity) <= threshold)
                .OrderBy(i => i.ItemCode)
                .ToListAsync();
        }
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