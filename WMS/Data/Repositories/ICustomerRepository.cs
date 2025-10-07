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
        
        // Advanced Search methods
        Task<IEnumerable<Customer>> SearchAsync(CustomerSearchRequest request);
        Task<IEnumerable<Customer>> QuickSearchAsync(string query);
    }
    
    // Search request model for Customer
    public class CustomerSearchRequest
    {
        public string? SearchText { get; set; }
        public string? StatusFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? CityFilter { get; set; }
        public string? CustomerTypeFilter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}