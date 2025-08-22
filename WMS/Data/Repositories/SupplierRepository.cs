using Microsoft.EntityFrameworkCore;
using WMS.Models;

namespace WMS.Data.Repositories
{
    public class SupplierRepository : Repository<Supplier>, ISupplierRepository
    {
        public SupplierRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Supplier>> GetAllWithPurchaseOrdersAsync()
        {
            return await _dbSet
                .Include(s => s.PurchaseOrders)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Supplier?> GetByIdWithPurchaseOrdersAsync(int id)
        {
            return await _dbSet
                .Include(s => s.PurchaseOrders)
                    .ThenInclude(po => po.PurchaseOrderDetails)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Supplier>> GetActiveSuppliers()
        {
            return await _dbSet
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _dbSet.AnyAsync(s => s.Email == email);
        }

        public async Task<IEnumerable<Supplier>> SearchSuppliersAsync(string searchTerm)
        {
            return await _dbSet
                .Where(s => s.IsActive &&
                           (s.Name.Contains(searchTerm) ||
                            s.Email.Contains(searchTerm)))
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}