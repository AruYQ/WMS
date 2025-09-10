using WMS.Models;
using WMS.Models.ViewModels;

namespace WMS.Services
{
    public interface ICustomerService
    {
        // Basic CRUD operations
        Task<Customer?> GetByIdAsync(int id);
        Task<IEnumerable<Customer>> GetAllAsync();
        Task<Customer> CreateAsync(Customer customer);
        Task<Customer> UpdateAsync(Customer customer);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);

        // Search and filter operations
        Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm);
        Task<IEnumerable<Customer>> GetActiveCustomersAsync();
        Task<IEnumerable<Customer>> GetCustomersWithSalesOrdersAsync();

        // ViewModel operations
        Task<CustomerViewModel> GetCustomerViewModelAsync(int id);
        Task<CustomerViewModel> PopulateCustomerViewModelAsync(CustomerViewModel viewModel);
        Task<CustomerIndexViewModel> GetCustomerIndexViewModelAsync(string? searchTerm = null, bool? isActive = null);
        Task<CustomerDetailsViewModel> GetCustomerDetailsViewModelAsync(int id);
        Task<CustomerSummaryViewModel> GetCustomerSummaryAsync();

        // Validation operations
        Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
        Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null);

        // AJAX operations
        Task<bool> CheckEmailExistsAsync(string email, int? excludeId = null);
        Task<IEnumerable<object>> SearchCustomersForAjaxAsync(string searchTerm);
        Task<object> GetCustomerDetailsForAjaxAsync(int id);
        Task<IEnumerable<CustomerPerformanceViewModel>> GetPerformanceDataAsync();
        Task<CustomerPerformanceReportViewModel> GetPerformanceReportViewModelAsync();
        Task<IEnumerable<object>> GetCustomersForExportAsync();

        // Statistics operations
        Task<Dictionary<string, object>> GetCustomerStatisticsAsync();
    }
}
