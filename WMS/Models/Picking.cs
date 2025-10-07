using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk Picking - dokumen pengambilan barang dari warehouse untuk Sales Order
    /// Kebalikan dari Putaway (ASN)
    /// </summary>
    public class Picking : BaseEntity
    {
        /// <summary>
        /// Nomor Picking yang unik (auto-generated)
        /// Format: PKG-YYYY-MM-DD-XXX
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "Picking Number")]
        public string PickingNumber { get; set; } = string.Empty;

        /// <summary>
        /// ID Sales Order yang akan dipick
        /// </summary>
        [Required(ErrorMessage = "Sales Order wajib dipilih")]
        [Display(Name = "Sales Order")]
        public int SalesOrderId { get; set; }

        /// <summary>
        /// Tanggal picking dibuat/dimulai
        /// </summary>
        [Required]
        [Display(Name = "Picking Date")]
        [DataType(DataType.Date)]
        public DateTime PickingDate { get; set; } = DateTime.Today;

        /// <summary>
        /// Tanggal picking selesai
        /// </summary>
        [Display(Name = "Completed Date")]
        [DataType(DataType.DateTime)]
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Status Picking (Pending, InProgress, Completed, Cancelled)
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Catatan tambahan untuk picking
        /// </summary>
        [MaxLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        // Navigation Properties
        /// <summary>
        /// Sales Order yang terkait dengan picking ini
        /// </summary>
        public virtual SalesOrder SalesOrder { get; set; } = null!;

        /// <summary>
        /// Detail picking per item dan lokasi
        /// </summary>
        public virtual ICollection<PickingDetail> PickingDetails { get; set; } = new List<PickingDetail>();

        // Computed Properties
        /// <summary>
        /// Status dalam bahasa Indonesia untuk tampilan
        /// </summary>
        [NotMapped]
        public string StatusIndonesia
        {
            get
            {
                return Status switch
                {
                    "Pending" => "Menunggu",
                    "InProgress" => "Sedang Proses",
                    "Completed" => "Selesai",
                    "Cancelled" => "Dibatalkan",
                    _ => Status
                };
            }
        }

        /// <summary>
        /// CSS class untuk styling status
        /// </summary>
        [NotMapped]
        public string StatusCssClass
        {
            get
            {
                return Status switch
                {
                    "Pending" => "badge bg-secondary",
                    "InProgress" => "badge bg-warning",
                    "Completed" => "badge bg-success",
                    "Cancelled" => "badge bg-danger",
                    _ => "badge bg-light"
                };
            }
        }

        /// <summary>
        /// Total quantity yang harus dipick
        /// </summary>
        [NotMapped]
        public int TotalQuantityRequired => PickingDetails?.Sum(d => d.QuantityRequired) ?? 0;

        /// <summary>
        /// Total quantity yang sudah dipick
        /// </summary>
        [NotMapped]
        public int TotalQuantityPicked => PickingDetails?.Sum(d => d.QuantityPicked) ?? 0;

        /// <summary>
        /// Total quantity yang masih tersisa
        /// </summary>
        [NotMapped]
        public int TotalQuantityRemaining => PickingDetails?.Sum(d => d.RemainingQuantity) ?? 0;

        /// <summary>
        /// Persentase picking completion
        /// </summary>
        [NotMapped]
        public decimal CompletionPercentage
        {
            get
            {
                if (TotalQuantityRequired == 0) return 0;
                return Math.Round((decimal)TotalQuantityPicked / TotalQuantityRequired * 100, 2);
            }
        }

        /// <summary>
        /// Apakah picking sudah complete (semua item terpick)
        /// </summary>
        [NotMapped]
        public bool IsFullyPicked => TotalQuantityRemaining == 0 && TotalQuantityRequired > 0;

        /// <summary>
        /// Apakah ada item yang short (kurang dari required)
        /// </summary>
        [NotMapped]
        public bool HasShortItems => PickingDetails?.Any(d => d.Status == "Short") ?? false;

        /// <summary>
        /// Jumlah item yang berbeda
        /// </summary>
        [NotMapped]
        public int TotalItemTypes => PickingDetails?.Select(d => d.ItemId).Distinct().Count() ?? 0;

        /// <summary>
        /// Jumlah lokasi yang digunakan
        /// </summary>
        [NotMapped]
        public int TotalLocationsUsed => PickingDetails?.Select(d => d.LocationId).Distinct().Count() ?? 0;

        /// <summary>
        /// Apakah picking bisa diedit
        /// </summary>
        [NotMapped]
        public bool CanBeEdited => Status == "Pending" || Status == "InProgress";

        /// <summary>
        /// Apakah picking bisa di-complete
        /// </summary>
        [NotMapped]
        public bool CanBeCompleted => Status == "InProgress" && TotalQuantityPicked > 0;

        /// <summary>
        /// Apakah picking bisa dibatalkan
        /// </summary>
        [NotMapped]
        public bool CanBeCancelled => Status == "Pending" || Status == "InProgress";

        /// <summary>
        /// Display text untuk SO Number
        /// </summary>
        [NotMapped]
        public string SONumber => SalesOrder?.SONumber ?? string.Empty;

        /// <summary>
        /// Display text untuk Customer Name
        /// </summary>
        [NotMapped]
        public string CustomerName => SalesOrder?.CustomerName ?? string.Empty;

        /// <summary>
        /// Summary untuk dashboard/list
        /// </summary>
        [NotMapped]
        public string Summary => $"{PickingNumber} - {SONumber} - {CustomerName} ({CompletionPercentage:F1}%)";
    }
}
