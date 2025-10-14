// Request model untuk advanced search supplier

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Request model untuk advanced search supplier
    /// Digunakan di ItemController untuk mencari supplier dengan multiple criteria
    /// </summary>
    public class SupplierAdvancedSearchRequest
    {
        /// <summary>
        /// Nama supplier (partial match)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Email supplier (partial match)
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Phone supplier (partial match)
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// City supplier (partial match)
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Contact person supplier (partial match)
        /// </summary>
        public string? ContactPerson { get; set; }

        /// <summary>
        /// Tanggal supplier dibuat - dari tanggal
        /// </summary>
        public DateTime? CreatedDateFrom { get; set; }

        /// <summary>
        /// Tanggal supplier dibuat - sampai tanggal
        /// </summary>
        public DateTime? CreatedDateTo { get; set; }

        /// <summary>
        /// Page number untuk pagination (default: 1)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size untuk pagination (default: 20)
        /// </summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Response model untuk advanced search supplier
    /// </summary>
    public class SupplierAdvancedSearchResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<SupplierSearchResult> Data { get; set; } = new List<SupplierSearchResult>();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }

    /// <summary>
    /// Model untuk hasil search supplier
    /// </summary>
    public class SupplierSearchResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? City { get; set; }
        public string? ContactPerson { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
