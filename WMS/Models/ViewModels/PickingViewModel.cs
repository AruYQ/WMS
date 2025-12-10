using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
#if false // Temporarily disabled: legacy Razor view models superseded by API DTOs.
    /// <summary>
    /// ViewModel untuk Picking operations
    /// </summary>
    public class PickingViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Picking Number")]
        public string PickingNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sales Order wajib dipilih")]
        [Display(Name = "Sales Order")]
        public int SalesOrderId { get; set; }

        [Display(Name = "SO Number")]
        public string SONumber { get; set; } = string.Empty;

        [Display(Name = "Customer")]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Picking Date")]
        [DataType(DataType.Date)]
        public DateTime PickingDate { get; set; } = DateTime.Today;

        [Display(Name = "Completed Date")]
        [DataType(DataType.DateTime)]
        public DateTime? CompletedDate { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Status")]
        public string StatusIndonesia { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        // Summary properties
        [Display(Name = "Total Required")]
        public int TotalQuantityRequired { get; set; }

        [Display(Name = "Total Picked")]
        public int TotalQuantityPicked { get; set; }

        [Display(Name = "Total Remaining")]
        public int TotalQuantityRemaining { get; set; }

        [Display(Name = "Completion %")]
        public decimal CompletionPercentage { get; set; }

        [Display(Name = "Item Types")]
        public int TotalItemTypes { get; set; }

        [Display(Name = "Locations Used")]
        public int TotalLocationsUsed { get; set; }

        public bool IsFullyPicked { get; set; }
        public bool HasShortItems { get; set; }
        public bool CanBeEdited { get; set; }
        public bool CanBeCompleted { get; set; }
        public bool CanBeCancelled { get; set; }

        // Details collection
        public List<PickingDetailViewModel> PickingDetails { get; set; } = new();

        // Dropdowns for form
        public SelectList? SalesOrders { get; set; }
        public SelectList? Locations { get; set; }
    }

    /// <summary>
    /// ViewModel untuk Picking Detail
    /// </summary>
    public class PickingDetailViewModel
    {
        public int Id { get; set; }

        [Required]
        public int PickingId { get; set; }

        [Required]
        public int SalesOrderDetailId { get; set; }

        [Required(ErrorMessage = "Item wajib dipilih")]
        [Display(Name = "Item")]
        public int ItemId { get; set; }

        [Display(Name = "Item Code")]
        public string ItemCode { get; set; } = string.Empty;

        [Display(Name = "Item Name")]
        public string ItemName { get; set; } = string.Empty;

        [Display(Name = "Unit")]
        public string ItemUnit { get; set; } = "PCS";

        [Display(Name = "Location")]
        public int? LocationId { get; set; }

        [Display(Name = "Location Code")]
        public string LocationCode { get; set; } = string.Empty;

        [Display(Name = "Location Name")]
        public string LocationName { get; set; } = string.Empty;

        [Display(Name = "Source Location")]
        public int? SourceLocationId { get; set; }

        [Display(Name = "Holding Location")]
        public int? HoldingLocationId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity harus lebih dari 0")]
        [Display(Name = "Quantity Required")]
        public int QuantityRequired { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity tidak boleh negatif")]
        [Display(Name = "Quantity to Pick")]
        public int QuantityToPick { get; set; }

        [Display(Name = "Quantity Picked")]
        public int QuantityPicked { get; set; }

        [Display(Name = "Remaining")]
        public int RemainingQuantity { get; set; }

        [Display(Name = "Available in Location")]
        public int AvailableQuantity { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Status")]
        public string StatusIndonesia { get; set; } = string.Empty;

        [MaxLength(200)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        public decimal PickedPercentage { get; set; }
        public bool IsFullyPicked { get; set; }
        public bool IsPartialPicked { get; set; }
    }

    /// <summary>
    /// ViewModel untuk Location Suggestion saat picking
    /// </summary>
    public class LocationSuggestion
    {
        public int LocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationDisplay { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsSuggested { get; set; }
        public int SuggestionOrder { get; set; } // 1 = first suggestion (FIFO)
    }

    /// <summary>
    /// ViewModel untuk Picking List summary (Index page)
    /// </summary>
    public class PickingListViewModel
    {
        public int Id { get; set; }
        public string PickingNumber { get; set; } = string.Empty;
        public string SONumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime PickingDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusIndonesia { get; set; } = string.Empty;
        public string StatusCssClass { get; set; } = string.Empty;
        public decimal CompletionPercentage { get; set; }
        public int TotalQuantityRequired { get; set; }
        public int TotalQuantityPicked { get; set; }
        public int TotalItemTypes { get; set; }
    }
#endif
}
