// Models/Item.cs
// Model untuk master data item/barang

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk data item/barang yang disimpan di gudang
    /// </summary>
    public class Item : BaseEntity
    {
        /// <summary>
        /// Kode unik item (seperti SKU)
        /// </summary>
        [Required(ErrorMessage = "Kode item wajib diisi")]
        [MaxLength(50, ErrorMessage = "Kode item maksimal 50 karakter")]
        [Display(Name = "Kode Item")]
        public string ItemCode { get; set; } = string.Empty;

        /// <summary>
        /// Nama item/barang
        /// </summary>
        [Required(ErrorMessage = "Nama item wajib diisi")]
        [MaxLength(200, ErrorMessage = "Nama item maksimal 200 karakter")]
        [Display(Name = "Nama Item")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Deskripsi detail item
        /// </summary>
        [MaxLength(500, ErrorMessage = "Deskripsi maksimal 500 karakter")]
        [Display(Name = "Deskripsi")]
        public string? Description { get; set; }

        /// <summary>
        /// Unit satuan (pcs, kg, liter, dll)
        /// </summary>
        [Required(ErrorMessage = "Unit satuan wajib diisi")]
        [MaxLength(10, ErrorMessage = "Unit maksimal 10 karakter")]
        [Display(Name = "Satuan")]
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Harga standar untuk referensi (tidak selalu digunakan untuk transaksi)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Harga harus lebih besar atau sama dengan 0")]
        [Display(Name = "Harga Standar")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal StandardPrice { get; set; }

        /// <summary>
        /// Status aktif item
        /// </summary>
        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        /// <summary>
        /// Detail Purchase Order yang berisi item ini
        /// </summary>
        public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();

        /// <summary>
        /// Detail ASN yang berisi item ini
        /// </summary>
        public virtual ICollection<ASNDetail> ASNDetails { get; set; } = new List<ASNDetail>();

        /// <summary>
        /// Detail Sales Order yang berisi item ini
        /// </summary>
        public virtual ICollection<SalesOrderDetail> SalesOrderDetails { get; set; } = new List<SalesOrderDetail>();

        /// <summary>
        /// Inventory record untuk item ini di berbagai lokasi
        /// </summary>
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

        /// <summary>
        /// Display name untuk dropdown dan tampilan lainnya
        /// </summary>
        [NotMapped]
        public string DisplayName => $"{ItemCode} - {Name}";

        /// <summary>
        /// Total stok dari semua lokasi
        /// </summary>
        [NotMapped]
        public int TotalStock => Inventories?.Sum(i => i.Quantity) ?? 0;
    }
}