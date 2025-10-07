// ViewModel untuk Customer views

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk Create/Edit Customer
    /// </summary>
    public class CustomerViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama customer wajib diisi")]
        [MaxLength(100, ErrorMessage = "Nama customer maksimal 100 karakter")]
        [Display(Name = "Nama Customer")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email customer wajib diisi")]
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
        public int SalesOrderCount { get; set; }
        public decimal TotalOrderValue { get; set; }
        public DateTime? LastOrderDate { get; set; }

        // Related data
        public List<SalesOrderSummary> SalesOrders { get; set; } = new List<SalesOrderSummary>();
    }

    /// <summary>
    /// Summary untuk Sales Order di customer details
    /// </summary>
    public class SalesOrderSummary
    {
        public int Id { get; set; }
        public string SONumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? RequiredDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public int TotalQuantity { get; set; }
        public int TotalItemTypes { get; set; }

        public string StatusIndonesia
        {
            get
            {
                return Status switch
                {
                    "Draft" => "Draft",
                    "Confirmed" => "Dikonfirmasi",
                    "Shipped" => "Dikirim",
                    "Completed" => "Selesai",
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
                    "Confirmed" => "badge bg-primary",
                    "Shipped" => "badge bg-info",
                    "Completed" => "badge bg-success",
                    "Cancelled" => "badge bg-danger",
                    _ => "badge bg-secondary"
                };
            }
        }

        public string TotalAmountDisplay => TotalAmount.ToString("C");
        public string GrandTotalDisplay => GrandTotal.ToString("C");
        public string OrderDateDisplay => OrderDate.ToString("dd/MM/yyyy");
        public string RequiredDateDisplay => RequiredDate?.ToString("dd/MM/yyyy") ?? "-";
    }

    /// <summary>
    /// ViewModel untuk Index page dengan search dan filter
    /// </summary>
    public class CustomerIndexViewModel
    {
        public List<CustomerViewModel> Customers { get; set; } = new List<CustomerViewModel>();
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int CustomersWithOrders { get; set; }
        public int InactiveCustomers { get; set; }
    }

    /// <summary>
    /// ViewModel untuk Customer Details page
    /// </summary>
    public class CustomerDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }
        public List<SalesOrderSummary> SalesOrders { get; set; } = new List<SalesOrderSummary>();
        public int SalesOrderCount { get; set; }
        public decimal TotalOrderValue { get; set; }
        public DateTime? LastOrderDate { get; set; }

        public string TotalOrderValueDisplay => TotalOrderValue.ToString("C");
        public string LastOrderDateDisplay => LastOrderDate?.ToString("dd/MM/yyyy") ?? "-";
        public string StatusText => IsActive ? "Aktif" : "Tidak Aktif";
        public string StatusCssClass => IsActive ? "badge bg-success" : "badge bg-secondary";
    }

    /// <summary>
    /// ViewModel untuk Customer Summary/Statistics
    /// </summary>
    public class CustomerSummaryViewModel
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }
        public int CustomersWithOrders { get; set; }
        public decimal TotalOrderValue { get; set; }
        public decimal AverageOrderValue { get; set; }

        public string TotalOrderValueDisplay => TotalOrderValue.ToString("C");
        public string AverageOrderValueDisplay => AverageOrderValue.ToString("C");
    }

    /// <summary>
    /// ViewModel untuk Customer Performance Report
    /// </summary>
    public class CustomerPerformanceViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public DateTime? FirstOrderDate { get; set; }
        public int DaysSinceLastOrder { get; set; }
        public bool IsActive { get; set; }

        public string TotalValueDisplay => TotalValue.ToString("C");
        public string AverageOrderValueDisplay => AverageOrderValue.ToString("C");
        public string LastOrderDateDisplay => LastOrderDate?.ToString("dd/MM/yyyy") ?? "-";
        public string FirstOrderDateDisplay => FirstOrderDate?.ToString("dd/MM/yyyy") ?? "-";
        public string StatusText => IsActive ? "Aktif" : "Tidak Aktif";
        public string StatusCssClass => IsActive ? "badge bg-success" : "badge bg-secondary";
    }

    /// <summary>
    /// ViewModel untuk Customer Performance Report dengan List
    /// </summary>
    public class CustomerPerformanceReportViewModel
    {
        public List<CustomerPerformanceViewModel> Customers { get; set; } = new List<CustomerPerformanceViewModel>();
        public int TotalCustomers { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageRevenuePerCustomer { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }

        public string TotalRevenueDisplay => TotalRevenue.ToString("C");
        public string AverageRevenuePerCustomerDisplay => AverageRevenuePerCustomer.ToString("C");
        public string AverageOrderValueDisplay => AverageOrderValue.ToString("C");
    }

    /// <summary>
    /// ViewModel untuk Customer Export
    /// </summary>
    public class CustomerExportViewModel
    {
        public List<CustomerViewModel> Customers { get; set; } = new List<CustomerViewModel>();
        public DateTime ExportDate { get; set; } = DateTime.Now;
        public string ExportFormat { get; set; } = "Excel";
        public int TotalRecords { get; set; }
        public string CompanyName { get; set; } = string.Empty;
    }
}
