using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Request model untuk create new company (SuperAdmin only)
    /// Auto-creates admin user for the company
    /// </summary>
    public class CompanyCreateRequest
    {
        [Required(ErrorMessage = "Company code wajib diisi")]
        [MaxLength(20)]
        [Display(Name = "Company Code")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Company name wajib diisi")]
        [MaxLength(100)]
        [Display(Name = "Company Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [MaxLength(100)]
        [Display(Name = "Company Email")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [MaxLength(20)]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [MaxLength(300)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [MaxLength(100)]
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [MaxLength(20)]
        [Display(Name = "Tax Number")]
        public string? TaxNumber { get; set; }

        // Admin user info (will be auto-created)
        [Required(ErrorMessage = "Admin full name wajib diisi")]
        [MaxLength(100)]
        [Display(Name = "Admin Full Name")]
        public string AdminFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Admin email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [MaxLength(100)]
        [Display(Name = "Admin Email")]
        public string AdminEmail { get; set; } = string.Empty;

        [Phone]
        [MaxLength(20)]
        [Display(Name = "Admin Phone")]
        public string? AdminPhone { get; set; }
    }

    /// <summary>
    /// Request model untuk update company
    /// </summary>
    public class CompanyUpdateRequest
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Company name wajib diisi")]
        [MaxLength(100)]
        [Display(Name = "Company Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [MaxLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [MaxLength(20)]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [MaxLength(300)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [MaxLength(100)]
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [MaxLength(20)]
        [Display(Name = "Tax Number")]
        public string? TaxNumber { get; set; }

        [Display(Name = "Max Users")]
        public int MaxUsers { get; set; } = 5;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO untuk company list (API response)
    /// </summary>
    public class CompanyDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? ContactPerson { get; set; }
        public bool IsActive { get; set; }
        public int TotalUsers { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model untuk company details
    /// </summary>
    public class CompanyDetailResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? ContactPerson { get; set; }
        public string? TaxNumber { get; set; }
        public string SubscriptionPlan { get; set; } = "Free";
        public DateTime? SubscriptionEndDate { get; set; }
        public int MaxUsers { get; set; }
        public int CurrentUsers { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        // Statistics
        public int TotalItems { get; set; }
        public int TotalLocations { get; set; }
        public int TotalPurchaseOrders { get; set; }
        public int TotalSalesOrders { get; set; }
    }

    /// <summary>
    /// Result model untuk company creation operation
    /// </summary>
    public class CompanyCreationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public CompanyViewModel? Company { get; set; }
    }

    /// <summary>
    /// ViewModel untuk company (simple version)
    /// </summary>
    public class CompanyViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

