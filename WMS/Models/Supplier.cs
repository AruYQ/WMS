// Model untuk master data supplier/pemasok

using System.ComponentModel.DataAnnotations;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk data supplier/pemasok
    /// Digunakan untuk Purchase Order dan email notification
    /// </summary>
    public class Supplier : BaseEntity
    {
        /// <summary>
        /// Nama perusahaan supplier
        /// </summary>
        [Required(ErrorMessage = "Nama supplier wajib diisi")]
        [MaxLength(100, ErrorMessage = "Nama supplier maksimal 100 karakter")]
        [Display(Name = "Nama Supplier")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Email supplier untuk mengirim Purchase Order
        /// </summary>
        [Required(ErrorMessage = "Email supplier wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [MaxLength(100, ErrorMessage = "Email maksimal 100 karakter")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Nomor telepon supplier
        /// </summary>
        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [MaxLength(20, ErrorMessage = "Nomor telepon maksimal 20 karakter")]
        [Display(Name = "Nomor Telepon")]
        public string? Phone { get; set; }

        /// <summary>
        /// Alamat lengkap supplier
        /// </summary>
        [MaxLength(200, ErrorMessage = "Alamat maksimal 200 karakter")]
        [Display(Name = "Alamat")]
        public string? Address { get; set; }

        /// <summary>
        /// Status aktif supplier
        /// </summary>
        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        /// <summary>
        /// Daftar Purchase Order dari supplier ini
        /// </summary>
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }
}