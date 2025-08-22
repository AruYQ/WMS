// ViewModel untuk Sales Order

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk Create/Edit Sales Order
    /// </summary>
    public class SalesOrderViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Nomor SO")]
        public string SONumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer wajib dipilih")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Required]
        [Display(Name = "Tanggal Order")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        [Display(Name = "Tanggal Dibutuhkan")]
        [DataType(DataType.Date)]
        public DateTime? RequiredDate { get; set; }

        [Display(Name = "Catatan")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        // Data untuk dropdown
        public SelectList? Customers { get; set; }
        public SelectList? Items { get; set; }

        // Detail items
        public List<SalesOrderDetailViewModel> Details { get; set; } = new();

        // Display properties
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TotalWarehouseFee { get; set; }
        public decimal GrandTotal { get; set; }
        public string Status { get; set; } = string.Empty;

        // Stock validation results
        public List<string> StockWarnings { get; set; } = new();
    }

    /// <summary>
    /// ViewModel untuk Sales Order Detail dengan stock checking
    /// </summary>
    public class SalesOrderDetailViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Item wajib dipilih")]
        public int ItemId { get; set; }

        [Required(ErrorMessage = "Quantity wajib diisi")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity harus lebih dari 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Unit Price wajib diisi")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit Price harus lebih dari 0")]
        [Display(Name = "Harga per Unit")]
        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }
        public decimal WarehouseFeeApplied { get; set; }

        [MaxLength(200)]
        public string? Notes { get; set; }

        // Display properties
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemUnit { get; set; } = string.Empty;
        public int AvailableStock { get; set; }
        public bool IsStockSufficient => AvailableStock >= Quantity;
        public decimal WarehouseFeePerUnit { get; set; }
    }
}