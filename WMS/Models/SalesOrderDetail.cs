// Models/SalesOrderDetail.cs
// Model untuk detail item dalam Sales Order

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk detail item dalam Sales Order
    /// Setiap baris item dalam SO akan menjadi satu record SalesOrderDetail
    /// Warehouse fee diterapkan di level ini
    /// </summary>
    public class SalesOrderDetail : BaseEntity
    {
        /// <summary>
        /// ID Sales Order yang memiliki detail ini
        /// </summary>
        [Required]
        [Display(Name = "Sales Order")]
        public int SalesOrderId { get; set; }

        /// <summary>
        /// ID Item yang dijual
        /// </summary>
        [Required(ErrorMessage = "Item wajib dipilih")]
        [Display(Name = "Item")]
        public int ItemId { get; set; }

        /// <summary>
        /// Jumlah yang dijual
        /// </summary>
        [Required(ErrorMessage = "Quantity wajib diisi")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity harus lebih dari 0")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        /// <summary>
        /// Harga jual per unit
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
        /// Sales Order yang memiliki detail ini
        /// </summary>
        public virtual SalesOrder SalesOrder { get; set; } = null!;

        /// <summary>
        /// Item yang dijual
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
        /// Format: "ItemCode - ItemName (Qty x UnitPrice = Total) + Fee"
        /// </summary>
        [NotMapped]
        public string FullDescription => $"{ItemDisplay} ({Quantity} {ItemUnit} × {UnitPrice:C} = {TotalPrice:C})";

        /// <summary>
        /// Profit margin jika diketahui cost price
        /// </summary>
        [NotMapped]
        public decimal ProfitMargin
        {
            get
            {
                var standardPrice = Item?.StandardPrice ?? 0;
                if (standardPrice == 0) return 0;
                return ((UnitPrice - standardPrice) / UnitPrice) * 100;
            }
        }
    }
}