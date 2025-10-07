using System.ComponentModel.DataAnnotations;

namespace WMS.Data.Repositories
{
    public class ASNSearchRequest
    {
        public string? SearchText { get; set; }
        public string? ASNNumberFilter { get; set; }
        public string? SupplierNameFilter { get; set; }
        public string? StatusFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;

        [Range(1, 1000, ErrorMessage = "PageSize must be between 1 and 1000")]
        public int PageSize { get; set; } = 10;

        public bool IsValid()
        {
            if (DateFrom.HasValue && DateTo.HasValue && DateFrom.Value > DateTo.Value)
            {
                return false;
            }
            return true;
        }

        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (DateFrom.HasValue && DateTo.HasValue && DateFrom.Value > DateTo.Value)
            {
                errors.Add("Date From cannot be after Date To.");
            }
            return errors;
        }
    }
}
