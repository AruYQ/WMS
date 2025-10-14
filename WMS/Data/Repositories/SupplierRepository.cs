using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;

namespace WMS.Data.Repositories
{
    public class SupplierRepository : Repository<Supplier>, ISupplierRepository
    {
        public SupplierRepository(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<Repository<Supplier>> logger) : base(context, currentUserService, logger)
        {
        }

        public async Task<IEnumerable<Supplier>> GetAllWithPurchaseOrdersAsync()
        {
            return await GetBaseQuery()
                .Include(s => s.PurchaseOrders)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Supplier?> GetByIdWithPurchaseOrdersAsync(int id)
        {
            return await GetBaseQuery()
                .Include(s => s.PurchaseOrders)
                    .ThenInclude(po => po.PurchaseOrderDetails)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Supplier>> GetActiveSuppliers()
        {
            return await GetBaseQuery()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await GetBaseQuery().AnyAsync(s => s.Email == email && !s.IsDeleted);
        }

        public async Task<IEnumerable<Supplier>> SearchSuppliersAsync(string searchTerm)
        {
            return await GetBaseQuery()
                .Where(s => s.IsActive &&
                           (s.Name.Contains(searchTerm) ||
                            s.Email.Contains(searchTerm)))
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Supplier>> SearchAsync(WMS.Models.ViewModels.SupplierSearchRequest request)
        {
            var query = GetBaseQuery();

            // Apply filters
            if (!string.IsNullOrEmpty(request.SearchText))
            {
                query = query.Where(s => s.Name.Contains(request.SearchText) ||
                                       s.Email.Contains(request.SearchText) ||
                                       s.Phone.Contains(request.SearchText) ||
                                       s.Code.Contains(request.SearchText));
            }

            if (!string.IsNullOrEmpty(request.StatusFilter))
            {
                if (request.StatusFilter == "active")
                    query = query.Where(s => s.IsActive);
                else if (request.StatusFilter == "inactive")
                    query = query.Where(s => !s.IsActive);
            }

            if (!string.IsNullOrEmpty(request.CityFilter))
            {
                query = query.Where(s => s.City.Contains(request.CityFilter));
            }

            if (!string.IsNullOrEmpty(request.ContactPersonFilter))
            {
                query = query.Where(s => s.ContactPerson.Contains(request.ContactPersonFilter));
            }

            if (!string.IsNullOrEmpty(request.SupplierNameFilter))
            {
                query = query.Where(s => s.Name.Contains(request.SupplierNameFilter));
            }

            if (!string.IsNullOrEmpty(request.PhoneFilter))
            {
                query = query.Where(s => s.Phone.Contains(request.PhoneFilter));
            }

            if (!string.IsNullOrEmpty(request.SupplierCodeFilter))
            {
                query = query.Where(s => s.Code.Contains(request.SupplierCodeFilter));
            }

            if (request.DateFrom.HasValue)
            {
                query = query.Where(s => s.CreatedDate >= request.DateFrom.Value);
            }

            if (request.DateTo.HasValue)
            {
                query = query.Where(s => s.CreatedDate <= request.DateTo.Value);
            }

            // Apply pagination
            return await query
                .OrderBy(s => s.Name)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Supplier>> QuickSearchAsync(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return await GetBaseQuery()
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .Take(10)
                    .ToListAsync();
            }

            return await GetBaseQuery()
                .Where(s => s.IsActive &&
                           (s.Name.Contains(query) ||
                            s.Code.Contains(query) ||
                            s.Email.Contains(query)))
                .OrderBy(s => s.Name)
                .Take(10)
                .ToListAsync();
        }
    }
}