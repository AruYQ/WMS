// Models/PurchaseOrder.cs
// Model untuk Purchase Order (PO)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk Purchase Order - dokumen pemesanan ke supplier
    /// "The Opening Act" dalam sistem WMS
    /// </summary>
    public class PurchaseOrder : BaseEntity
    {
        /// <summary>
        /// Nomor PO yang unik (auto-generated)
        /// Format: PO-YYYY-MM-DD-XXX
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "Nomor PO")]
        public string PONumber { get; set; } = string.Empty;

        /// <summary>
        /// ID Supplier yang akan menerima PO ini
        /// </summary>
        [Required(ErrorMessage = "Supplier wajib dipilih")]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        /// <summary>
        /// Tanggal pembuatan PO
        /// </summary>
        [Required]
        [Display(Name = "Tanggal Order")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        /// <summary>
        /// Tanggal diharapkan barang sampai
        /// </summary>
        [Display(Name = "Tanggal Diharapkan")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedDeliveryDate { get; set; }

        /// <summary>
        /// Status PO (Draft, Sent, Received, Cancelled)
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Draft";

        /// <summary>
        /// Total nilai PO (dihitung dari detail)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Amount")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Catatan tambahan untuk PO
        /// </summary>
        [MaxLength(500)]
        [Display(Name = "Catatan")]
        public string? Notes { get; set; }

        /// <summary>
        /// Flag apakah email sudah dikirim ke supplier
        /// </summary>
        [Display(Name = "Email Terkirim")]
        public bool EmailSent { get; set; } = false;

        /// <summary>
        /// Tanggal email dikirim
        /// </summary>
        [Display(Name = "Tanggal Email Terkirim")]
        public DateTime? EmailSentDate { get; set; }

        // Navigation Properties
        /// <summary>
        /// Data supplier yang menerima PO
        /// </summary>
        public virtual Supplier Supplier { get; set; } = null!;

        /// <summary>
        /// Daftar item yang dipesan dalam PO ini
        /// </summary>
        public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();

        /// <summary>
        /// Daftar ASN yang terkait dengan PO ini
        /// </summary>
        public virtual ICollection<AdvancedShippingNotice> AdvancedShippingNotices { get; set; } = new List<AdvancedShippingNotice>();

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
                    "Sent" => "Terkirim",
                    "Closed" => "Selesai",
                    "Received" => "Diterima",
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
                    "Sent" => "badge bg-primary",
                    "Closed" => "badge bg-success",
                    "Received" => "badge bg-success",
                    "Cancelled" => "badge bg-danger",
                    _ => "badge bg-light"
                };
            }
        }

        /// <summary>
        /// Jumlah total item yang dipesan
        /// </summary>
        [NotMapped]
        public int TotalQuantity => PurchaseOrderDetails?.Sum(d => d.Quantity) ?? 0;

        /// <summary>
        /// Jumlah jenis item yang berbeda
        /// </summary>
        [NotMapped]
        public int TotalItemTypes => PurchaseOrderDetails?.Count ?? 0;

        /// <summary>
        /// Apakah PO bisa diedit (hanya yang statusnya Draft)
        /// </summary>
        [NotMapped]
        public bool CanBeEdited => Status == "Draft";

        /// <summary>
        /// Apakah PO bisa dikirim via email
        /// </summary>
        [NotMapped]
        public bool CanBeSent => Status == "Draft" && PurchaseOrderDetails.Any();

        /// <summary>
        /// Apakah PO bisa dibatalkan
        /// </summary>
        [NotMapped]
        public bool CanBeCancelled => Status == "Draft" || Status == "Sent";
    }
}