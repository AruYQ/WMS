using WMS.Models;

namespace WMS.Data.Repositories
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<IEnumerable<Customer>> GetAllWithSalesOrdersAsync();
        Task<Customer?> GetByIdWithSalesOrdersAsync(int id);
        Task<IEnumerable<Customer>> GetActiveCustomersAsync();
        Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
        Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null);
        Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm);
        Task<IEnumerable<Customer>> GetCustomersWithSalesOrdersAsync();
    }
}