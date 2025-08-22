// Models/PurchaseOrderDetail.cs
// Model untuk detail item dalam Purchase Order

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk detail item dalam Purchase Order
    /// Setiap baris item dalam PO akan menjadi satu record PurchaseOrderDetail
    /// </summary>
    public class PurchaseOrderDetail : BaseEntity
    {
        /// <summary>
        /// ID Purchase Order yang memiliki detail ini
        /// </summary>
        [Required]
        [Display(Name = "Purchase Order")]
        public int PurchaseOrderId { get; set; }

        /// <summary>
        /// ID Item yang dipesan
        /// </summary>
        [Required(ErrorMessage = "Item wajib dipilih")]
        [Display(Name = "Item")]
        public int ItemId { get; set; }

        /// <summary>
        /// Jumlah yang dipesan
        /// </summary>
        [Required(ErrorMessage = "Quantity wajib diisi")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity harus lebih dari 0")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        /// <summary>
        /// Harga per unit saat pemesanan
        /// </summary>
        [Required(ErrorMessage = "Unit Price wajib diisi")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit Price harus lebih dari 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Harga per Unit")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Total harga untuk item ini (Quantity × UnitPrice)
        /// Dihitung otomatis
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Harga")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Catatan khusus untuk item ini
        /// </summary>
        [MaxLength(200)]
        [Display(Name = "Catatan")]
        public string? Notes { get; set; }

        // Navigation Properties
        /// <summary>
        /// Purchase Order yang memiliki detail ini
        /// </summary>
        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

        /// <summary>
        /// Item yang dipesan
        /// </summary>
        public virtual Item Item { get; set; } = null!;

        // Methods
        /// <summary>
        /// Menghitung total price berdasarkan quantity dan unit price
        /// </summary>
        public void CalculateTotalPrice()
        {
            TotalPrice = Quantity * UnitPrice;
        }

        // Computed Properties
        /// <summary>
        /// Display text untuk item (kode + nama)
        /// </summary>
        [NotMapped]
        public string ItemDisplay => Item?.DisplayName ?? string.Empty;

        /// <summary>
        /// Unit satuan dari item
        /// </summary>
        [NotMapped]
        public string ItemUnit => Item?.Unit ?? string.Empty;

        /// <summary>
        /// Informasi lengkap untuk tampilan di grid
        /// Format: "ItemCode - ItemName (Qty x UnitPrice = Total)"
        /// </summary>
        [NotMapped]
        public string FullDescription => $"{ItemDisplay} ({Quantity} {ItemUnit} × {UnitPrice:C} = {TotalPrice:C})";
    }
}