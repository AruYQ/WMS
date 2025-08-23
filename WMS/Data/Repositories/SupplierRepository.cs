using Microsoft.EntityFrameworkCore;
using WMS.Models;
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
            return await GetBaseQuery().AnyAsync(s => s.Email == email);
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
    }
}