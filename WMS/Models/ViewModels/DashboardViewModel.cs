// Models/ViewModels/DashboardViewModel.cs
// ViewModel untuk dashboard utama WMS

using System.ComponentModel.DataAnnotations;
using WMS.Utilities;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk dashboard utama
    /// Menampilkan ringkasan dan metrics penting WMS
    /// </summary>
    public class DashboardViewModel
    {
        // Key Performance Indicators (KPI)
        public DashboardKPI KPI { get; set; } = new DashboardKPI();

        // Recent Activities
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();

        // Alerts dan Notifications
        public List<SystemAlert> SystemAlerts { get; set; } = new List<SystemAlert>();

        // Charts Data
        public InventoryChartData InventoryCharts { get; set; } = new InventoryChartData();
        public OrderChartData OrderCharts { get; set; } = new OrderChartData();
        public WarehouseChartData WarehouseCharts { get; set; } = new WarehouseChartData();

        // Quick Stats
        public QuickStats QuickStats { get; set; } = new QuickStats();

        // Top Lists
        public List<TopItem> TopItems { get; set; } = new List<TopItem>();
        public List<TopSupplier> TopSuppliers { get; set; } = new List<TopSupplier>();
        public List<TopCustomer> TopCustomers { get; set; } = new List<TopCustomer>();

        // Pending Actions (require attention)
        public PendingActions PendingActions { get; set; } = new PendingActions();

        // Date Range untuk filter
        [Display(Name = "Maximum Recent Activities")]
        [Range(5, 50, ErrorMessage = "Maksimal recent activities harus antara 5-50")]
        public int MaxRecentActivities { get; set; } = 10;

        [Display(Name = "Show System Alerts")]
        public bool ShowSystemAlerts { get; set; } = true;

        [Display(Name = "Show KPI Charts")]
        public bool ShowKPICharts { get; set; } = true;

        [Display(Name = "Show Inventory Charts")]
        public bool ShowInventoryCharts { get; set; } = true;

        [Display(Name = "Show Order Charts")]
        public bool ShowOrderCharts { get; set; } = true;

        [Display(Name = "Show Top Lists")]
        public bool ShowTopLists { get; set; } = true;

        [Display(Name = "Default Date Range (days)")]
        [Range(1, 365, ErrorMessage = "Default date range harus antara 1-365 hari")]
        public int DefaultDateRangeDays { get; set; } = 30;

        // Widget Visibility Settings
        [Display(Name = "Show Purchase Order Widget")]
        public bool ShowPurchaseOrderWidget { get; set; } = true;

        [Display(Name = "Show Sales Order Widget")]
        public bool ShowSalesOrderWidget { get; set; } = true;

        [Display(Name = "Show Inventory Widget")]
        public bool ShowInventoryWidget { get; set; } = true;

        [Display(Name = "Show Location Widget")]
        public bool ShowLocationWidget { get; set; } = true;

        [Display(Name = "Show Pending Actions Widget")]
        public bool ShowPendingActionsWidget { get; set; } = true;

        // Date Range Properties
        [Display(Name = "Dari Tanggal")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "Sampai Tanggal")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        // Last Refresh Time
        public DateTime LastRefresh { get; set; } = DateTime.Now;

        // Helper Properties
        public bool HasCriticalAlerts => SystemAlerts.Any(a => a.Level == NotificationLevel.Critical);
        public bool HasWarnings => SystemAlerts.Any(a => a.Level == NotificationLevel.Warning);
        public int AlertCount => SystemAlerts.Count;
        public string LastRefreshDisplay => LastRefresh.ToString(Constants.DATETIME_FORMAT);
    }

    /// <summary>
    /// ViewModel untuk export dashboard data
    /// </summary>
    public class DashboardExportViewModel
    {
        [Display(Name = "Export Type")]
        public string ExportType { get; set; } = "PDF"; // PDF, Excel, CSV

        [Display(Name = "Include KPI Summary")]
        public bool IncludeKPI { get; set; } = true;

        [Display(Name = "Include Charts")]
        public bool IncludeCharts { get; set; } = true;

        [Display(Name = "Include Recent Activities")]
        public bool IncludeRecentActivities { get; set; } = false;

        [Display(Name = "Include Top Lists")]
        public bool IncludeTopLists { get; set; } = true;

        [Display(Name = "Date Range")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; } = DateTime.Today.AddDays(-30);

        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; } = DateTime.Today;

        public List<string> ExportTypes { get; set; } = new List<string> { "PDF", "Excel", "CSV" };

        public string ExportFileName
        {
            get
            {
                var extension = ExportType.ToLower() switch
                {
                    "excel" => "xlsx",
                    "csv" => "csv",
                    _ => "pdf"
                };
                return $"Dashboard_Report_{DateTime.Now:yyyyMMdd}.{extension}";
            }
        }
    }

    /// <summary>
    /// Key Performance Indicators untuk dashboard
    /// </summary>
    public class DashboardKPI
    {
        // Order Metrics
        public int TotalPurchaseOrders { get; set; }
        public int PendingPurchaseOrders { get; set; }
        public decimal TotalPurchaseValue { get; set; }

        // Sales Order - DISABLED
        // public int TotalSalesOrders { get; set; }
        // public int PendingSalesOrders { get; set; }
        // public decimal TotalSalesValue { get; set; }

        public int TotalASNs { get; set; }
        public int PendingASNs { get; set; }

        // Inventory Metrics
        public int TotalItems { get; set; }
        public int ActiveItems { get; set; }
        public int TotalInventoryRecords { get; set; }
        public decimal TotalInventoryValue { get; set; }

        // Location Metrics
        public int TotalLocations { get; set; }
        public int ActiveLocations { get; set; }
        public double AverageCapacityUtilization { get; set; }
        public int FullLocations { get; set; }

        // Stock Level Metrics
        public int ItemsInStock { get; set; }
        public int LowStockItems { get; set; }
        public int CriticalStockItems { get; set; }
        public int OutOfStockItems { get; set; }

        // Growth Metrics (compared to previous period)
        public double OrderGrowthPercentage { get; set; }
        public double InventoryValueGrowth { get; set; }

        // Computed Properties
        public string TotalPurchaseValueDisplay => TotalPurchaseValue.ToString(Constants.CURRENCY_FORMAT);
        //public string TotalSalesValueDisplay => TotalSalesValue.ToString(Constants.CURRENCY_FORMAT);
        public string TotalInventoryValueDisplay => TotalInventoryValue.ToString(Constants.CURRENCY_FORMAT);

        public double PurchaseOrderCompletionRate
        {
            get
            {
                if (TotalPurchaseOrders == 0) return 0;
                return (double)(TotalPurchaseOrders - PendingPurchaseOrders) / TotalPurchaseOrders * 100;
            }
        }

        //public double SalesOrderCompletionRate
        //{
        //    get
        //    {
        //        if (TotalSalesOrders == 0) return 0;
        //        return (double)(TotalSalesOrders - PendingSalesOrders) / TotalSalesOrders * 100;
        //    }
        //}

        public double StockHealthScore
        {
            get
            {
                if (TotalItems == 0) return 100;
                var healthyItems = TotalItems - LowStockItems - CriticalStockItems - OutOfStockItems;
                return (double)healthyItems / TotalItems * 100;
            }
        }

        public string StockHealthScoreClass
        {
            get
            {
                return StockHealthScore switch
                {
                    >= 80 => "text-success",
                    >= 60 => "text-warning",
                    _ => "text-danger"
                };
            }
        }
    }

    /// <summary>
    /// Recent activity untuk dashboard
    /// </summary>
    public class RecentActivity
    {
        public DateTime ActivityDate { get; set; }
        public string ActivityType { get; set; } = string.Empty; // PO_CREATED, SO_SHIPPED, ASN_RECEIVED, etc.
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string BadgeClass { get; set; } = string.Empty;

        public string ActivityDateDisplay => ActivityDate.ToString(Constants.DATETIME_FORMAT);
        public string TimeAgo
        {
            get
            {
                var diff = DateTime.Now - ActivityDate;
                return diff.TotalDays >= 1
                    ? $"{(int)diff.TotalDays} hari lalu"
                    : diff.TotalHours >= 1
                    ? $"{(int)diff.TotalHours} jam lalu"
                    : $"{(int)diff.TotalMinutes} menit lalu";
            }
        }
    }

    /// <summary>
    /// System alerts dan notifications
    /// </summary>
    public class SystemAlert
    {
        public int Id { get; set; }
        public NotificationLevel Level { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ActionUrl { get; set; } = string.Empty;
        public string ActionText { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsRead { get; set; }

        public string LevelIndonesia
        {
            get
            {
                return Level switch
                {
                    NotificationLevel.Info => "Info",
                    NotificationLevel.Warning => "Peringatan",
                    NotificationLevel.Error => "Error",
                    NotificationLevel.Critical => "Kritis",
                    _ => "Unknown"
                };
            }
        }

        public string LevelCssClass
        {
            get
            {
                return Level switch
                {
                    NotificationLevel.Info => "alert-info",
                    NotificationLevel.Warning => "alert-warning",
                    NotificationLevel.Error => "alert-danger",
                    NotificationLevel.Critical => "alert-danger border-danger",
                    _ => "alert-secondary"
                };
            }
        }

        public string BadgeCssClass
        {
            get
            {
                return Level switch
                {
                    NotificationLevel.Info => "badge bg-info",
                    NotificationLevel.Warning => "badge bg-warning",
                    NotificationLevel.Error => "badge bg-danger",
                    NotificationLevel.Critical => "badge bg-danger",
                    _ => "badge bg-secondary"
                };
            }
        }

        public string IconClass
        {
            get
            {
                return Level switch
                {
                    NotificationLevel.Info => "fas fa-info-circle",
                    NotificationLevel.Warning => "fas fa-exclamation-triangle",
                    NotificationLevel.Error => "fas fa-times-circle",
                    NotificationLevel.Critical => "fas fa-exclamation-circle",
                    _ => "fas fa-bell"
                };
            }
        }

        public string CreatedDateDisplay => CreatedDate.ToString(Constants.DATETIME_FORMAT);
    }

    /// <summary>
    /// Data untuk inventory charts
    /// </summary>
    public class InventoryChartData
    {
        public List<ChartDataPoint> StockLevelDistribution { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> InventoryValueByCategory { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> TopItemsByQuantity { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> TopItemsByValue { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> LocationUtilization { get; set; } = new List<ChartDataPoint>();
    }

    /// <summary>
    /// Data untuk order charts
    /// </summary>
    public class OrderChartData
    {
        public List<ChartDataPoint> PurchaseOrderTrend { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> SalesOrderTrend { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> OrderValueTrend { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> OrderStatusDistribution { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> MonthlyOrderComparison { get; set; } = new List<ChartDataPoint>();
    }

    /// <summary>
    /// Data untuk warehouse charts
    /// </summary>
    public class WarehouseChartData
    {
        public List<ChartDataPoint> CapacityUtilizationByArea { get; set; } = new List<ChartDataPoint>();
        public List<ChartDataPoint> MovementFrequencyByLocation { get; set; } = new List<ChartDataPoint>();
    }

    /// <summary>
    /// Generic chart data point
    /// </summary>
    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime? Date { get; set; }

        public string ValueDisplay => Value.ToString("N0");
        public string ValueCurrency => Value.ToString(Constants.CURRENCY_FORMAT);
        public string DateDisplay => Date?.ToString(Constants.DATE_FORMAT) ?? string.Empty;
    }

    /// <summary>
    /// Quick stats untuk dashboard
    /// </summary>
    public class QuickStats
    {
        public decimal TodaysPurchaseValue { get; set; }
        public decimal TodaysSalesValue { get; set; }
        public int TodaysASNReceived { get; set; }
        public int TodaysItemsProcessed { get; set; }
        public int ActiveSuppliers { get; set; }
        public int ActiveCustomers { get; set; }

        // Computed Properties
        public string TodaysPurchaseValueDisplay => TodaysPurchaseValue.ToString(Constants.CURRENCY_FORMAT);
        public string TodaysSalesValueDisplay => TodaysSalesValue.ToString(Constants.CURRENCY_FORMAT);
    }

    /// <summary>
    /// Top performing items
    /// </summary>
    public class TopItem
    {
        public int ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public int MovementCount { get; set; }
        public DateTime LastMovement { get; set; }

        public string ItemDisplay => $"{ItemCode} - {ItemName}";
        public string TotalValueDisplay => TotalValue.ToString(Constants.CURRENCY_FORMAT);
    }

    /// <summary>
    /// Top suppliers
    /// </summary>
    public class TopSupplier
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalOrderValue { get; set; }
        public decimal AverageOrderValue => OrderCount > 0 ? TotalOrderValue / OrderCount : 0;
        public DateTime LastOrderDate { get; set; }

        public string TotalOrderValueDisplay => TotalOrderValue.ToString(Constants.CURRENCY_FORMAT);
        public string AverageOrderValueDisplay => AverageOrderValue.ToString(Constants.CURRENCY_FORMAT);
    }

    /// <summary>
    /// Top customers
    /// </summary>
    public class TopCustomer
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalOrderValue { get; set; }
        public decimal AverageOrderValue => OrderCount > 0 ? TotalOrderValue / OrderCount : 0;
        public DateTime LastOrderDate { get; set; }

        public string TotalOrderValueDisplay => TotalOrderValue.ToString(Constants.CURRENCY_FORMAT);
        public string AverageOrderValueDisplay => AverageOrderValue.ToString(Constants.CURRENCY_FORMAT);
    }

    /// <summary>
    /// Actions yang membutuhkan perhatian
    /// </summary>
    public class PendingActions
    {
        public int PurchaseOrdersToSend { get; set; }
        public int ASNsToProcess { get; set; }
        public int SalesOrdersToShip { get; set; }
        public int ItemsToPutaway { get; set; }
        public int LowStockAlerts { get; set; }
        public int OverCapacityLocations { get; set; }
        public int PendingAdjustments { get; set; }

        public int TotalPendingActions =>
            PurchaseOrdersToSend + ASNsToProcess + SalesOrdersToShip +
            ItemsToPutaway + LowStockAlerts + OverCapacityLocations + PendingAdjustments;

        public bool HasPendingActions => TotalPendingActions > 0;

        public List<PendingActionItem> GetPendingActionsList()
        {
            var actions = new List<PendingActionItem>();

            if (PurchaseOrdersToSend > 0)
                actions.Add(new PendingActionItem
                {
                    Title = "Purchase Orders to Send",
                    Count = PurchaseOrdersToSend,
                    ActionUrl = "/PurchaseOrder?status=Draft",
                    IconClass = "fas fa-paper-plane",
                    BadgeClass = "badge bg-primary"
                });

            if (ASNsToProcess > 0)
                actions.Add(new PendingActionItem
                {
                    Title = "ASNs to Process",
                    Count = ASNsToProcess,
                    ActionUrl = "/ASN?status=Arrived",
                    IconClass = "fas fa-truck",
                    BadgeClass = "badge bg-info"
                });

            if (SalesOrdersToShip > 0)
                actions.Add(new PendingActionItem
                {
                    Title = "Sales Orders to Ship",
                    Count = SalesOrdersToShip,
                    ActionUrl = "/SalesOrder?status=Confirmed",
                    IconClass = "fas fa-shipping-fast",
                    BadgeClass = "badge bg-success"
                });

            if (ItemsToPutaway > 0)
                actions.Add(new PendingActionItem
                {
                    Title = "Items to Putaway",
                    Count = ItemsToPutaway,
                    ActionUrl = "/Inventory/Putaway",
                    IconClass = "fas fa-boxes",
                    BadgeClass = "badge bg-warning"
                });

            if (LowStockAlerts > 0)
                actions.Add(new PendingActionItem
                {
                    Title = "Low Stock Alerts",
                    Count = LowStockAlerts,
                    ActionUrl = "/Inventory?stockLevel=Low",
                    IconClass = "fas fa-exclamation-triangle",
                    BadgeClass = "badge bg-danger"
                });

            if (OverCapacityLocations > 0)
                actions.Add(new PendingActionItem
                {
                    Title = "Over Capacity Locations",
                    Count = OverCapacityLocations,
                    ActionUrl = "/Location?capacity=Over",
                    IconClass = "fas fa-warehouse",
                    BadgeClass = "badge bg-dark"
                });

            return actions;
        }
    }

    /// <summary>
    /// Individual pending action item
    /// </summary>
    public class PendingActionItem
    {
        public string Title { get; set; } = string.Empty;
        public int Count { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string BadgeClass { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel untuk dashboard settings
    /// </summary>
    public class DashboardSettingsViewModel
    {
        [Display(Name = "Auto Refresh")]
        public bool AutoRefresh { get; set; } = true;

        [Display(Name = "Refresh Interval (seconds)")]
        [Range(30, 3600, ErrorMessage = "Refresh interval harus antara 30-3600 detik")]
        public int RefreshInterval { get; set; } = Constants.DASHBOARD_REFRESH_INTERVAL;

        [Display(Name = "Show Recent Activities")]
        public bool ShowRecentActivities { get; set; } = true;
    }
}