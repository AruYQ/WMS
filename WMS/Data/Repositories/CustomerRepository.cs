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

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await GetBaseQuery().AnyAsync(c => c.Email == email);
        }

        public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm)
        {
            return await GetBaseQuery()
                .Where(c => c.IsActive &&
                           (c.Name.Contains(searchTerm) ||
                            c.Email.Contains(searchTerm)))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}