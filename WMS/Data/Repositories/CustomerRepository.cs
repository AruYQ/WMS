using Microsoft.EntityFrameworkCore;
using WMS.Models;
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
    }
}