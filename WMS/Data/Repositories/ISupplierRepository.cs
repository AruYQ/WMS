using WMS.Models;
using System.ComponentModel.DataAnnotations;

namespace WMS.Data.Repositories
{
    public interface ISupplierRepository : IRepository<Supplier>
    {
        Task<IEnumerable<Supplier>> GetAllWithPurchaseOrdersAsync();
        Task<Supplier?> GetByIdWithPurchaseOrdersAsync(int id);
        Task<IEnumerable<Supplier>> GetActiveSuppliers();
        Task<bool> ExistsByEmailAsync(string email);
        Task<IEnumerable<Supplier>> SearchSuppliersAsync(string searchTerm);
        
        // Advanced Search methods
        Task<IEnumerable<Supplier>> SearchAsync(WMS.Models.ViewModels.SupplierSearchRequest request);
        Task<IEnumerable<Supplier>> QuickSearchAsync(string query);
    }
    
    // Search request model for Supplier
    public class SupplierSearchRequest
    {
        public string? SearchText { get; set; }
        public string? StatusFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? CityFilter { get; set; }
        public string? ContactPersonFilter { get; set; }
        public string? SupplierNameFilter { get; set; }
        public string? PhoneFilter { get; set; }
        public string? SupplierCodeFilter { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;
        
        [Range(1, 1000, ErrorMessage = "PageSize must be between 1 and 1000")]
        public int PageSize { get; set; } = 50;

        // Validation method
        public bool IsValid()
        {
            // Validate date range
            if (DateFrom.HasValue && DateTo.HasValue && DateFrom > DateTo)
            {
                return false;
            }

            // Validate page and page size
            if (Page < 1 || PageSize < 1 || PageSize > 1000)
            {
                return false;
            }

            return true;
        }

        // Get validation errors
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (DateFrom.HasValue && DateTo.HasValue && DateFrom > DateTo)
            {
                errors.Add("DateFrom cannot be greater than DateTo");
            }

            if (Page < 1)
            {
                errors.Add("Page must be greater than 0");
            }

            if (PageSize < 1 || PageSize > 1000)
            {
                errors.Add("PageSize must be between 1 and 1000");
            }

            return errors;
        }
    }
}