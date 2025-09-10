using Microsoft.EntityFrameworkCore;
using WMS.Data;
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
                .Include(i => i.Supplier)
                .Include(i => i.Inventories)
                    .ThenInclude(inv => inv.Location)
                .ToListAsync();
        }

        public async Task<Item?> GetByIdWithInventoryAsync(int id)
        {
            return await GetBaseQuery()
                .Include(i => i.Supplier)
                .Include(i => i.Inventories)
                    .ThenInclude(inv => inv.Location)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Item?> GetByItemCodeAsync(string itemCode)
        {
            return await GetBaseQuery()
                .Include(i => i.Supplier)
                .Include(i => i.Inventories)
                    .ThenInclude(inv => inv.Location)
                .FirstOrDefaultAsync(i => i.ItemCode == itemCode);
        }

        public async Task<IEnumerable<Item>> GetBySupplierIdAsync(int supplierId)
        {
            return await GetBaseQuery()
                .Where(i => i.SupplierId == supplierId)
                .Include(i => i.Supplier)
                .Include(i => i.Inventories)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetActiveItemsAsync()
        {
            return await GetBaseQuery()
                .Where(i => i.IsActive)
                .Include(i => i.Supplier)
                .Include(i => i.Inventories)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> SearchItemsAsync(string searchTerm)
        {
            return await GetBaseQuery()
                .Where(i => i.Name.Contains(searchTerm) || i.ItemCode.Contains(searchTerm))
                .Include(i => i.Supplier)
                .Include(i => i.Inventories)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetItemsWithoutSupplierAsync()
        {
            return await GetBaseQuery()
                .Where(i => i.SupplierId == null)
                .Include(i => i.Inventories)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> SearchItemsBySupplierAsync(string searchTerm, int supplierId)
        {
            return await GetBaseQuery()
                .Where(i => i.SupplierId == supplierId && 
                           (i.Name.Contains(searchTerm) || i.ItemCode.Contains(searchTerm)))
                .Include(i => i.Supplier)
                .Include(i => i.Inventories)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetItemsWithLowStockAsync(int threshold = 10)
        {
            return await GetBaseQuery()
                .Include(i => i.Supplier)
                .Include(i => i.Inventories)
                .Where(i => i.Inventories.Sum(inv => inv.Quantity) <= threshold)
                .ToListAsync();
        }

        public async Task<IEnumerable<Inventory>> GetInventoriesByItemIdAsync(int itemId)
        {
            return await _context.Inventories
                .Include(i => i.Location)
                .Where(i => i.ItemId == itemId)
                .ToListAsync();
        }

        public async Task<IEnumerable<PurchaseOrderDetail>> GetPurchaseOrderDetailsByItemIdAsync(int itemId)
        {
            return await _context.PurchaseOrderDetails
                .Include(pod => pod.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                .Where(pod => pod.ItemId == itemId)
                .ToListAsync();
        }

        public async Task<IEnumerable<ASNDetail>> GetASNDetailsByItemIdAsync(int itemId)
        {
            return await _context.ASNDetails
                .Include(ad => ad.ASN)
                .Where(ad => ad.ItemId == itemId)
                .ToListAsync();
        }

        public async Task<IEnumerable<SalesOrderDetail>> GetSalesOrderDetailsByItemIdAsync(int itemId)
        {
            // Sales Order - DISABLED
            return new List<SalesOrderDetail>();
        }

        public async Task<bool> IsItemCodeUniqueAsync(string itemCode, int? excludeId = null)
        {
            var query = GetBaseQuery().Where(i => i.ItemCode == itemCode);
            
            if (excludeId.HasValue)
            {
                query = query.Where(i => i.Id != excludeId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<Dictionary<int, int>> GetItemStockSummaryAsync()
        {
            var items = await GetBaseQuery()
                .Include(i => i.Inventories)
                .ToListAsync();

            return items.ToDictionary(
                item => item.Id,
                item => item.Inventories.Sum(inv => inv.Quantity)
            );
        }

        public async Task<IEnumerable<Supplier>> GetActiveSuppliersForDropdownAsync()
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue)
            {
                return new List<Supplier>();
            }

            return await _context.Suppliers
                .Where(s => s.CompanyId == companyId.Value && s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}