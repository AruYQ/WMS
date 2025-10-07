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
        public string FullDescription => $"{ItemDisplay} - {ShippedQuantity} {ItemUnit} @ {ActualPricePerItem:C}";
    }
}