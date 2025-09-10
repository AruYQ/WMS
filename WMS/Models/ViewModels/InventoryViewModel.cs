using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk Inventory dengan computed properties
    /// </summary>
    public class InventoryViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Item wajib dipilih")]
        [Display(Name = "Item")]
        public int ItemId { get; set; }
        
        [Required(ErrorMessage = "Lokasi wajib dipilih")]
        [Display(Name = "Lokasi")]
        public int LocationId { get; set; }
        
        [Required(ErrorMessage = "Quantity wajib diisi")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity tidak boleh negatif")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }
        
        [Display(Name = "Harga Terakhir")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal LastCostPrice { get; set; }
        
        [Required(ErrorMessage = "Status wajib dipilih")]
        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;
        
        [MaxLength(500)]
        [Display(Name = "Catatan")]
        public string? Notes { get; set; }
        
        [MaxLength(100)]
        [Display(Name = "Referensi Sumber")]
        public string? SourceReference { get; set; }
        
        [Display(Name = "Terakhir Diupdate")]
        public DateTime LastUpdated { get; set; }
        
        // Data untuk dropdown
        public SelectList? Items { get; set; }
        public SelectList? Locations { get; set; }
        
        // Enhanced location dropdown items with capacity info
        public List<LocationDropdownItem> LocationDropdownItems { get; set; } = new List<LocationDropdownItem>();
        
        // Display properties (computed)
        public string ItemDisplay { get; set; } = string.Empty;
        public string LocationDisplay { get; set; } = string.Empty;
        public string ItemUnit { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public string Summary { get; set; } = string.Empty;
        
        // Status styling
        public string StatusCssClass { get; set; } = string.Empty;
        public string StatusIndonesia { get; set; } = string.Empty;
        public string QuantityCssClass { get; set; } = string.Empty;
        
        // Stock level indicators
        public string StockLevel { get; set; } = string.Empty;
        public bool IsAvailableForSale { get; set; }
        public bool NeedsReorder { get; set; }
    }
    
    /// <summary>
    /// ViewModel untuk putaway operations
    /// </summary>
    public class PutawayViewModel
    {
        public int ASNId { get; set; }
        
        [Display(Name = "Nomor ASN")]
        public string ASNNumber { get; set; } = string.Empty;
        
        [Display(Name = "Nomor PO")]
        public string PONumber { get; set; } = string.Empty;
        
        [Display(Name = "Supplier")]
        public string SupplierName { get; set; } = string.Empty;
        
        [Display(Name = "Tanggal Pengiriman")]
        public DateTime ShipmentDate { get; set; }
        
        [Display(Name = "Tanggal Diproses")]
        public DateTime? ProcessedDate { get; set; }
        
        // Available locations untuk putaway
        public SelectList AvailableLocations { get; set; } = new SelectList(new List<object>(), "Id", "DisplayName");
        
        // Enhanced location dropdown items with capacity info
        public List<LocationDropdownItem> LocationDropdownItems { get; set; } = new List<LocationDropdownItem>();
        
        // List of putaway details
        public List<PutawayDetailViewModel> PutawayDetails { get; set; } = new List<PutawayDetailViewModel>();
    }
    
    /// <summary>
    /// ViewModel untuk detail putaway per ASN Detail
    /// </summary>
    public class PutawayDetailViewModel
    {
        public int ASNId { get; set; }
        public int ASNDetailId { get; set; }
        public int ItemId { get; set; }
        
        [Display(Name = "Kode Item")]
        public string ItemCode { get; set; } = string.Empty;
        
        [Display(Name = "Nama Item")]
        public string ItemName { get; set; } = string.Empty;
        
        [Display(Name = "Unit")]
        public string ItemUnit { get; set; } = string.Empty;
        
        [Display(Name = "Total Quantity")]
        public int TotalQuantity { get; set; }
        
        [Display(Name = "Remaining Quantity")]
        public int RemainingQuantity { get; set; }
        
        [Display(Name = "Actual Price per Item")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal ActualPricePerItem { get; set; }
        
        [Display(Name = "Warehouse Fee Rate")]
        [DisplayFormat(DataFormatString = "{0:P2}")]
        public decimal WarehouseFeeRate { get; set; }
        
        [Display(Name = "Warehouse Fee Amount")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal WarehouseFeeAmount { get; set; }
        
        // For putaway form
        [Range(1, int.MaxValue, ErrorMessage = "Quantity putaway harus lebih dari 0")]
        [Display(Name = "Quantity to Putaway")]
        public int QuantityToPutaway { get; set; }
        
        [Required(ErrorMessage = "Lokasi wajib dipilih")]
        [Display(Name = "Target Lokasi")]
        public int LocationId { get; set; }
        
        [MaxLength(200)]
        [Display(Name = "Catatan")]
        public string? Notes { get; set; }
        
        // Suggested location
        public int? SuggestedLocationId { get; set; }
        
        // Display properties
        public string ItemDisplay { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool CanPutaway { get; set; }
    }
    
    /// <summary>
    /// ViewModel untuk inventory index dengan filtering
    /// </summary>
    public class InventoryIndexViewModel
    {
        [Display(Name = "Search")]
        public string? SearchTerm { get; set; }
        
        [Display(Name = "Status")]
        public string? StatusFilter { get; set; }
        
        [Display(Name = "Show Low Stock Only")]
        public bool ShowLowStockOnly { get; set; }
        
        [Display(Name = "Show Empty Locations")]
        public bool ShowEmptyLocations { get; set; }
        
        public IEnumerable<InventoryViewModel> Inventories { get; set; } = new List<InventoryViewModel>();
        public string[] AvailableStatuses { get; set; } = Array.Empty<string>();
        public InventorySummaryViewModel Summary { get; set; } = new InventorySummaryViewModel();
    }
    
    /// <summary>
    /// ViewModel untuk location status dengan inventory
    /// </summary>
    public class LocationStatusViewModel
    {
        public int LocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int MaxCapacity { get; set; }
        public int CurrentCapacity { get; set; }
        public int AvailableCapacity { get; set; }
        public double CapacityPercentage { get; set; }
        public bool IsFull { get; set; }
        public int ItemCount { get; set; }
        public string StatusCssClass { get; set; } = string.Empty;
        public List<InventoryViewModel> Items { get; set; } = new List<InventoryViewModel>();
    }
    
    /// <summary>
    /// ViewModel untuk location dropdown dengan capacity info
    /// </summary>
    public class LocationDropdownItem
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int MaxCapacity { get; set; }
        public int CurrentCapacity { get; set; }
        public int AvailableCapacity { get; set; }
        public string DisplayText { get; set; } = string.Empty;
        public string CssClass { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public bool CanAccommodate { get; set; }
        public bool IsFull { get; set; }
        public double CapacityPercentage { get; set; }
    }
    
    /// <summary>
    /// ViewModel untuk putaway index - list ASN yang ready untuk putaway
    /// </summary>
    public class PutawayIndexViewModel
    {
        [Display(Name = "Filter Status ASN")]
        public string? StatusFilter { get; set; }
        
        [Display(Name = "Filter Supplier")]
        public string? SupplierFilter { get; set; }
        
        [Display(Name = "Show Today Only")]
        public bool ShowTodayOnly { get; set; }
        
        public IEnumerable<ASNForPutawayViewModel> ProcessedASNs { get; set; } = new List<ASNForPutawayViewModel>();
        public PutawaySummaryViewModel Summary { get; set; } = new PutawaySummaryViewModel();
    }
    
    /// <summary>
    /// ViewModel untuk ASN yang ready untuk putaway
    /// </summary>
    public class ASNForPutawayViewModel
    {
        public int ASNId { get; set; }
        
        [Display(Name = "Nomor ASN")]
        public string ASNNumber { get; set; } = string.Empty;
        
        [Display(Name = "Nomor PO")]
        public string PONumber { get; set; } = string.Empty;
        
        [Display(Name = "Supplier")]
        public string SupplierName { get; set; } = string.Empty;
        
        [Display(Name = "Tanggal Sampai")]
        public DateTime? ActualArrivalDate { get; set; }
        
        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;
        
        [Display(Name = "Total Items")]
        public int TotalItemTypes { get; set; }
        
        [Display(Name = "Total Quantity")]
        public int TotalQuantity { get; set; }
        
        [Display(Name = "Items Pending Putaway")]
        public int PendingPutawayCount { get; set; }
        
        [Display(Name = "Completion Progress")]
        [DisplayFormat(DataFormatString = "{0:F1}%")]
        public double CompletionPercentage { get; set; }
        
        // Display properties
        public string StatusIndonesia { get; set; } = string.Empty;
        public string StatusCssClass { get; set; } = string.Empty;
        public string CompletionCssClass { get; set; } = string.Empty;
        public bool CanStartPutaway { get; set; }
        public bool IsCompleted { get; set; }
        
        // Quick actions
        public bool CanProcessAll { get; set; }
        public int ReadyForPutawayCount { get; set; }
    }
    
    /// <summary>
    /// ViewModel untuk putaway summary
    /// </summary>
    public class PutawaySummaryViewModel
    {
        [Display(Name = "Total ASN Processed")]
        public int TotalProcessedASNs { get; set; }
        
        [Display(Name = "Total Items Pending")]
        public int TotalPendingItems { get; set; }
        
        [Display(Name = "Total Quantity Pending")]
        public int TotalPendingQuantity { get; set; }
        
        [Display(Name = "Today's Putaway")]
        public int TodayPutawayCount { get; set; }
        
        [Display(Name = "Oldest Pending ASN")]
        public string? OldestPendingASN { get; set; }
        
        [Display(Name = "Days Since Oldest")]
        public int? DaysSinceOldest { get; set; }
        
        // Status breakdown
        public Dictionary<string, int> StatusBreakdown { get; set; } = new Dictionary<string, int>();
    }
    
    /// <summary>
    /// ViewModel untuk putaway result/response
    /// </summary>
    public class PutawayResultViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedItems { get; set; }
        public int TotalQuantity { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// ViewModel untuk inventory summary statistics
    /// </summary>
    public class InventorySummaryViewModel
    {
        [Display(Name = "Total Items")]
        public int TotalItems { get; set; }
        
        [Display(Name = "Total Value")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal TotalValue { get; set; }
        
        [Display(Name = "Available Stock")]
        public int AvailableStock { get; set; }
        
        [Display(Name = "Reserved Stock")]
        public int ReservedStock { get; set; }
        
        [Display(Name = "Damaged Stock")]
        public int DamagedStock { get; set; }
        
        [Display(Name = "Low Stock Items")]
        public int LowStockCount { get; set; }
        
        [Display(Name = "Empty Locations")]
        public int EmptyLocationCount { get; set; }
        
        // Status breakdown
        public Dictionary<string, int> StatusBreakdown { get; set; } = new Dictionary<string, int>();
    }
}