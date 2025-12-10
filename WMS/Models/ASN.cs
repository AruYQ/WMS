// Models/AdvancedShippingNotice.cs
// Model untuk Advanced Shipping Notice (ASN)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk Advanced Shipping Notice (ASN)
    /// "The Plot Twist" - notifikasi pengiriman dari supplier
    /// Berisi informasi actual price dan warehouse fee calculation
    /// </summary>
    public class AdvancedShippingNotice : BaseEntity
    {
        /// <summary>
        /// Nomor ASN yang unik (auto-generated)
        /// Format: ASN-YYYY-MM-DD-XXX
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "Nomor ASN")]
        public string ASNNumber { get; set; } = string.Empty;

        /// <summary>
        /// ID Purchase Order yang terkait dengan ASN ini
        /// </summary>
        [Required(ErrorMessage = "Purchase Order wajib dipilih")]
        [Display(Name = "Purchase Order")]
        public int PurchaseOrderId { get; set; }

        /// <summary>
        /// Tanggal barang dikirim dari supplier
        /// </summary>
        [Required(ErrorMessage = "Tanggal pengiriman wajib diisi")]
        [Display(Name = "Tanggal Pengiriman")]
        [DataType(DataType.Date)]
        public DateTime ShipmentDate { get; set; } = DateTime.Today;

        /// <summary>
        /// Tanggal diperkirakan sampai di gudang
        /// </summary>
        [Display(Name = "Perkiraan Sampai")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedArrivalDate { get; set; }

        /// <summary>
        /// Tanggal actual barang sampai di gudang
        /// Otomatis diisi saat status berubah ke "Arrived"
        /// </summary>
        [Display(Name = "Tanggal Actual Sampai")]
        [DataType(DataType.DateTime)]
        public DateTime? ActualArrivalDate { get; set; }

        /// <summary>
        /// Status ASN (Pending, On Delivery, Arrived, Processed)
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Nama perusahaan pengiriman/kurir
        /// </summary>
        [MaxLength(100)]
        [Display(Name = "Nama Kurir")]
        public string? CarrierName { get; set; }

        /// <summary>
        /// Nomor tracking untuk pelacakan
        /// </summary>
        [MaxLength(50)]
        [Display(Name = "Nomor Tracking")]
        public string? TrackingNumber { get; set; }

        /// <summary>
        /// Catatan tambahan untuk pengiriman
        /// </summary>
        [MaxLength(500)]
        [Display(Name = "Catatan")]
        public string? Notes { get; set; }

        /// <summary>
        /// ID lokasi sementara untuk menyimpan barang sebelum putaway
        /// </summary>
        [Required(ErrorMessage = "Holding location wajib dipilih")]
        [Display(Name = "Holding Location")]
        public int HoldingLocationId { get; set; }

        // Navigation Properties
        /// <summary>
        /// Purchase Order yang terkait dengan ASN ini
        /// </summary>
        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

        /// <summary>
        /// Detail item yang dikirim dalam ASN ini
        /// </summary>
        public virtual ICollection<ASNDetail> ASNDetails { get; set; } = new List<ASNDetail>();

        /// <summary>
        /// Lokasi sementara untuk menyimpan barang sebelum putaway
        /// </summary>
        public virtual Location HoldingLocation { get; set; } = null!;

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
                    "On Delivery" => "Sedang di Jalan",
                    "Arrived" => "Sudah Sampai",
                    "Processed" => "Sudah Diproses",
                    "Put Away" => "Sebagian Tersimpan",
                    "Completed" => "Lengkap Tersimpan",
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
                    "On Delivery" => "badge bg-info",
                    "Arrived" => "badge bg-success",
                    "Processed" => "badge bg-primary",
                    "Put Away" => "badge bg-warning text-dark",
                    "Completed" => "badge bg-success",
                    "Cancelled" => "badge bg-danger",
                    _ => "badge bg-light"
                };
            }
        }

        /// <summary>
        /// Jumlah total item yang dikirim
        /// </summary>
        [NotMapped]
        public int TotalShippedQuantity => ASNDetails?.Sum(d => d.ShippedQuantity) ?? 0;


        /// <summary>
        /// Jumlah jenis item yang berbeda
        /// </summary>
        [NotMapped]
        public int TotalItemTypes => ASNDetails?.Count ?? 0;

        /// <summary>
        /// Apakah ASN bisa diproses (status Arrived)
        /// </summary>
        [NotMapped]
        public bool CanBeProcessed => Status == "Arrived";

        /// <summary>
        /// Apakah ASN bisa diedit (status Pending)
        /// </summary>
        [NotMapped]
        public bool CanBeEdited => Status == "Pending";

        /// <summary>
        /// Nomor PO terkait untuk tampilan
        /// </summary>
        [NotMapped]
        public string PONumberDisplay => PurchaseOrder?.PONumber ?? string.Empty;

        /// <summary>
        /// Nama supplier dari PO terkait
        /// </summary>
        [NotMapped]
        public string SupplierName => PurchaseOrder?.Supplier?.Name ?? string.Empty;

        /// <summary>
        /// Durasi pengiriman (dari shipment date ke actual arrival date)
        /// </summary>
        [NotMapped]
        public TimeSpan? ShipmentDuration => ActualArrivalDate.HasValue ? ActualArrivalDate.Value - ShipmentDate : null;

        /// <summary>
        /// Durasi pengiriman dalam hari
        /// </summary>
        [NotMapped]
        public int? ShipmentDurationDays => ShipmentDuration?.Days;

        /// <summary>
        /// Apakah pengiriman tepat waktu (actual <= expected)
        /// </summary>
        [NotMapped]
        public bool? IsOnTime => ActualArrivalDate.HasValue && ExpectedArrivalDate.HasValue
            ? ActualArrivalDate.Value <= ExpectedArrivalDate.Value
            : null;

        /// <summary>
        /// Delay dalam hari (jika terlambat)
        /// </summary>
        [NotMapped]
        public int? DelayDays
        {
            get
            {
                if (!ActualArrivalDate.HasValue || !ExpectedArrivalDate.HasValue) return null;
                var delay = (ActualArrivalDate.Value.Date - ExpectedArrivalDate.Value.Date).Days;
                return delay > 0 ? delay : 0;
            }
        }

        /// <summary>
        /// Display text untuk actual arrival date
        /// </summary>
        [NotMapped]
        public string ActualArrivalDateDisplay => ActualArrivalDate?.ToString("dd/MM/yyyy HH:mm") ?? "-";

        /// <summary>
        /// CSS class untuk on-time status
        /// </summary>
        [NotMapped]
        public string OnTimeStatusCssClass
        {
            get
            {
                if (!IsOnTime.HasValue) return "text-muted";
                return IsOnTime.Value ? "text-success" : "text-danger";
            }
        }

        /// <summary>
        /// Text untuk on-time status
        /// </summary>
        [NotMapped]
        public string OnTimeStatusText
        {
            get
            {
                if (!IsOnTime.HasValue) return "Belum Sampai";
                if (IsOnTime.Value) return DelayDays == 0 ? "Tepat Waktu" : "Lebih Cepat";
                return $"Terlambat {DelayDays} hari";
            }
        }
    }
}