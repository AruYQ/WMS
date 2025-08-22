using WMS.Models;

namespace WMS.Data.Repositories
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<IEnumerable<Customer>> GetAllWithSalesOrdersAsync();
        Task<Customer?> GetByIdWithSalesOrdersAsync(int id);
        Task<IEnumerable<Customer>> GetActiveCustomersAsync();
        Task<bool> ExistsByEmailAsync(string email);
        Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm);
    }
}