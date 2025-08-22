using WMS.Models;

namespace WMS.Data.Repositories
{
    public interface ISupplierRepository : IRepository<Supplier>
    {
        Task<IEnumerable<Supplier>> GetAllWithPurchaseOrdersAsync();
        Task<Supplier?> GetByIdWithPurchaseOrdersAsync(int id);
        Task<IEnumerable<Supplier>> GetActiveSuppliers();
        Task<bool> ExistsByEmailAsync(string email);
        Task<IEnumerable<Supplier>> SearchSuppliersAsync(string searchTerm);
    }
}