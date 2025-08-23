using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.Services;

namespace WMS.Data.Repositories
{
    public class ItemRepository : Repository<Item>, IItemRepository
    {
        public ItemRepository(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<Repository<Item>> logger) : base(context, currentUserService, logger)
        {
        }

        public async Task<IEnumerable<Item>> GetAllWithInventoryAsync()
        {
            return await GetBaseQuery()
                .Include(i => i.Inventories)
                    .ThenInclude(inv => inv.Location)
                .OrderBy(i => i.ItemCode)
                .ToListAsync();
        }

        public async Task<Item?> GetByIdWithInventoryAsync(int id)
        {
            return await GetBaseQuery()
                .Include(i => i.Inventories)
                    .ThenInclude(inv => inv.Location)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Item?> GetByItemCodeAsync(string itemCode)
        {
            return await GetBaseQuery()
                .FirstOrDefaultAsync(i => i.ItemCode == itemCode);
        }

        public async Task<IEnumerable<Item>> GetActiveItemsAsync()
        {
            return await GetBaseQuery()
                .Where(i => i.IsActive)
                .OrderBy(i => i.ItemCode)
                .ToListAsync();
        }

        public async Task<bool> ExistsByItemCodeAsync(string itemCode)
        {
            return await GetBaseQuery().AnyAsync(i => i.ItemCode == itemCode);
        }

        public async Task<IEnumerable<Item>> SearchItemsAsync(string searchTerm)
        {
            return await GetBaseQuery()
                .Where(i => i.IsActive &&
                           (i.ItemCode.Contains(searchTerm) ||
                            i.Name.Contains(searchTerm) ||
                            (i.Description != null && i.Description.Contains(searchTerm))))
                .OrderBy(i => i.ItemCode)
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetItemStockSummaryAsync()
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue) return new Dictionary<int, int>();

            return await _context.Inventories
                .Where(i => i.CompanyId == companyId.Value && i.Status == "Available")
                .GroupBy(i => i.ItemId)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(i => i.Quantity));
        }

        public async Task<IEnumerable<Item>> GetItemsWithLowStockAsync(int threshold = 10)
        {
            return await GetBaseQuery()
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

                return await DeleteAsync(entity);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}