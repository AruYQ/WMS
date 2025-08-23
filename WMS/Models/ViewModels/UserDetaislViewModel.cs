using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk user details page
    /// </summary>
    public class UserDetailsViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Nama Lengkap")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Nomor Telepon")]
        public string? Phone { get; set; }

        [Display(Name = "Company")]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; }

        [Display(Name = "Email Terverifikasi")]
        public bool EmailVerified { get; set; }

        [Display(Name = "Terakhir Login")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", NullDisplayText = "Belum pernah login")]
        public DateTime? LastLoginDate { get; set; }

        [Display(Name = "Tanggal Dibuat")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Dibuat Oleh")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Tanggal Dimodifikasi")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", NullDisplayText = "-")]
        public DateTime? ModifiedDate { get; set; }

        [Display(Name = "Dimodifikasi Oleh")]
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// User roles dengan detail
        /// </summary>
        [Display(Name = "Roles")]
        public List<UserRoleDetailViewModel> UserRoles { get; set; } = new List<UserRoleDetailViewModel>();

        /// <summary>
        /// Activity summary
        /// </summary>
        public UserActivitySummaryViewModel ActivitySummary { get; set; } = new UserActivitySummaryViewModel();

        /// <summary>
        /// Actions available untuk current user
        /// </summary>
        public UserActionsViewModel AvailableActions { get; set; } = new UserActionsViewModel();
    }

    /// <summary>
    /// Detailed user role information
    /// </summary>
    public class UserRoleDetailViewModel
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }
        public DateTime AssignedDate { get; set; }
        public string? AssignedBy { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
        public bool IsActive { get; set; }

        /// <summary>
        /// Permissions as comma-separated string
        /// </summary>
        public string PermissionsDisplay => string.Join(", ", Permissions);
    }

    /// <summary>
    /// User activity summary
    /// </summary>
    public class UserActivitySummaryViewModel
    {
        public int TotalLogins { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public int DaysSinceLastLogin => LastLoginDate.HasValue
            ? (DateTime.Now - LastLoginDate.Value).Days
            : int.MaxValue;
        public bool IsRecentlyActive => DaysSinceLastLogin <= 7;

        // Could add more activity metrics here
        // public int TotalActions { get; set; }
        // public DateTime? LastActionDate { get; set; }
    }

    /// <summary>
    /// Available actions untuk user
    /// </summary>
    public class UserActionsViewModel
    {
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanResetPassword { get; set; }
        public bool CanManageRoles { get; set; }
        public bool CanActivateDeactivate { get; set; }
        public bool CanViewAuditLog { get; set; }
        public bool CanUnlock { get; set; }

        /// <summary>
        /// Is this the current user viewing their own profile?
        /// </summary>
        public bool IsCurrentUser { get; set; }

        /// <summary>
        /// Is the target user an admin?
        /// </summary>
        public bool IsTargetUserAdmin { get; set; }

        /// <summary>
        /// Is target user the last admin in company?
        /// </summary>
        public bool IsLastAdminInCompany { get; set; }
    }
}