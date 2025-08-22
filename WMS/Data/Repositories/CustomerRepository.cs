using Microsoft.EntityFrameworkCore;
using WMS.Models;

namespace WMS.Data.Repositories
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Customer>> GetAllWithSalesOrdersAsync()
        {
            return await _dbSet
                .Include(c => c.SalesOrders)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Customer?> GetByIdWithSalesOrdersAsync(int id)
        {
            return await _dbSet
                .Include(c => c.SalesOrders)
                    .ThenInclude(so => so.SalesOrderDetails)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
        {
            return await _dbSet
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _dbSet.AnyAsync(c => c.Email == email);
        }

        public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm)
        {
            return await _dbSet
                .Where(c => c.IsActive &&
                           (c.Name.Contains(searchTerm) ||
                            c.Email.Contains(searchTerm)))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}