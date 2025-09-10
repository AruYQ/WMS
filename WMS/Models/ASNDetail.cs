// Models/ASNDetail.cs
// Model untuk detail item dalam Advanced Shipping Notice

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk detail item dalam Advanced Shipping Notice
    /// Berisi informasi actual price dan warehouse fee calculation
    /// </summary>
    public class ASNDetail : BaseEntity
    {
        /// <summary>
        /// ID ASN yang memiliki detail ini
        /// </summary>
        [Required]
        [Display(Name = "ASN")]
        public int ASNId { get; set; }

        /// <summary>
        /// ID Item yang dikirim
        /// </summary>
        [Required(ErrorMessage = "Item wajib dipilih")]
        [Display(Name = "Item")]
        public int ItemId { get; set; }

        /// <summary>
        /// Jumlah yang benar-benar dikirim (mungkin berbeda dari PO)
        /// </summary>
        [Required(ErrorMessage = "Shipped Quantity wajib diisi")]
        [Range(1, int.MaxValue, ErrorMessage = "Shipped Quantity harus lebih dari 0")]
        [Display(Name = "Qty Dikirim")]
        public int ShippedQuantity { get; set; }

        /// <summary>
        /// Harga actual per item yang dibayar ke supplier
        /// Ini yang akan digunakan untuk menghitung warehouse fee
        /// </summary>
        [Required(ErrorMessage = "Actual Price wajib diisi")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Actual Price harus lebih dari 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Harga Actual per Item")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal ActualPricePerItem { get; set; }

        /// <summary>
        /// Rate warehouse fee yang diterapkan (dalam decimal: 0.05 = 5%)
        /// Dihitung berdasarkan ActualPricePerItem:
        /// ≤ 1,000,000: 5% (0.05)
        /// > 1,000,000 ≤ 10,000,000: 3% (0.03)  
        /// > 10,000,000: 1% (0.01)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(5,4)")]
        [Display(Name = "Warehouse Fee Rate")]
        [DisplayFormat(DataFormatString = "{0:P2}", ApplyFormatInEditMode = false)]
        public decimal WarehouseFeeRate { get; set; }

        /// <summary>
        /// Jumlah warehouse fee dalam rupiah per item
        /// ActualPricePerItem × WarehouseFeeRate
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Warehouse Fee Amount")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal WarehouseFeeAmount { get; set; }

        /// <summary>
        /// Catatan khusus untuk item ini
        /// </summary>
        [MaxLength(200)]
        [Display(Name = "Catatan")]
        public string? Notes { get; set; }
        /// <summary>
        /// Jumlah yang masih perlu di-putaway
        /// Dihitung: ShippedQuantity - AlreadyPutAwayQuantity
        /// </summary>
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Remaining Quantity tidak boleh negatif")]
        [Display(Name = "Sisa Quantity")]
        public int RemainingQuantity { get; set; }

        /// <summary>
        /// Jumlah yang sudah di-putaway ke inventory
        /// </summary>
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Already Put Away Quantity tidak boleh negatif")]
        [Display(Name = "Sudah Di-putaway")]
        public int AlreadyPutAwayQuantity { get; set; } = 0;

        // Navigation Properties
        /// <summary>
        /// ASN yang memiliki detail ini
        /// </summary>
        public virtual AdvancedShippingNotice ASN { get; set; } = null!;

        /// <summary>
        /// Item yang dikirim
        /// </summary>
        public virtual Item Item { get; set; } = null!;

        // Methods
        /// <summary>
        /// Menghitung warehouse fee berdasarkan actual price
        /// FIXED: Updated rates sesuai requirement baru
        /// </summary>
        public void CalculateWarehouseFee()
        {
            // FIXED: Logic warehouse fee sesuai requirement baru:
            // < 1 juta per item: 3%
            // 1 juta - 10 juta per item: 2%  
            // > 10 juta per item: 1%

            if (ActualPricePerItem <= 1000000m)
            {
                WarehouseFeeRate = 0.03m; // FIXED: 3% (was 0.05m)
            }
            else if (ActualPricePerItem <= 10000000m)
            {
                WarehouseFeeRate = 0.02m; // FIXED: 2% (was 0.03m)
            }
            else
            {
                WarehouseFeeRate = 0.01m; // 1% (unchanged)
            }

            WarehouseFeeAmount = ActualPricePerItem * WarehouseFeeRate;
        }

        /// <summary>
        /// Initialize RemainingQuantity saat ASNDetail dibuat
        /// </summary>
        public void InitializeRemainingQuantity()
        {
            RemainingQuantity = ShippedQuantity;
            AlreadyPutAwayQuantity = 0;
        }

        /// <summary>
        /// Update putaway quantity dan recalculate remaining
        /// </summary>
        public void UpdatePutawayQuantity(int putawayQuantity)
        {
            // Handle case where putawayQuantity is 0 (no previous putaway)
            if (putawayQuantity < 0)
                throw new ArgumentException("Putaway quantity cannot be negative");
            
            if (putawayQuantity > ShippedQuantity)
                throw new ArgumentException("Putaway quantity cannot exceed shipped quantity");

            // FIX: Set total putaway quantity, don't add to existing
            AlreadyPutAwayQuantity = putawayQuantity;
            RemainingQuantity = ShippedQuantity - AlreadyPutAwayQuantity;
        }

        /// <summary>
        /// Add incremental putaway quantity (for partial putaway)
        /// </summary>
        public void AddPutawayQuantity(int additionalQuantity)
        {
            if (additionalQuantity <= 0)
                throw new ArgumentException("Additional putaway quantity must be positive");
            
            if (additionalQuantity > RemainingQuantity)
                throw new ArgumentException("Additional putaway quantity cannot exceed remaining quantity");

            AlreadyPutAwayQuantity += additionalQuantity;
            RemainingQuantity = ShippedQuantity - AlreadyPutAwayQuantity;
        }

        // Computed Properties
        /// <summary>
        /// Display text untuk item (kode + nama)
        /// </summary>
        [NotMapped]
        public string ItemDisplay => Item?.DisplayName ?? string.Empty;

        /// <summary>
        /// Unit satuan dari item
        /// </summary>
        [NotMapped]
        public string ItemUnit => Item?.Unit ?? string.Empty;

        /// <summary>
        /// Total actual value untuk quantity yang dikirim
        /// ShippedQuantity × ActualPricePerItem
        /// </summary>
        [NotMapped]
        public decimal TotalActualValue => ShippedQuantity * ActualPricePerItem;

        /// <summary>
        /// Total warehouse fee untuk quantity yang dikirim
        /// ShippedQuantity × WarehouseFeeAmount
        /// </summary>
        [NotMapped]
        public decimal TotalWarehouseFee => ShippedQuantity * WarehouseFeeAmount;

        /// <summary>
        /// Warehouse fee rate dalam bentuk persentase untuk tampilan
        /// </summary>
        [NotMapped]
        public string WarehouseFeeRateDisplay => $"{WarehouseFeeRate * 100:0.##}%";

        /// <summary>
        /// CSS class untuk styling warehouse fee rate
        /// </summary>
        [NotMapped]
        public string WarehouseFeeRateCssClass
        {
            get
            {
                if (WarehouseFeeRate >= 0.05m) return "badge bg-danger"; // 5%
                if (WarehouseFeeRate >= 0.03m) return "badge bg-warning"; // 3%
                return "badge bg-success"; // 1%
            }
        }

        /// <summary>
        /// Kategori harga untuk warehouse fee
        /// </summary>
        [NotMapped]
        public string PriceCategory
        {
            get
            {
                if (ActualPricePerItem <= 1000000m) return "Harga Rendah (≤ 1 Juta)";
                if (ActualPricePerItem <= 10000000m) return "Harga Menengah (1-10 Juta)";
                return "Harga Tinggi (> 10 Juta)";
            }
        }

        /// <summary>
        /// Informasi lengkap untuk tampilan di grid
        /// </summary>
        [NotMapped]
        public string FullDescription => $"{ItemDisplay} - {ShippedQuantity} {ItemUnit} @ {ActualPricePerItem:C} (Fee: {WarehouseFeeRateDisplay})";
    }
}