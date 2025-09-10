// ViewModel untuk Advanced Shipping Notice

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk Create/Edit ASN
    /// </summary>
    public class ASNViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Nomor ASN")]
        public string ASNNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Purchase Order wajib dipilih")]
        [Display(Name = "Purchase Order")]
        public int PurchaseOrderId { get; set; }

        [Required]
        [Display(Name = "Tanggal Pengiriman")]
        [DataType(DataType.Date)]
        public DateTime ShipmentDate { get; set; } = DateTime.Today;

        [Display(Name = "Perkiraan Sampai")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedArrivalDate { get; set; }

        [Display(Name = "Tanggal Actual Sampai")]
        [DataType(DataType.DateTime)]
        public DateTime? ActualArrivalDate { get; set; }

        [Display(Name = "Nama Kurir")]
        [MaxLength(100)]
        public string? CarrierName { get; set; }

        [Display(Name = "Nomor Tracking")]
        [MaxLength(50)]
        public string? TrackingNumber { get; set; }

        [Display(Name = "Catatan")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        // Data untuk dropdown
        public SelectList? PurchaseOrders { get; set; }
        public SelectList? Items { get; set; }

        // Detail items
        public List<ASNDetailViewModel> Details { get; set; } = new();

        // Display properties
        public string PONumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalWarehouseFee { get; set; }

        // PO Details for reference
        public List<PurchaseOrderDetailViewModel> PODetails { get; set; } = new();

        // New computed properties for arrival tracking
        public bool IsOnTime => ActualArrivalDate.HasValue && ExpectedArrivalDate.HasValue
            ? ActualArrivalDate.Value <= ExpectedArrivalDate.Value
            : false;

        public int? DelayDays
        {
            get
            {
                if (!ActualArrivalDate.HasValue || !ExpectedArrivalDate.HasValue) return null;
                var delay = (ActualArrivalDate.Value.Date - ExpectedArrivalDate.Value.Date).Days;
                return delay > 0 ? delay : 0;
            }
        }

        public string OnTimeStatusText
        {
            get
            {
                if (!ActualArrivalDate.HasValue) return "Belum Sampai";
                if (!ExpectedArrivalDate.HasValue) return "Sudah Sampai";

                if (IsOnTime)
                {
                    return DelayDays == 0 ? "Tepat Waktu" : "Lebih Cepat";
                }
                return $"Terlambat {DelayDays} hari";
            }
        }

        public string ActualArrivalDateDisplay => ActualArrivalDate?.ToString("dd/MM/yyyy HH:mm") ?? "-";
    }

    /// <summary>
    /// ViewModel untuk ASN Detail dengan warehouse fee calculation
    /// </summary>
    public class ASNDetailViewModel
    {
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }

        [Required(ErrorMessage = "Shipped Quantity wajib diisi")]
        [Range(1, int.MaxValue, ErrorMessage = "Shipped Quantity harus lebih dari 0")]
        [Display(Name = "Qty Dikirim")]
        public int ShippedQuantity { get; set; }

        [Display(Name = "Sisa Quantity")]
        public int RemainingQuantity { get; set; }

        [Display(Name = "Sudah Di-putaway")]
        public int AlreadyPutAwayQuantity { get; set; }

        [Required(ErrorMessage = "Actual Price wajib diisi")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Actual Price harus lebih dari 0")]
        [Display(Name = "Harga Actual")]
        public decimal ActualPricePerItem { get; set; }

        [Display(Name = "Warehouse Fee Rate")]
        public decimal WarehouseFeeRate { get; set; }

        [Display(Name = "Warehouse Fee Amount")]
        public decimal WarehouseFeeAmount { get; set; }

        [MaxLength(200)]
        public string? Notes { get; set; }

        // Display properties
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemUnit { get; set; } = string.Empty;
        public int OrderedQuantity { get; set; } // Dari PO
        public decimal OrderedPrice { get; set; } // Dari PO
        public string WarehouseFeeTier { get; set; } = string.Empty;
    }
}   