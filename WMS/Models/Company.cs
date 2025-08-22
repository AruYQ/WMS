// Models/Company.cs
// Model untuk data company/perusahaan untuk multi-tenancy

using System.ComponentModel.DataAnnotations;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk data company/perusahaan
    /// Setiap company memiliki data yang terpisah (multi-tenant)
    /// </summary>
    public class Company : BaseEntityWithoutCompany
    {
        /// <summary>
        /// Nama perusahaan
        /// </summary>
        [Required(ErrorMessage = "Nama perusahaan wajib diisi")]
        [MaxLength(100, ErrorMessage = "Nama perusahaan maksimal 100 karakter")]
        [Display(Name = "Nama Perusahaan")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Kode perusahaan (unik)
        /// </summary>
        [Required(ErrorMessage = "Kode perusahaan wajib diisi")]
        [MaxLength(20, ErrorMessage = "Kode perusahaan maksimal 20 karakter")]
        [Display(Name = "Kode Perusahaan")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Email perusahaan
        /// </summary>
        [Required(ErrorMessage = "Email perusahaan wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [MaxLength(100, ErrorMessage = "Email maksimal 100 karakter")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Nomor telepon perusahaan
        /// </summary>
        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [MaxLength(20, ErrorMessage = "Nomor telepon maksimal 20 karakter")]
        [Display(Name = "Nomor Telepon")]
        public string? Phone { get; set; }

        /// <summary>
        /// Alamat lengkap perusahaan
        /// </summary>
        [MaxLength(300, ErrorMessage = "Alamat maksimal 300 karakter")]
        [Display(Name = "Alamat")]
        public string? Address { get; set; }

        /// <summary>
        /// Nama kontak person
        /// </summary>
        [MaxLength(100, ErrorMessage = "Nama kontak maksimal 100 karakter")]
        [Display(Name = "Kontak Person")]
        public string? ContactPerson { get; set; }

        /// <summary>
        /// NPWP perusahaan
        /// </summary>
        [MaxLength(20, ErrorMessage = "NPWP maksimal 20 karakter")]
        [Display(Name = "NPWP")]
        public string? TaxNumber { get; set; }

        /// <summary>
        /// Status aktif perusahaan
        /// </summary>
        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Tanggal berakhir subscription (untuk SaaS model)
        /// </summary>
        [Display(Name = "Tanggal Berakhir")]
        public DateTime? SubscriptionEndDate { get; set; }

        /// <summary>
        /// Plan subscription (Free, Basic, Premium)
        /// </summary>
        [MaxLength(20)]
        [Display(Name = "Plan Subscription")]
        public string SubscriptionPlan { get; set; } = "Free";

        /// <summary>
        /// Maksimal user untuk company ini
        /// </summary>
        [Display(Name = "Maksimal User")]
        public int MaxUsers { get; set; } = 5;

        // Navigation Properties
        /// <summary>
        /// Daftar user yang bekerja di company ini
        /// </summary>
        public virtual ICollection<User> Users { get; set; } = new List<User>();

        /// <summary>
        /// Daftar supplier milik company ini
        /// </summary>
        public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();

        /// <summary>
        /// Daftar customer milik company ini
        /// </summary>
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

        /// <summary>
        /// Daftar item milik company ini
        /// </summary>
        public virtual ICollection<Item> Items { get; set; } = new List<Item>();

        /// <summary>
        /// Daftar location milik company ini
        /// </summary>
        public virtual ICollection<Location> Locations { get; set; } = new List<Location>();

        /// <summary>
        /// Daftar purchase order milik company ini
        /// </summary>
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

        /// <summary>
        /// Daftar sales order milik company ini
        /// </summary>
        public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();

        /// <summary>
        /// Daftar ASN milik company ini
        /// </summary>
        public virtual ICollection<AdvancedShippingNotice> AdvancedShippingNotices { get; set; } = new List<AdvancedShippingNotice>();

        /// <summary>
        /// Daftar inventory milik company ini
        /// </summary>
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}