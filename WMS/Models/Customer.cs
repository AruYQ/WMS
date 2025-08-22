// Models/Customer.cs
// Model untuk master data customer/pelanggan

using System.ComponentModel.DataAnnotations;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk data customer/pelanggan
    /// Digunakan untuk Sales Order
    /// </summary>
    public class Customer : BaseEntity
    {
        /// <summary>
        /// Nama perusahaan atau nama customer
        /// </summary>
        [Required(ErrorMessage = "Nama customer wajib diisi")]
        [MaxLength(100, ErrorMessage = "Nama customer maksimal 100 karakter")]
        [Display(Name = "Nama Customer")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Email customer untuk komunikasi dan invoice
        /// </summary>
        [Required(ErrorMessage = "Email customer wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [MaxLength(100, ErrorMessage = "Email maksimal 100 karakter")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Nomor telepon customer
        /// </summary>
        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [MaxLength(20, ErrorMessage = "Nomor telepon maksimal 20 karakter")]
        [Display(Name = "Nomor Telepon")]
        public string? Phone { get; set; }

        /// <summary>
        /// Alamat lengkap customer untuk pengiriman
        /// </summary>
        [MaxLength(200, ErrorMessage = "Alamat maksimal 200 karakter")]
        [Display(Name = "Alamat")]
        public string? Address { get; set; }

        /// <summary>
        /// Status aktif customer
        /// </summary>
        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        /// <summary>
        /// Daftar Sales Order dari customer ini
        /// </summary>
        public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    }
}