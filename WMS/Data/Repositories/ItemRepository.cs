using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;
using WMS.Models.ViewModels;
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
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> SearchItemsAsync(string searchTerm)
        {
            return await GetBaseQuery()
                .Where(i => i.IsActive &&
                           (i.Name.Contains(searchTerm) ||
                            i.ItemCode.Contains(searchTerm) ||
                            i.Description.Contains(searchTerm)))
                .Include(i => i.Supplier)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetItemsWithoutSupplierAsync()
        {
            return await GetBaseQuery()
                .Where(i => i.SupplierId == null)
                .Include(i => i.Supplier)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> SearchItemsBySupplierAsync(string searchTerm, int supplierId)
        {
            return await GetBaseQuery()
                .Where(i => i.SupplierId == supplierId &&
                           (i.Name.Contains(searchTerm) ||
                            i.ItemCode.Contains(searchTerm) ||
                            i.Description.Contains(searchTerm)))
                .Include(i => i.Supplier)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetItemsWithLowStockAsync(int threshold = 10)
        {
            return await GetBaseQuery()
                .Where(i => i.IsActive)
                .Include(i => i.Supplier)
                .Include(i => i.Inventories)
                .Where(i => i.Inventories.Sum(inv => inv.Quantity) <= threshold)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Inventory>> GetInventoriesByItemIdAsync(int itemId)
        {
            return await _context.Inventories
                .Where(inv => inv.ItemId == itemId)
                .Include(inv => inv.Location)
                .OrderBy(inv => inv.Location.Code)
                .ToListAsync();
        }

        public async Task<IEnumerable<PurchaseOrderDetail>> GetPurchaseOrderDetailsByItemIdAsync(int itemId)
        {
            return await _context.PurchaseOrderDetails
                .Where(pod => pod.ItemId == itemId)
                .Include(pod => pod.PurchaseOrder)
                .OrderByDescending(pod => pod.PurchaseOrder.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ASNDetail>> GetASNDetailsByItemIdAsync(int itemId)
        {
            return await _context.ASNDetails
                .Where(ad => ad.ItemId == itemId)
                .Include(ad => ad.ASN)
                .OrderByDescending(ad => ad.ASN.ShipmentDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<SalesOrderDetail>> GetSalesOrderDetailsByItemIdAsync(int itemId)
        {
            return await _context.SalesOrderDetails
                .Where(sod => sod.ItemId == itemId)
                .Include(sod => sod.SalesOrder)
                .OrderByDescending(sod => sod.SalesOrder.OrderDate)
                .ToListAsync();
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
            return await GetBaseQuery()
                .Where(i => i.IsActive)
                .Include(i => i.Inventories)
                .ToDictionaryAsync(
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

        public async Task<IEnumerable<Item>> SearchAsync(WMS.Models.ViewModels.ItemSearchRequest request)
        {
            var query = GetBaseQuery();

            // Apply filters
            if (!string.IsNullOrEmpty(request.SearchText))
            {
                query = query.Where(i => i.Name.Contains(request.SearchText) ||
                                       i.ItemCode.Contains(request.SearchText) ||
                                       i.Description.Contains(request.SearchText));
            }

            if (!string.IsNullOrEmpty(request.StatusFilter))
            {
                if (request.StatusFilter == "active")
                    query = query.Where(i => i.IsActive);
                else if (request.StatusFilter == "inactive")
                    query = query.Where(i => !i.IsActive);
            }

            if (request.SupplierFilter.HasValue)
            {
                query = query.Where(i => i.SupplierId == request.SupplierFilter.Value);
            }

            if (request.PriceFrom.HasValue)
            {
                query = query.Where(i => i.StandardPrice >= request.PriceFrom.Value);
            }

            if (request.PriceTo.HasValue)
            {
                query = query.Where(i => i.StandardPrice <= request.PriceTo.Value);
            }

            if (request.DateFrom.HasValue)
            {
                query = query.Where(i => i.CreatedDate >= request.DateFrom.Value);
            }

            if (request.DateTo.HasValue)
            {
                query = query.Where(i => i.CreatedDate <= request.DateTo.Value);
            }

            // Apply pagination and include
            return await query
                .Include(i => i.Supplier)
                .OrderBy(i => i.Name)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> QuickSearchAsync(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return await GetBaseQuery()
                    .Include(i => i.Supplier)
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.Name)
                    .Take(10)
                    .ToListAsync();
            }

            return await GetBaseQuery()
                .Include(i => i.Supplier)
                .Where(i => i.IsActive &&
                           (i.Name.Contains(query) ||
                            i.ItemCode.Contains(query) ||
                            i.Description.Contains(query)))
                .OrderBy(i => i.Name)
                .Take(10)
                .ToListAsync();
        }
    }
}