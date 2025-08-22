// Models/ViewModels/ItemTrackingViewModel.cs
// ViewModel untuk item tracking dan location management

using System.ComponentModel.DataAnnotations;
using WMS.Utilities;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk item tracking
    /// Menampilkan informasi lengkap tentang keberadaan item di seluruh lokasi
    /// </summary>
    public class ItemTrackingViewModel
    {
        // Filter Properties
        [Display(Name = "Cari Item")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Kategori Item")]
        public int? ItemId { get; set; }

        [Display(Name = "Area Lokasi")]
        public string? LocationArea { get; set; }

        [Display(Name = "Status Stok")]
        public string? StockStatus { get; set; }

        [Display(Name = "Dari Tanggal")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "Sampai Tanggal")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        // Data Properties
        public List<ItemLocationInfo> ItemLocations { get; set; } = new List<ItemLocationInfo>();
        public List<ItemSummary> ItemSummaries { get; set; } = new List<ItemSummary>();
        public List<LocationSummary> LocationSummaries { get; set; } = new List<LocationSummary>();

        // Summary Statistics
        public int TotalItems { get; set; }
        public int TotalLocations { get; set; }
        public int TotalInventoryRecords { get; set; }
        public decimal TotalInventoryValue { get; set; }

        // Stock Level Counts
        public int HighStockCount { get; set; }
        public int MediumStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int CriticalStockCount { get; set; }
        public int EmptyStockCount { get; set; }

        // Lists untuk filter dropdown
        public List<Item> AvailableItems { get; set; } = new List<Item>();
        public List<string> LocationAreas { get; set; } = new List<string>();
        public List<string> StockStatusOptions { get; set; } = new List<string>
        {
            "TINGGI", "SEDANG", "RENDAH", "KRITIS", "KOSONG"
        };

        // Computed Properties
        public string TotalValueDisplay => TotalInventoryValue.ToString("C");

        public Dictionary<string, int> StockLevelDistribution => new()
        {
            { "TINGGI", HighStockCount },
            { "SEDANG", MediumStockCount },
            { "RENDAH", LowStockCount },
            { "KRITIS", CriticalStockCount },
            { "KOSONG", EmptyStockCount }
        };

        public bool HasCriticalItems => CriticalStockCount > 0 || EmptyStockCount > 0;
        public bool HasLowStockItems => LowStockCount > 0;

        // Alert Messages
        public List<string> Alerts
        {
            get
            {
                var alerts = new List<string>();

                if (EmptyStockCount > 0)
                    alerts.Add($"{EmptyStockCount} item kehabisan stok");

                if (CriticalStockCount > 0)
                    alerts.Add($"{CriticalStockCount} item stok kritis");

                if (LowStockCount > 0)
                    alerts.Add($"{LowStockCount} item stok rendah");

                return alerts;
            }
        }
    }

    /// <summary>
    /// Informasi detail item di suatu lokasi
    /// </summary>
    public class ItemLocationInfo
    {
        public int InventoryId { get; set; }
        public int ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemUnit { get; set; } = string.Empty;

        public int LocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationArea => LocationCode.Split('-')[0]; // A dari A-01-01

        public int Quantity { get; set; }
        public decimal LastCostPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public string? Notes { get; set; }

        // Computed Properties
        public string ItemDisplay => $"{ItemCode} - {ItemName}";
        public string LocationDisplay => $"{LocationCode} - {LocationName}";
        public decimal TotalValue => Quantity * LastCostPrice;

        public string StatusIndonesia
        {
            get
            {
                return Status switch
                {
                    Constants.INVENTORY_STATUS_AVAILABLE => "Tersedia",
                    Constants.INVENTORY_STATUS_RESERVED => "Dipesan",
                    Constants.INVENTORY_STATUS_DAMAGED => "Rusak",
                    Constants.INVENTORY_STATUS_QUARANTINE => "Karantina",
                    Constants.INVENTORY_STATUS_BLOCKED => "Diblokir",
                    Constants.INVENTORY_STATUS_EMPTY => "Kosong",
                    _ => Status
                };
            }
        }

        public string StatusCssClass
        {
            get
            {
                return Status switch
                {
                    Constants.INVENTORY_STATUS_AVAILABLE => "badge bg-success",
                    Constants.INVENTORY_STATUS_RESERVED => "badge bg-warning",
                    Constants.INVENTORY_STATUS_DAMAGED => "badge bg-danger",
                    Constants.INVENTORY_STATUS_QUARANTINE => "badge bg-secondary",
                    Constants.INVENTORY_STATUS_BLOCKED => "badge bg-dark",
                    Constants.INVENTORY_STATUS_EMPTY => "badge bg-light text-dark",
                    _ => "badge bg-light text-dark"
                };
            }
        }

        public string StockLevel
        {
            get
            {
                if (Quantity == 0) return "KOSONG";
                if (Quantity <= Constants.CRITICAL_STOCK_THRESHOLD) return "KRITIS";
                if (Quantity <= Constants.LOW_STOCK_THRESHOLD) return "RENDAH";
                if (Quantity <= 50) return "SEDANG";
                return "TINGGI";
            }
        }

        public string StockLevelCssClass
        {
            get
            {
                return StockLevel switch
                {
                    "KOSONG" => "text-danger fw-bold",
                    "KRITIS" => "text-danger fw-bold",
                    "RENDAH" => "text-warning fw-bold",
                    "SEDANG" => "text-info",
                    "TINGGI" => "text-success",
                    _ => "text-muted"
                };
            }
        }

        public bool IsAvailableForSale => Status == Constants.INVENTORY_STATUS_AVAILABLE && Quantity > 0;
    }

    /// <summary>
    /// Ringkasan per item (total di semua lokasi)
    /// </summary>
    public class ItemSummary
    {
        public int ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemUnit { get; set; } = string.Empty;
        public decimal StandardPrice { get; set; }

        public int TotalQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int DamagedQuantity { get; set; }
        public int LocationCount { get; set; }

        public decimal AverageCostPrice { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime LastUpdated { get; set; }

        // Computed Properties
        public string ItemDisplay => $"{ItemCode} - {ItemName}";
        public decimal ValuePerUnit => TotalQuantity > 0 ? TotalValue / TotalQuantity : 0;

        public string StockLevel
        {
            get
            {
                if (TotalQuantity == 0) return "KOSONG";
                if (TotalQuantity <= Constants.CRITICAL_STOCK_THRESHOLD) return "KRITIS";
                if (TotalQuantity <= Constants.LOW_STOCK_THRESHOLD) return "RENDAH";
                if (TotalQuantity <= 50) return "SEDANG";
                return "TINGGI";
            }
        }

        public string StockLevelCssClass
        {
            get
            {
                return StockLevel switch
                {
                    "KOSONG" => "text-danger fw-bold",
                    "KRITIS" => "text-danger fw-bold",
                    "RENDAH" => "text-warning fw-bold",
                    "SEDANG" => "text-info",
                    "TINGGI" => "text-success",
                    _ => "text-muted"
                };
            }
        }

        public bool NeedsAttention => StockLevel == "KOSONG" || StockLevel == "KRITIS";

        // Stock Distribution
        public double AvailablePercentage => TotalQuantity > 0 ? (double)AvailableQuantity / TotalQuantity * 100 : 0;
        public double ReservedPercentage => TotalQuantity > 0 ? (double)ReservedQuantity / TotalQuantity * 100 : 0;
        public double DamagedPercentage => TotalQuantity > 0 ? (double)DamagedQuantity / TotalQuantity * 100 : 0;
    }

    /// <summary>
    /// Ringkasan per lokasi
    /// </summary>
    public class LocationSummary
    {
        public int LocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationArea => LocationCode.Split('-')[0];

        public int MaxCapacity { get; set; }
        public int CurrentCapacity { get; set; }
        public int ItemTypeCount { get; set; } // Berapa jenis item
        public int TotalQuantity { get; set; }  // Total quantity semua item
        public decimal TotalValue { get; set; }

        public DateTime LastActivity { get; set; }
        public bool IsActive { get; set; }

        // Computed Properties
        public string LocationDisplay => $"{LocationCode} - {LocationName}";
        public int AvailableCapacity => MaxCapacity - CurrentCapacity;

        public double CapacityPercentage
        {
            get
            {
                if (MaxCapacity == 0) return 0;
                return (double)CurrentCapacity / MaxCapacity * 100;
            }
        }

        public string CapacityStatus
        {
            get
            {
                if (CapacityPercentage >= 100) return "PENUH";
                if (CapacityPercentage >= Constants.LOCATION_FULL_THRESHOLD * 100) return "HAMPIR PENUH";
                if (CapacityPercentage >= 50) return "SETENGAH";
                if (CapacityPercentage > 0) return "TERSEDIA";
                return "KOSONG";
            }
        }

        public string CapacityStatusCssClass
        {
            get
            {
                return CapacityStatus switch
                {
                    "PENUH" => "badge bg-danger",
                    "HAMPIR PENUH" => "badge bg-warning",
                    "SETENGAH" => "badge bg-info",
                    "TERSEDIA" => "badge bg-success",
                    "KOSONG" => "badge bg-light text-dark",
                    _ => "badge bg-secondary"
                };
            }
        }

        public bool IsNearlyFull => CapacityPercentage >= Constants.LOCATION_FULL_THRESHOLD * 100;
        public bool IsFull => CapacityPercentage >= 100;
        public bool IsEmpty => CurrentCapacity == 0;

        public string UtilizationSummary => $"{CurrentCapacity}/{MaxCapacity} ({CapacityPercentage:F1}%)";
    }

    /// <summary>
    /// ViewModel untuk stock movement history
    /// </summary>
    public class StockMovementViewModel
    {
        [Display(Name = "Item")]
        public int? ItemId { get; set; }

        [Display(Name = "Lokasi")]
        public int? LocationId { get; set; }

        [Display(Name = "Tipe Movement")]
        public string? MovementType { get; set; }

        [Display(Name = "Dari Tanggal")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "Sampai Tanggal")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        public List<StockMovementRecord> Movements { get; set; } = new List<StockMovementRecord>();

        // Lists untuk filter
        public List<Item> AvailableItems { get; set; } = new List<Item>();
        public List<Location> AvailableLocations { get; set; } = new List<Location>();
        public List<string> MovementTypes { get; set; } = new List<string>
        {
            "INBOUND", "OUTBOUND", "ADJUSTMENT", "TRANSFER", "DAMAGE", "RETURN"
        };

        // Summary
        public int TotalMovements => Movements.Count;
        public int InboundMovements => Movements.Count(m => m.MovementType == "INBOUND");
        public int OutboundMovements => Movements.Count(m => m.MovementType == "OUTBOUND");
        public decimal TotalValueIn => Movements.Where(m => m.MovementType == "INBOUND").Sum(m => m.ValueChange);
        public decimal TotalValueOut => Movements.Where(m => m.MovementType == "OUTBOUND").Sum(m => Math.Abs(m.ValueChange));
    }

    /// <summary>
    /// Record untuk stock movement history
    /// </summary>
    public class StockMovementRecord
    {
        public int Id { get; set; }
        public DateTime MovementDate { get; set; }
        public string MovementType { get; set; } = string.Empty; // INBOUND, OUTBOUND, ADJUSTMENT, etc.
        public string ReferenceType { get; set; } = string.Empty; // PO, SO, ASN, MANUAL
        public string ReferenceNumber { get; set; } = string.Empty;

        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;

        public int QuantityBefore { get; set; }
        public int QuantityChange { get; set; }
        public int QuantityAfter { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal ValueChange => QuantityChange * UnitPrice;

        public string Notes { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;

        // Computed Properties
        public string ItemDisplay => $"{ItemCode} - {ItemName}";
        public string LocationDisplay => $"{LocationCode} - {LocationName}";

        public string MovementTypeIndonesia
        {
            get
            {
                return MovementType switch
                {
                    "INBOUND" => "Masuk",
                    "OUTBOUND" => "Keluar",
                    "ADJUSTMENT" => "Penyesuaian",
                    "TRANSFER" => "Transfer",
                    "DAMAGE" => "Rusak",
                    "RETURN" => "Retur",
                    _ => MovementType
                };
            }
        }

        public string MovementTypeCssClass
        {
            get
            {
                return MovementType switch
                {
                    "INBOUND" => "badge bg-success",
                    "OUTBOUND" => "badge bg-primary",
                    "ADJUSTMENT" => "badge bg-warning",
                    "TRANSFER" => "badge bg-info",
                    "DAMAGE" => "badge bg-danger",
                    "RETURN" => "badge bg-secondary",
                    _ => "badge bg-light"
                };
            }
        }

        public string QuantityChangeDisplay
        {
            get
            {
                if (QuantityChange > 0)
                    return $"+{QuantityChange}";
                return QuantityChange.ToString();
            }
        }

        public string QuantityChangeCssClass => QuantityChange >= 0 ? "text-success" : "text-danger";

        public string MovementSummary => $"{MovementTypeIndonesia}: {QuantityChangeDisplay} @ {LocationDisplay}";
    }
}