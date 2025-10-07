using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk Picking Detail - detail pengambilan barang per item dan lokasi
    /// </summary>
    public class PickingDetail : BaseEntity
    {
        /// <summary>
        /// ID Picking document
        /// </summary>
        [Required]
        [Display(Name = "Picking")]
        public int PickingId { get; set; }

        /// <summary>
        /// ID Sales Order Detail (line item dari SO)
        /// </summary>
        [Required]
        [Display(Name = "Sales Order Detail")]
        public int SalesOrderDetailId { get; set; }

        /// <summary>
        /// ID Item yang akan dipick
        /// </summary>
        [Required]
        [Display(Name = "Item")]
        public int ItemId { get; set; }

        /// <summary>
        /// ID Lokasi tempat item akan dipick
        /// </summary>
        [Required]
        [Display(Name = "Location")]
        public int LocationId { get; set; }

        /// <summary>
        /// Quantity yang dibutuhkan dari SO Detail
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity harus lebih dari 0")]
        [Display(Name = "Quantity Required")]
        public int QuantityRequired { get; set; }

        /// <summary>
        /// Quantity yang sudah dipick (actual)
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Quantity tidak boleh negatif")]
        [Display(Name = "Quantity Picked")]
        public int QuantityPicked { get; set; } = 0;

        /// <summary>
        /// Sisa quantity yang belum dipick
        /// </summary>
        [Display(Name = "Remaining Quantity")]
        public int RemainingQuantity { get; set; }

        /// <summary>
        /// Status detail picking (Pending, Picked, Short)
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Catatan untuk detail ini
        /// </summary>
        [MaxLength(200)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        // Navigation Properties
        /// <summary>
        /// Picking document induk
        /// </summary>
        public virtual Picking Picking { get; set; } = null!;

        /// <summary>
        /// Sales Order Detail yang terkait
        /// </summary>
        public virtual SalesOrderDetail SalesOrderDetail { get; set; } = null!;

        /// <summary>
        /// Item yang dipick
        /// </summary>
        public virtual Item Item { get; set; } = null!;

        /// <summary>
        /// Lokasi tempat picking
        /// </summary>
        public virtual Location Location { get; set; } = null!;

        // Computed Properties
        /// <summary>
        /// Status dalam bahasa Indonesia
        /// </summary>
        [NotMapped]
        public string StatusIndonesia
        {
            get
            {
                return Status switch
                {
                    "Pending" => "Menunggu",
                    "Picked" => "Sudah Dipick",
                    "Short" => "Kurang",
                    _ => Status
                };
            }
        }

        /// <summary>
        /// CSS class untuk status
        /// </summary>
        [NotMapped]
        public string StatusCssClass
        {
            get
            {
                return Status switch
                {
                    "Pending" => "badge bg-secondary",
                    "Picked" => "badge bg-success",
                    "Short" => "badge bg-warning",
                    _ => "badge bg-light"
                };
            }
        }

        /// <summary>
        /// Display text untuk item
        /// </summary>
        [NotMapped]
        public string ItemDisplay => Item?.DisplayName ?? $"Item {ItemId}";

        /// <summary>
        /// Display text untuk item code
        /// </summary>
        [NotMapped]
        public string ItemCode => Item?.ItemCode ?? string.Empty;

        /// <summary>
        /// Display text untuk item name
        /// </summary>
        [NotMapped]
        public string ItemName => Item?.Name ?? string.Empty;

        /// <summary>
        /// Unit satuan dari item
        /// </summary>
        [NotMapped]
        public string ItemUnit => Item?.Unit ?? "PCS";

        /// <summary>
        /// Display text untuk location
        /// </summary>
        [NotMapped]
        public string LocationDisplay => Location?.DisplayName ?? $"Location {LocationId}";

        /// <summary>
        /// Location code
        /// </summary>
        [NotMapped]
        public string LocationCode => Location?.Code ?? string.Empty;

        /// <summary>
        /// Nama lokasi
        /// </summary>
        [NotMapped]
        public string LocationName => Location?.Name ?? string.Empty;

        /// <summary>
        /// Persentase picked dari required
        /// </summary>
        [NotMapped]
        public decimal PickedPercentage
        {
            get
            {
                if (QuantityRequired == 0) return 0;
                return Math.Round((decimal)QuantityPicked / QuantityRequired * 100, 2);
            }
        }

        /// <summary>
        /// Apakah fully picked
        /// </summary>
        [NotMapped]
        public bool IsFullyPicked => QuantityPicked >= QuantityRequired;

        /// <summary>
        /// Apakah partial picked (short)
        /// </summary>
        [NotMapped]
        public bool IsPartialPicked => QuantityPicked > 0 && QuantityPicked < QuantityRequired;

        /// <summary>
        /// CSS class untuk progress bar
        /// </summary>
        [NotMapped]
        public string ProgressBarCssClass
        {
            get
            {
                if (IsFullyPicked) return "bg-success";
                if (IsPartialPicked) return "bg-warning";
                return "bg-secondary";
            }
        }

        // Business Logic Methods
        /// <summary>
        /// Update picked quantity dan recalculate remaining
        /// </summary>
        public void UpdatePickedQuantity(int pickedQty)
        {
            if (pickedQty < 0) pickedQty = 0;
            if (pickedQty > QuantityRequired) pickedQty = QuantityRequired;

            QuantityPicked = pickedQty;
            RemainingQuantity = QuantityRequired - QuantityPicked;
            
            // Update status based on picked quantity
            if (QuantityPicked == 0)
                Status = "Pending";
            else if (QuantityPicked >= QuantityRequired)
                Status = "Picked";
            else
                Status = "Short";

            ModifiedDate = DateTime.Now;
        }

        /// <summary>
        /// Add picked quantity (incremental)
        /// </summary>
        public void AddPickedQuantity(int additionalQty)
        {
            var newTotal = QuantityPicked + additionalQty;
            UpdatePickedQuantity(newTotal);
        }

        /// <summary>
        /// Calculate remaining quantity
        /// </summary>
        public void CalculateRemaining()
        {
            RemainingQuantity = QuantityRequired - QuantityPicked;
        }

        /// <summary>
        /// Validate picking detail
        /// </summary>
        public bool IsValid()
        {
            return PickingId > 0 
                && SalesOrderDetailId > 0 
                && ItemId > 0 
                && LocationId > 0 
                && QuantityRequired > 0 
                && QuantityPicked >= 0 
                && QuantityPicked <= QuantityRequired;
        }
    }
}
