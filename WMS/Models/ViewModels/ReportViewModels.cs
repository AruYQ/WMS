using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Base request for report generation
    /// </summary>
    public class ReportRequest
    {
        [Required]
        [Display(Name = "From Date")]
        public DateTime FromDate { get; set; } = DateTime.Now.AddMonths(-1);

        [Required]
        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; } = DateTime.Now;

        [Display(Name = "Format")]
        public string Format { get; set; } = "HTML"; // HTML, Excel, PDF
    }

    /// <summary>
    /// Inbound report request dengan filtering lengkap
    /// </summary>
    public class InboundReportRequest : ReportRequest
    {
        // === DOCUMENT TYPE SELECTION ===
        public bool IncludePO { get; set; } = true;
        public bool IncludeASN { get; set; } = true;
        public bool IncludePutaway { get; set; } = false;
        
        // === SUPPLIER FILTERS ===
        public int? SupplierId { get; set; } // Single supplier (legacy support)
        public List<int>? SupplierIds { get; set; } // Multi-select suppliers
        
        // === STATUS FILTERS (Multi-select) ===
        public List<string>? POStatuses { get; set; } // Multiple PO statuses
        public List<string>? ASNStatuses { get; set; } // Multiple ASN statuses
        public bool IncludeCancelled { get; set; } = false; // Global cancelled filter
        
        // === DOCUMENT NUMBER SEARCH ===
        public string? PONumberFilter { get; set; } // Partial match PO Number
        public string? ASNNumberFilter { get; set; } // Partial match ASN Number
        
        // === SEPARATE DATE RANGES ===
        public DateTime? POFromDate { get; set; } // Custom date range for PO
        public DateTime? POToDate { get; set; }
        public DateTime? ASNFromDate { get; set; } // Custom date range for ASN
        public DateTime? ASNToDate { get; set; }
        public DateTime? PutawayFromDate { get; set; } // Custom date range for Putaway
        public DateTime? PutawayToDate { get; set; }
        
        // === AMOUNT/VALUE RANGE FILTERS ===
        public decimal? MinPOAmount { get; set; } // Minimum PO Total Amount
        public decimal? MaxPOAmount { get; set; } // Maximum PO Total Amount
        public decimal? MinASNAmount { get; set; } // Minimum ASN Total Amount
        public decimal? MaxASNAmount { get; set; } // Maximum ASN Total Amount
        
        // === LOCATION FILTERS (Untuk Putaway) ===
        public int? LocationId { get; set; } // Single location
        public List<int>? LocationIds { get; set; } // Multi-select locations
        public string? LocationCodeFilter { get; set; } // Search by Location Code (partial)
        public string? LocationCategoryFilter { get; set; } // Filter by Category (Storage/Holding)
        
        // === QUANTITY RANGE (Untuk Putaway) ===
        public int? MinPutawayQuantity { get; set; }
        public int? MaxPutawayQuantity { get; set; }
        
        // === ITEM COUNT FILTER ===
        public int? MinItemsCount { get; set; } // Minimum jumlah items di PO/ASN
        public int? MaxItemsCount { get; set; } // Maximum jumlah items di PO/ASN
        
        // === SORTING OPTIONS ===
        public string? SortBy { get; set; } = "Date"; // Date, Amount, Supplier, DocumentNumber, Status
        public string? SortOrder { get; set; } = "ASC"; // ASC, DESC
        
        // === GROUPING OPTIONS ===
        public bool GroupBySupplier { get; set; } = false;
        public bool GroupByStatus { get; set; } = false;
        public bool GroupByDate { get; set; } = false; // Group by day/week/month
        public string? GroupByDateType { get; set; } // Day, Week, Month (if GroupByDate = true)
    }

    /// <summary>
    /// Outbound report request untuk Sales Order & Picking
    /// </summary>
    public class OutboundReportRequest : ReportRequest
    {
        public int? CustomerId { get; set; }
        public List<int>? CustomerIds { get; set; }
        public List<string>? Statuses { get; set; }
        public bool IncludePickings { get; set; } = true;
    }

    /// <summary>
    /// Inbound report data lengkap (PO + ASN + Putaway)
    /// </summary>
    public class InboundReportData
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        
        // Summary Statistics
        public int TotalPurchaseOrders { get; set; }
        public int TotalASN { get; set; }
        public int TotalPutaway { get; set; } // Total putaway transactions
        public int TotalReceived { get; set; }
        public decimal TotalPOValue { get; set; }
        public decimal TotalASNValue { get; set; }
        
        // Filter Applied
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? POStatusFilter { get; set; }
        public string? ASNStatusFilter { get; set; }
        
        public List<InboundReportLine> Lines { get; set; } = new();

        public class InboundReportLine
        {
            public DateTime Date { get; set; }
            public string DocumentNumber { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty; // PO, ASN, or Putaway
            public string SupplierName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            
            // For PO
            public string? PONumber { get; set; }
            public int? TotalItems { get; set; } // Jumlah jenis item yang berbeda
            public int? TotalQuantity { get; set; } // Total quantity semua item
            public decimal? TotalAmount { get; set; }
            
            // For ASN
            public string? ASNNumber { get; set; }
            public string? PONumberForASN { get; set; }
            public int? TotalItemsASN { get; set; } // Jumlah jenis item yang berbeda
            public int? TotalQuantityASN { get; set; } // Total quantity semua item
            public decimal? TotalAmountASN { get; set; }
            
            // For Putaway
            public string? PutawayReference { get; set; } // ASN Number
            public string? ItemCode { get; set; }
            public string? ItemName { get; set; }
            public string? LocationCode { get; set; }
            public string? LocationName { get; set; }
            public int? PutawayQuantity { get; set; }
            public string? ASNNumberForPutaway { get; set; }
        }
    }

    /// <summary>
    /// Outbound report (SO & Picking)
    /// </summary>
    public class OutboundReportData
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalSalesOrders { get; set; }
        public int TotalPickings { get; set; }
        public int TotalShipped { get; set; }
        public decimal TotalValue { get; set; }
        public List<OutboundReportLine> Lines { get; set; } = new();

        public class OutboundReportLine
        {
            public DateTime Date { get; set; }
            public string DocumentNumber { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty; // SO or Picking
            public string CustomerName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int TotalItems { get; set; } // Jumlah jenis item yang berbeda
            public int TotalQuantity { get; set; } // Total quantity semua item
            public decimal TotalAmount { get; set; }
        }
    }

    /// <summary>
    /// Inventory movement report request
    /// </summary>
    public class InventoryMovementReportRequest : ReportRequest
    {
        public bool IncludePutaway { get; set; } = true;
        public bool IncludePicking { get; set; } = true;
        public int? ItemId { get; set; }
        public List<int>? ItemIds { get; set; }
        public string? ItemSearch { get; set; }
    }

    /// <summary>
    /// Inventory movement report
    /// </summary>
    public class InventoryMovementReportData
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalMovements { get; set; }
        public int TotalPutawayQuantity { get; set; }
        public int TotalPickingQuantity { get; set; }
        public int TotalItemsInvolved { get; set; }
        public int TotalPutawayTransactions { get; set; }
        public int TotalPickingTransactions { get; set; }
        public List<InventoryMovementLine> Lines { get; set; } = new();

        public class InventoryMovementLine
        {
            public DateTime Date { get; set; }
            public string ItemCode { get; set; } = string.Empty;
            public string ItemName { get; set; } = string.Empty;
            public string MovementType { get; set; } = string.Empty; // IN, OUT, ADJUST
            public int Quantity { get; set; }
            public string Reference { get; set; } = string.Empty;
            public string? DocumentNumber { get; set; }
            public string? CustomerName { get; set; }
            public string? SupplierName { get; set; }
        }
    }

    /// <summary>
    /// Stock report request untuk agregasi inventory
    /// </summary>
    public class StockReportRequest : ReportRequest
    {
        public int? SupplierId { get; set; }
        public string? Category { get; set; }
        public string? ItemSearch { get; set; }
        public bool IncludeZeroStock { get; set; } = false;
    }

    /// <summary>
    /// Stock report aggregated per item (regardless of location)
    /// </summary>
    public class StockReportData
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public int TotalDistinctItems { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public List<StockReportLine> Lines { get; set; } = new();

        public class StockReportLine
        {
            public int ItemId { get; set; }
            public string ItemCode { get; set; } = string.Empty;
            public string ItemName { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public int TotalQuantity { get; set; }
            public decimal AverageCost { get; set; }
            public decimal TotalValue { get; set; }
            public int LocationCount { get; set; }
            public List<string> Locations { get; set; } = new();
        }
    }

    /// <summary>
    /// Supplier report request
    /// </summary>
    public class SupplierReportRequest : ReportRequest
    {
        public bool? IsActive { get; set; } // null = all, true = active only, false = inactive only
        public string? Search { get; set; } // Search by name, email, or code
        public string? SortBy { get; set; } = "Name"; // Name, TotalPO, TotalItems, TotalValue
        public string? SortOrder { get; set; } = "ASC"; // ASC, DESC
    }

    /// <summary>
    /// Supplier report data
    /// </summary>
    public class SupplierReportData
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public int TotalSuppliers { get; set; }
        public int ActiveSuppliers { get; set; }
        public int InactiveSuppliers { get; set; }
        public int TotalPurchaseOrders { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalPOValue { get; set; }
        public List<SupplierReportLine> Lines { get; set; } = new();

        public class SupplierReportLine
        {
            public int SupplierId { get; set; }
            public string SupplierName { get; set; } = string.Empty;
            public string? Code { get; set; }
            public string Email { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
            public string? ContactPerson { get; set; }
            public bool IsActive { get; set; }
            public int TotalPurchaseOrders { get; set; }
            public int TotalItems { get; set; }
            public decimal TotalPOValue { get; set; }
            public DateTime? LastPODate { get; set; }
        }
    }

    /// <summary>
    /// Customer report request
    /// </summary>
    public class CustomerReportRequest : ReportRequest
    {
        public bool? IsActive { get; set; } // null = all, true = active only, false = inactive only
        public string? Search { get; set; } // Search by name, email, or code
        public string? CustomerType { get; set; } // Filter by customer type
        public string? SortBy { get; set; } = "Name"; // Name, TotalSO, TotalValue
        public string? SortOrder { get; set; } = "ASC"; // ASC, DESC
    }

    /// <summary>
    /// Customer report data
    /// </summary>
    public class CustomerReportData
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }
        public int TotalSalesOrders { get; set; }
        public decimal TotalSOValue { get; set; }
        public List<CustomerReportLine> Lines { get; set; } = new();

        public class CustomerReportLine
        {
            public int CustomerId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string? Code { get; set; }
            public string Email { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
            public string? CustomerType { get; set; }
            public bool IsActive { get; set; }
            public int TotalSalesOrders { get; set; }
            public decimal TotalSOValue { get; set; }
            public DateTime? LastSODate { get; set; }
        }
    }

    /// <summary>
    /// Report template info (for saved reports)
    /// </summary>
    public class ReportTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Export request for reports dengan filtering
    /// </summary>
    public class ReportExportRequest
    {
        [Required]
        public string ReportType { get; set; } = string.Empty; // Inbound, Outbound, Inventory, Stock, Supplier, Customer

        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        [Required]
        public string Format { get; set; } = "PDF"; // Excel, PDF

        // Filtering options for Inbound Report (mirror InboundReportRequest)
        public bool IncludePO { get; set; } = true;
        public bool IncludeASN { get; set; } = true;
        public bool IncludePutaway { get; set; } = true;
        public int? SupplierId { get; set; }
        public List<int>? SupplierIds { get; set; }
        public List<string>? POStatuses { get; set; }
        public List<string>? ASNStatuses { get; set; }
        public bool IncludeCancelled { get; set; } = false;
        public string? PONumberFilter { get; set; }
        public string? ASNNumberFilter { get; set; }
        public DateTime? POFromDate { get; set; }
        public DateTime? POToDate { get; set; }
        public DateTime? ASNFromDate { get; set; }
        public DateTime? ASNToDate { get; set; }
        public DateTime? PutawayFromDate { get; set; }
        public DateTime? PutawayToDate { get; set; }
        public decimal? MinPOAmount { get; set; }
        public decimal? MaxPOAmount { get; set; }
        public decimal? MinASNAmount { get; set; }
        public decimal? MaxASNAmount { get; set; }
        public int? LocationId { get; set; }
        public List<int>? LocationIds { get; set; }
        public string? LocationCodeFilter { get; set; }
        public string? LocationCategoryFilter { get; set; }
        public int? MinPutawayQuantity { get; set; }
        public int? MaxPutawayQuantity { get; set; }
        public int? MinItemsCount { get; set; }
        public int? MaxItemsCount { get; set; }
        public string? SortBy { get; set; } = "Date";
        public string? SortOrder { get; set; } = "ASC";

        // Filtering options for Outbound Report
        public int? CustomerId { get; set; }
        public List<int>? CustomerIds { get; set; }
        public List<string>? Statuses { get; set; }
        public bool IncludePickings { get; set; } = true;

        // Filtering options for Inventory Movement Report
        public bool IncludePutawayMovements { get; set; } = true;
        public bool IncludePickingMovements { get; set; } = true;
        public int? ItemId { get; set; }
        public List<int>? ItemIds { get; set; }
        public string? ItemSearch { get; set; }

        // Filtering options for Stock Report
        public string? Category { get; set; }
        public bool IncludeZeroStock { get; set; } = false;

        // Filtering options for Supplier Report
        public bool? SupplierIsActive { get; set; }
        public string? SupplierSearch { get; set; }
        public string? SupplierSortBy { get; set; } = "Name";
        public string? SupplierSortOrder { get; set; } = "ASC";

        // Filtering options for Customer Report
        public bool? CustomerIsActive { get; set; }
        public string? CustomerSearch { get; set; }
        public string? CustomerType { get; set; }
        public string? CustomerSortBy { get; set; } = "Name";
        public string? CustomerSortOrder { get; set; } = "ASC";
    }
}

