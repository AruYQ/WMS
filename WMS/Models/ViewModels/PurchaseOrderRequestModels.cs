using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Request model for creating purchase order
    /// </summary>
    public class PurchaseOrderCreateRequest
    {
        [Required(ErrorMessage = "Supplier is required")]
        public int SupplierId { get; set; }

        public DateTime? OrderDate { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public string? Notes { get; set; }

        [Required(ErrorMessage = "At least one item is required")]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<PurchaseOrderDetailRequest> Details { get; set; } = new();
    }

    /// <summary>
    /// Request model for updating purchase order
    /// </summary>
    public class PurchaseOrderUpdateRequest
    {
        public int? SupplierId { get; set; }

        public DateTime? OrderDate { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public string? Notes { get; set; }

        public List<PurchaseOrderDetailRequest>? Details { get; set; }
    }

    /// <summary>
    /// Request model for purchase order detail
    /// </summary>
    public class PurchaseOrderDetailRequest
    {
        [Required(ErrorMessage = "Item is required")]
        public int ItemId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        public string? Notes { get; set; }
    }
}

