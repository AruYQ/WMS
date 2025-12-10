// ViewModel untuk Item views

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk Create/Edit Item
    /// </summary>
    public class ItemViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Item Code wajib diisi")]
        [MaxLength(50, ErrorMessage = "Item Code maksimal 50 karakter")]
        [Display(Name = "Item Code")]
        public string ItemCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nama item wajib diisi")]
        [MaxLength(100, ErrorMessage = "Nama item maksimal 100 karakter")]
        [Display(Name = "Nama Item")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Deskripsi maksimal 500 karakter")]
        [Display(Name = "Deskripsi")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Unit wajib diisi")]
        [MaxLength(20, ErrorMessage = "Unit maksimal 20 karakter")]
        [Display(Name = "Unit")]
        public string Unit { get; set; } = string.Empty;

        [Required(ErrorMessage = "Harga beli wajib diisi")]
        [Range(0, double.MaxValue, ErrorMessage = "Harga beli harus lebih dari 0")]
        [Display(Name = "Harga Beli")]
        [DataType(DataType.Currency)]
        public decimal PurchasePrice { get; set; }

        [Required(ErrorMessage = "Harga jual wajib diisi")]
        [Range(0, double.MaxValue, ErrorMessage = "Harga jual harus lebih dari 0")]
        [Display(Name = "Harga Jual")]
        [DataType(DataType.Currency)]
        public decimal StandardPrice { get; set; }

        [Required(ErrorMessage = "Supplier wajib dipilih")]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; } = true;

        // Data untuk dropdown
        public SelectList? SupplierOptions { get; set; }

        // Display properties (computed)
        public string SupplierName { get; set; } = string.Empty;
        public string ItemDisplay { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        
        // Status styling
        public string StatusCssClass { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        
        // Stock information
        public int TotalStock { get; set; }
        public int TotalLocations { get; set; }
        public decimal TotalValue { get; set; }
        public string StockLevel { get; set; } = string.Empty;
        public bool NeedsReorder { get; set; }
        public bool IsAvailableForSale { get; set; }
    }

    /// <summary>
    /// ViewModel untuk Index Item
    /// </summary>
    public class ItemIndexViewModel
    {
        public List<ItemViewModel> Items { get; set; } = new List<ItemViewModel>();
        public string? SearchTerm { get; set; }
        public int? SupplierId { get; set; }
        public bool? IsActive { get; set; }
        public List<SelectListItem>? SupplierOptions { get; set; }

        // Statistics
        public ItemSummaryViewModel Summary { get; set; } = new ItemSummaryViewModel();
    }

    /// <summary>
    /// ViewModel untuk Details Item
    /// </summary>
    public class ItemDetailsViewModel
    {
        public ItemViewModel Item { get; set; } = new ItemViewModel();
        public int TotalQuantity { get; set; }
        public int TotalLocations { get; set; }
        public decimal TotalValue { get; set; }
        public List<InventoryViewModel> Inventories { get; set; } = new List<InventoryViewModel>();
        public List<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();
        public List<ASNDetail> ASNDetails { get; set; } = new List<ASNDetail>();
        public List<SalesOrderDetail> SalesOrderDetails { get; set; } = new List<SalesOrderDetail>();
    }

    /// <summary>
    /// ViewModel untuk Item summary statistics
    /// </summary>
    public class ItemSummaryViewModel
    {
        [Display(Name = "Total Items")]
        public int TotalItems { get; set; }
        
        [Display(Name = "Active Items")]
        public int ActiveItems { get; set; }
        
        [Display(Name = "Inactive Items")]
        public int InactiveItems { get; set; }
        
        [Display(Name = "Total Value")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal TotalValue { get; set; }
        
        [Display(Name = "Items with Stock")]
        public int ItemsWithStock { get; set; }
        
        [Display(Name = "Items Out of Stock")]
        public int ItemsOutOfStock { get; set; }
        
        [Display(Name = "Low Stock Items")]
        public int LowStockItems { get; set; }
        
        [Display(Name = "Average Price")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal AveragePrice { get; set; }
        
        // Status breakdown
        public Dictionary<string, int> StatusBreakdown { get; set; } = new Dictionary<string, int>();
    }
}