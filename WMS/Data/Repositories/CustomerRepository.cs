using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;

namespace WMS.Data.Repositories
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<Repository<Customer>> logger) : base(context, currentUserService, logger)
        {
        }

        public async Task<IEnumerable<Customer>> GetAllWithSalesOrdersAsync()
        {
            return await GetBaseQuery()
                .Include(c => c.SalesOrders)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Customer?> GetByIdWithSalesOrdersAsync(int id)
        {
            return await GetBaseQuery()
                .Include(c => c.SalesOrders)
                    .ThenInclude(so => so.SalesOrderDetails)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
        {
            return await GetBaseQuery()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
        {
            var query = GetBaseQuery().Where(c => c.Email == email);
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null)
        {
            var query = GetBaseQuery().Where(c => c.Phone == phone);
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm)
        {
            return await GetBaseQuery()
                .Where(c => c.Name.Contains(searchTerm) ||
                           c.Email.Contains(searchTerm) ||
                           (c.Phone != null && c.Phone.Contains(searchTerm)) ||
                           (c.Address != null && c.Address.Contains(searchTerm)))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Customer>> GetCustomersWithSalesOrdersAsync()
        {
            return await GetBaseQuery()
                .Include(c => c.SalesOrders)
                .Where(c => c.SalesOrders.Any())
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Customer>> SearchAsync(WMS.Models.ViewModels.CustomerSearchRequest request)
        {
            var query = GetBaseQuery();

            // Apply filters
            if (!string.IsNullOrEmpty(request.SearchText))
            {
                query = query.Where(c => c.Name.Contains(request.SearchText) ||
                                       c.Email.Contains(request.SearchText) ||
                                       c.Phone.Contains(request.SearchText) ||
                                       c.Code.Contains(request.SearchText));
            }

            if (!string.IsNullOrEmpty(request.StatusFilter))
            {
                if (request.StatusFilter == "active")
                    query = query.Where(c => c.IsActive);
                else if (request.StatusFilter == "inactive")
                    query = query.Where(c => !c.IsActive);
            }

            if (!string.IsNullOrEmpty(request.CityFilter))
            {
                query = query.Where(c => c.City.Contains(request.CityFilter));
            }

            if (!string.IsNullOrEmpty(request.CustomerTypeFilter))
            {
                query = query.Where(c => c.CustomerType == request.CustomerTypeFilter);
            }

            if (request.DateFrom.HasValue)
            {
                query = query.Where(c => c.CreatedDate >= request.DateFrom.Value);
            }

            if (request.DateTo.HasValue)
            {
                query = query.Where(c => c.CreatedDate <= request.DateTo.Value);
            }

            // Apply pagination
            return await query
                .OrderBy(c => c.Name)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Customer>> QuickSearchAsync(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return await GetBaseQuery()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Take(10)
                    .ToListAsync();
            }

            return await GetBaseQuery()
                .Where(c => c.IsActive &&
                           (c.Name.Contains(query) ||
                            c.Code.Contains(query) ||
                            c.Email.Contains(query)))
                .OrderBy(c => c.Name)
                .Take(10)
                .ToListAsync();
        }
    }
}