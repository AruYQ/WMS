// ViewModel untuk Supplier views

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk Create/Edit Supplier
    /// </summary>
    public class SupplierViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama supplier wajib diisi")]
        [MaxLength(100, ErrorMessage = "Nama supplier maksimal 100 karakter")]
        [Display(Name = "Nama Supplier")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email supplier wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [MaxLength(100, ErrorMessage = "Email maksimal 100 karakter")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [MaxLength(20, ErrorMessage = "Nomor telepon maksimal 20 karakter")]
        [Display(Name = "Nomor Telepon")]
        public string? Phone { get; set; }

        [MaxLength(200, ErrorMessage = "Alamat maksimal 200 karakter")]
        [Display(Name = "Alamat")]
        public string? Address { get; set; }

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        // Display properties
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }

        // Statistics
        public int PurchaseOrderCount { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalOrderValue { get; set; }
        public DateTime? LastOrderDate { get; set; }

        // Related data
        public List<PurchaseOrderSummary> PurchaseOrders { get; set; } = new List<PurchaseOrderSummary>();
        public List<ItemSummary> Items { get; set; } = new List<ItemSummary>();
    }

    /// <summary>
    /// Summary untuk Purchase Order di supplier details
    /// </summary>
    public class PurchaseOrderSummary
    {
        public int Id { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public bool EmailSent { get; set; }
        public DateTime? EmailSentDate { get; set; }

        public string StatusIndonesia
        {
            get
            {
                return Status switch
                {
                    "Draft" => "Draft",
                    "Sent" => "Terkirim",
                    "Closed" => "Selesai",
                    "Received" => "Diterima",
                    "Cancelled" => "Dibatalkan",
                    _ => Status
                };
            }
        }

        public string StatusCssClass
        {
            get
            {
                return Status switch
                {
                    "Draft" => "badge bg-secondary",
                    "Sent" => "badge bg-primary",
                    "Closed" => "badge bg-success",
                    "Received" => "badge bg-info",
                    "Cancelled" => "badge bg-danger",
                    _ => "badge bg-secondary"
                };
            }
        }

        public string TotalAmountDisplay => TotalAmount.ToString("C");
        public string OrderDateDisplay => OrderDate.ToString("dd/MM/yyyy");
        public string EmailSentDateDisplay => EmailSentDate?.ToString("dd/MM/yyyy HH:mm") ?? "-";
    }

    /// <summary>
    /// Summary untuk Item di supplier details
    /// </summary>
    public class ItemSummary
    {
        public int Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal StandardPrice { get; set; }
        public bool IsActive { get; set; }
        public int TotalStock { get; set; }
        public decimal TotalValue { get; set; }

        public string DisplayName => $"{ItemCode} - {Name}";
        public string StandardPriceDisplay => StandardPrice.ToString("C");
        public string TotalValueDisplay => TotalValue.ToString("C");
        public string StatusCssClass => IsActive ? "badge bg-success" : "badge bg-secondary";
        public string StatusText => IsActive ? "Aktif" : "Tidak Aktif";
    }

    /// <summary>
    /// ViewModel untuk Index page dengan search dan filter
    /// </summary>
    public class SupplierIndexViewModel
    {
        public List<SupplierViewModel> Suppliers { get; set; } = new List<SupplierViewModel>();
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public int TotalSuppliers { get; set; }
        public int ActiveSuppliers { get; set; }
        public int SuppliersWithOrders { get; set; }
        public int InactiveSuppliers { get; set; }
    }
}