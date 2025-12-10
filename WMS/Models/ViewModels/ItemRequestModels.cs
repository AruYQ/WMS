// Request models untuk Item API endpoints

using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Request model untuk create item
    /// </summary>
    public class ItemCreateRequest
    {
        [Required(ErrorMessage = "Item code is required")]
        [MaxLength(50, ErrorMessage = "Item code cannot exceed 50 characters")]
        public string ItemCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Item name is required")]
        [MaxLength(100, ErrorMessage = "Item name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Unit is required")]
        [MaxLength(20, ErrorMessage = "Unit cannot exceed 20 characters")]
        public string Unit { get; set; } = string.Empty;

        [Required(ErrorMessage = "Purchase price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Purchase price must be greater than or equal to 0")]
        public decimal PurchasePrice { get; set; }

        [Required(ErrorMessage = "Standard price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Standard price must be greater than or equal to 0")]
        public decimal StandardPrice { get; set; }

        [Required(ErrorMessage = "Supplier is required")]
        public int SupplierId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Request model untuk update item
    /// </summary>
    public class ItemUpdateRequest
    {
        [Required(ErrorMessage = "Item code is required")]
        [MaxLength(50, ErrorMessage = "Item code cannot exceed 50 characters")]
        public string ItemCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Item name is required")]
        [MaxLength(100, ErrorMessage = "Item name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Unit is required")]
        [MaxLength(20, ErrorMessage = "Unit cannot exceed 20 characters")]
        public string Unit { get; set; } = string.Empty;

        [Required(ErrorMessage = "Purchase price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Purchase price must be greater than or equal to 0")]
        public decimal PurchasePrice { get; set; }

        [Required(ErrorMessage = "Standard price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Standard price must be greater than or equal to 0")]
        public decimal StandardPrice { get; set; }

        [Required(ErrorMessage = "Supplier is required")]
        public int SupplierId { get; set; }

        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Response model untuk item list
    /// </summary>
    public class ItemListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ItemResponse> Data { get; set; } = new List<ItemResponse>();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }

    /// <summary>
    /// Response model untuk single item
    /// </summary>
    public class ItemResponse
    {
        public int Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public decimal StandardPrice { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public int TotalStock { get; set; }
        public decimal TotalValue { get; set; }
        public decimal ProfitMargin { get; set; }
        public decimal ProfitMarginPercentage { get; set; }
    }

    /// <summary>
    /// Request model untuk item list dengan pagination dan search
    /// </summary>
    public class ItemListRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public int? SupplierId { get; set; }
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; } = "ItemCode";
        public string? SortDirection { get; set; } = "asc";
    }
}
