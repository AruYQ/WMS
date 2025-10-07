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
        /// Status SO (Draft, Confirmed, Shipped, Completed, Cancelled)
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Draft";

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
                    "Draft" => "Draft",
                    "Confirmed" => "Dikonfirmasi",
                    "Shipped" => "Dikirim",
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
                    "Draft" => "badge bg-secondary",
                    "Confirmed" => "badge bg-primary",
                    "Shipped" => "badge bg-info",
                    "Completed" => "badge bg-success",
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
        /// Apakah SO bisa diedit (hanya yang statusnya Draft)
        /// </summary>
        [NotMapped]
        public bool CanBeEdited => Status == "Draft";

        /// <summary>
        /// Apakah SO bisa dikonfirmasi (Draft dengan detail yang ada)
        /// </summary>
        [NotMapped]
        public bool CanBeConfirmed => Status == "Draft" && SalesOrderDetails.Any();

        /// <summary>
        /// Apakah SO bisa dikirim (status Confirmed)
        /// </summary>
        [NotMapped]
        public bool CanBeShipped => Status == "Confirmed";

        /// <summary>
        /// Apakah SO bisa diselesaikan (status Shipped)
        /// </summary>
        [NotMapped]
        public bool CanBeCompleted => Status == "Shipped";

        /// <summary>
        /// Apakah SO bisa dibatalkan
        /// </summary>
        [NotMapped]
        public bool CanBeCancelled => Status == "Draft" || Status == "Confirmed";

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