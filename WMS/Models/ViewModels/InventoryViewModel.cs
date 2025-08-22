// Models/ViewModels/InventoryViewModel.cs
// ViewModel untuk inventory management dan putaway process

using System.ComponentModel.DataAnnotations;
using WMS.Utilities;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk tampilan inventory management
    /// Menggabungkan data inventory dengan informasi tambahan untuk UI
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

        [Range(0, int.MaxValue, ErrorMessage = "Quantity tidak boleh negatif")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 0;

        [Display(Name = "Last Cost Price")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal LastCostPrice { get; set; } = 0;

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = Constants.INVENTORY_STATUS_AVAILABLE;

        [MaxLength(200)]
        [Display(Name = "Catatan")]
        public string? Notes { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // Display Properties dari Navigation Objects
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemUnit { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int LocationMaxCapacity { get; set; }
        public int LocationCurrentCapacity { get; set; }

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

        public string QuantityCssClass
        {
            get
            {
                if (Quantity == 0) return "text-danger fw-bold";
                if (Quantity <= Constants.LOW_STOCK_THRESHOLD) return "text-warning fw-bold";
                return "text-success";
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

        public bool IsAvailableForSale => Status == Constants.INVENTORY_STATUS_AVAILABLE && Quantity > 0;

        public string Summary => $"{ItemDisplay} @ {LocationDisplay} ({Quantity} {ItemUnit})";

        // Location Capacity Info
        public double LocationCapacityPercentage
        {
            get
            {
                if (LocationMaxCapacity == 0) return 0;
                return (double)LocationCurrentCapacity / LocationMaxCapacity * 100;
            }
        }

        public int LocationAvailableCapacity => LocationMaxCapacity - LocationCurrentCapacity;

        public bool IsLocationAlmostFull => LocationCapacityPercentage >= 80;

        // Lists untuk dropdown
        public List<Item> AvailableItems { get; set; } = new List<Item>();
        public List<Location> AvailableLocations { get; set; } = new List<Location>();

        // Status options untuk dropdown
        public List<string> StatusOptions { get; set; } = new List<string>
        {
            Constants.INVENTORY_STATUS_AVAILABLE,
            Constants.INVENTORY_STATUS_RESERVED,
            Constants.INVENTORY_STATUS_DAMAGED,
            Constants.INVENTORY_STATUS_QUARANTINE,
            Constants.INVENTORY_STATUS_BLOCKED
        };
    }

    /// <summary>
    /// ViewModel untuk putaway process
    /// Digunakan saat memindahkan barang dari ASN ke lokasi penyimpanan
    /// </summary>
    public class PutawayViewModel
    {
        [Required(ErrorMessage = "ASN wajib dipilih")]
        [Display(Name = "ASN")]
        public int ASNId { get; set; }

        [Required(ErrorMessage = "ASN Detail wajib dipilih")]
        [Display(Name = "Item dari ASN")]
        public int ASNDetailId { get; set; }

        [Required(ErrorMessage = "Lokasi tujuan wajib dipilih")]
        [Display(Name = "Lokasi Tujuan")]
        public int LocationId { get; set; }

        [Required(ErrorMessage = "Quantity putaway wajib diisi")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity harus lebih besar dari 0")]
        [Display(Name = "Quantity Putaway")]
        public int QuantityToPutaway { get; set; }

        [MaxLength(200)]
        [Display(Name = "Catatan Putaway")]
        public string? PutawayNotes { get; set; }

        // ASN Information
        public string ASNNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime ASNDate { get; set; }

        // Item Information dari ASN Detail
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemUnit { get; set; } = string.Empty;
        public int ReceivedQuantity { get; set; }
        public int AlreadyPutawayQuantity { get; set; }
        public int RemainingQuantity => ReceivedQuantity - AlreadyPutawayQuantity;
        public decimal CostPrice { get; set; }

        // Location Information
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int LocationMaxCapacity { get; set; }
        public int LocationCurrentCapacity { get; set; }
        public int LocationAvailableCapacity => LocationMaxCapacity - LocationCurrentCapacity;

        // Display Properties
        public string ItemDisplay => $"{ItemCode} - {ItemName}";
        public string LocationDisplay => $"{LocationCode} - {LocationName}";
        public string ASNDisplay => $"{ASNNumber} - {SupplierName}";

        // Validation Properties
        public bool CanPutaway => RemainingQuantity > 0;
        public bool IsLocationSuitable => LocationAvailableCapacity >= QuantityToPutaway;

        // Lists untuk dropdown
        public List<AdvancedShippingNotice> AvailableASNs { get; set; } = new List<AdvancedShippingNotice>();
        public List<ASNDetail> AvailableASNDetails { get; set; } = new List<ASNDetail>();
        public List<Location> AvailableLocations { get; set; } = new List<Location>();

        // Computed Properties
        public double LocationCapacityAfterPutaway
        {
            get
            {
                if (LocationMaxCapacity == 0) return 0;
                var newCapacity = LocationCurrentCapacity + QuantityToPutaway;
                return (double)newCapacity / LocationMaxCapacity * 100;
            }
        }

        public bool WillLocationBeOverCapacity => LocationCapacityAfterPutaway > 100;

        public string ValidationMessage
        {
            get
            {
                if (!CanPutaway)
                    return "Tidak ada barang yang perlu di-putaway lagi.";

                if (QuantityToPutaway > RemainingQuantity)
                    return $"Quantity melebihi sisa yang harus di-putaway ({RemainingQuantity} {ItemUnit}).";

                if (WillLocationBeOverCapacity)
                    return $"Lokasi akan melebihi kapasitas maksimum. Sisa kapasitas: {LocationAvailableCapacity}";

                return string.Empty;
            }
        }

        public bool IsValid => CanPutaway &&
                              QuantityToPutaway <= RemainingQuantity &&
                              !WillLocationBeOverCapacity;
    }

    /// <summary>
    /// ViewModel untuk inventory adjustment
    /// Digunakan untuk penyesuaian stok manual
    /// </summary>
    public class InventoryAdjustmentViewModel
    {
        [Required(ErrorMessage = "Inventory wajib dipilih")]
        public int InventoryId { get; set; }

        [Display(Name = "Quantity Saat Ini")]
        public int CurrentQuantity { get; set; }

        [Required(ErrorMessage = "Quantity baru wajib diisi")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity tidak boleh negatif")]
        [Display(Name = "Quantity Baru")]
        public int NewQuantity { get; set; }

        [Required(ErrorMessage = "Alasan adjustment wajib diisi")]
        [MaxLength(500, ErrorMessage = "Alasan maksimal 500 karakter")]
        [Display(Name = "Alasan Adjustment")]
        public string AdjustmentReason { get; set; } = string.Empty;

        // Display Information
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemUnit { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;

        // Computed Properties
        public string ItemDisplay => $"{ItemCode} - {ItemName}";
        public string LocationDisplay => $"{LocationCode} - {LocationName}";
        public int QuantityDifference => NewQuantity - CurrentQuantity;
        public string AdjustmentType => QuantityDifference >= 0 ? "Penambahan" : "Pengurangan";
        public string AdjustmentTypeCssClass => QuantityDifference >= 0 ? "text-success" : "text-danger";

        public string AdjustmentSummary
        {
            get
            {
                if (QuantityDifference == 0) return "Tidak ada perubahan";
                var action = QuantityDifference > 0 ? "ditambah" : "dikurangi";
                var amount = Math.Abs(QuantityDifference);
                return $"Stok akan {action} {amount} {ItemUnit}";
            }
        }
    }

    /// <summary>
    /// ViewModel untuk stock transfer antar lokasi
    /// </summary>
    public class StockTransferViewModel
    {
        [Required(ErrorMessage = "Inventory source wajib dipilih")]
        [Display(Name = "Dari Inventory")]
        public int FromInventoryId { get; set; }

        [Required(ErrorMessage = "Lokasi tujuan wajib dipilih")]
        [Display(Name = "Ke Lokasi")]
        public int ToLocationId { get; set; }

        [Required(ErrorMessage = "Quantity transfer wajib diisi")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity harus lebih besar dari 0")]
        [Display(Name = "Quantity Transfer")]
        public int TransferQuantity { get; set; }

        [MaxLength(200)]
        [Display(Name = "Catatan Transfer")]
        public string? TransferNotes { get; set; }

        // Source Information
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemUnit { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
        public string FromLocationCode { get; set; } = string.Empty;
        public string FromLocationName { get; set; } = string.Empty;

        // Destination Information
        public string ToLocationCode { get; set; } = string.Empty;
        public string ToLocationName { get; set; } = string.Empty;
        public int ToLocationMaxCapacity { get; set; }
        public int ToLocationCurrentCapacity { get; set; }
        public int ToLocationAvailableCapacity => ToLocationMaxCapacity - ToLocationCurrentCapacity;

        // Display Properties
        public string ItemDisplay => $"{ItemCode} - {ItemName}";
        public string FromLocationDisplay => $"{FromLocationCode} - {FromLocationName}";
        public string ToLocationDisplay => $"{ToLocationCode} - {ToLocationName}";

        // Validation Properties
        public bool HasSufficientStock => TransferQuantity <= AvailableQuantity;
        public bool DestinationHasCapacity => TransferQuantity <= ToLocationAvailableCapacity;

        public string ValidationMessage
        {
            get
            {
                if (!HasSufficientStock)
                    return $"Stok tidak mencukupi. Tersedia: {AvailableQuantity} {ItemUnit}";

                if (!DestinationHasCapacity)
                    return $"Kapasitas lokasi tujuan tidak cukup. Tersedia: {ToLocationAvailableCapacity}";

                return string.Empty;
            }
        }

        public bool IsValid => HasSufficientStock && DestinationHasCapacity;

        // Lists untuk dropdown
        public List<Inventory> AvailableInventories { get; set; } = new List<Inventory>();
        public List<Location> AvailableLocations { get; set; } = new List<Location>();
    }
}