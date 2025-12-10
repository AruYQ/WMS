// Models/SalesOrder.cs
// Model untuk Sales Order (SO)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk Sales Order - dokumen penjualan ke customer
    /// "The Climax" - dimana warehouse fee diterapkan dan stok dikurangi
    /// </summary>
    public class SalesOrder : BaseEntity
    {
        /// <summary>
        /// Nomor SO yang unik (auto-generated)
        /// Format: SO-YYYY-MM-DD-XXX
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "Nomor SO")]
        public string SONumber { get; set; } = string.Empty;

        /// <summary>
        /// ID Customer yang memesan
        /// </summary>
        [Required(ErrorMessage = "Customer wajib dipilih")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        /// <summary>
        /// Tanggal pembuatan SO
        /// </summary>
        [Required]
        [Display(Name = "Tanggal Order")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        /// <summary>
        /// Tanggal yang diminta customer
        /// </summary>
        [Display(Name = "Tanggal Dibutuhkan")]
        [DataType(DataType.Date)]
        public DateTime? RequiredDate { get; set; }

        /// <summary>
        /// Status SO (Pending, In Progress, Picked, Shipped, Completed, Cancelled)
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// ID Holding Location untuk items yang sudah di-pick
        /// </summary>
        [Display(Name = "Holding Location")]
        public int? HoldingLocationId { get; set; }

        /// <summary>
        /// Nama Holding Location untuk display
        /// </summary>
        [NotMapped]
        public string? HoldingLocationName { get; set; }

        /// <summary>
        /// Total nilai jual (sebelum warehouse fee)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Amount")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal TotalAmount { get; set; }


        /// <summary>
        /// Catatan tambahan untuk SO
        /// </summary>
        [MaxLength(500)]
        [Display(Name = "Catatan")]
        public string? Notes { get; set; }

        // Navigation Properties
        /// <summary>
        /// Data customer yang memesan
        /// </summary>
        public virtual Customer Customer { get; set; } = null!;

        /// <summary>
        /// Holding Location untuk items yang sudah di-pick
        /// </summary>
        public virtual Location? HoldingLocation { get; set; }

        /// <summary>
        /// Daftar item yang dijual dalam SO ini
        /// </summary>
        public virtual ICollection<SalesOrderDetail> SalesOrderDetails { get; set; } = new List<SalesOrderDetail>();

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
                    "In Progress" => "Sedang Diproses",
                    "Picked" => "Sudah Dipick",
                    "Shipped" => "Dikirim",  // FINAL STATUS
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
                    "Pending" => "badge bg-warning",
                    "In Progress" => "badge bg-info",
                    "Picked" => "badge bg-primary",
                    "Shipped" => "badge bg-success",  // FINAL STATUS
                    "Cancelled" => "badge bg-danger",
                    _ => "badge bg-light"
                };
            }
        }

        /// <summary>
        /// Total keseluruhan (Amount)
        /// Ini yang akan dibayar customer
        /// </summary>
        [NotMapped]
        public decimal GrandTotal => TotalAmount;

        /// <summary>
        /// Jumlah total item yang dijual
        /// </summary>
        [NotMapped]
        public int TotalQuantity => SalesOrderDetails?.Sum(d => d.Quantity) ?? 0;

        /// <summary>
        /// Jumlah jenis item yang berbeda
        /// </summary>
        [NotMapped]
        public int TotalItemTypes => SalesOrderDetails?.Count ?? 0;


        /// <summary>
        /// Apakah SO bisa diedit (hanya yang statusnya Pending)
        /// </summary>
        [NotMapped]
        public bool CanBeEdited => Status == "Pending";

        /// <summary>
        /// Apakah SO bisa dikonfirmasi (Pending dengan detail yang ada)
        /// </summary>
        [NotMapped]
        public bool CanBeConfirmed => Status == "Pending" && SalesOrderDetails.Any();

        /// <summary>
        /// Apakah SO bisa dikirim (status Picked)
        /// </summary>
        [NotMapped]
        public bool CanBeShipped => Status == "Picked";


        /// <summary>
        /// Apakah SO bisa dibatalkan
        /// </summary>
        [NotMapped]
        public bool CanBeCancelled => Status == "Pending" || Status == "In Progress";

        /// <summary>
        /// Nama customer untuk tampilan
        /// </summary>
        [NotMapped]
        public string CustomerName => Customer?.Name ?? string.Empty;

        /// <summary>
        /// Ringkasan SO untuk dashboard
        /// </summary>
        [NotMapped]
        public string Summary => $"{SONumber} - {CustomerName} - {GrandTotal:C}";
    }
}