// Request model untuk advanced search item di Purchase Order

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Request model untuk advanced search item di Purchase Order
    /// </summary>
    public class ItemAdvancedSearchRequest
    {
        /// <summary>
        /// Kode item (partial match)
        /// </summary>
        public string? ItemCode { get; set; }

        /// <summary>
        /// Nama item (partial match)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Unit item (partial match)
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// Tanggal item dibuat - dari tanggal
        /// </summary>
        public DateTime? CreatedDateFrom { get; set; }

        /// <summary>
        /// Tanggal item dibuat - sampai tanggal
        /// </summary>
        public DateTime? CreatedDateTo { get; set; }

        /// <summary>
        /// Supplier ID untuk filter (wajib)
        /// </summary>
        public int SupplierId { get; set; }

        /// <summary>
        /// Purchase Order ID untuk context (optional)
        /// </summary>
        public int? PurchaseOrderId { get; set; }

        /// <summary>
        /// Page number untuk pagination (default: 1)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size untuk pagination (default: 10)
        /// </summary>
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// Response model untuk advanced search item
    /// </summary>
    public class ItemAdvancedSearchResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ItemSearchResult> Data { get; set; } = new List<ItemSearchResult>();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }

    /// <summary>
    /// Model untuk hasil search item
    /// </summary>
    public class ItemSearchResult
    {
        public int Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal StandardPrice { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
