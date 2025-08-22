// ViewModel untuk Purchase Order views

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk Create/Edit Purchase Order
    /// </summary>
    public class PurchaseOrderViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Nomor PO")]
        public string PONumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Supplier wajib dipilih")]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        [Required]
        [Display(Name = "Tanggal Order")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        [Display(Name = "Tanggal Diharapkan")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [Display(Name = "Catatan")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        // Data untuk dropdown
        public SelectList? Suppliers { get; set; }
        public SelectList? Items { get; set; }

        // Detail items
        public List<PurchaseOrderDetailViewModel> Details { get; set; } = new();

        // Display properties
        public string SupplierName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel untuk Purchase Order Detail
    /// </summary>
    public class PurchaseOrderDetailViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Item wajib dipilih")]
        public int ItemId { get; set; }

        [Required(ErrorMessage = "Quantity wajib diisi")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity harus lebih dari 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Unit Price wajib diisi")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit Price harus lebih dari 0")]
        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        [MaxLength(200)]
        public string? Notes { get; set; }

        // Display properties
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemUnit { get; set; } = string.Empty;
    }
}