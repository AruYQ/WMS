using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using WMS.Models;
using WMS.Services;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk create user
    /// </summary>
    public class CreateUserViewModel
    {
        /// <summary>
        /// Username
        /// </summary>
        [Required(ErrorMessage = "Username wajib diisi")]
        [StringLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email
        /// </summary>
        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [StringLength(100, ErrorMessage = "Email maksimal 100 karakter")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Password
        /// </summary>
        [Required(ErrorMessage = "Password wajib diisi")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Konfirmasi password
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password")]
        [Compare("Password", ErrorMessage = "Konfirmasi password tidak cocok")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// Nama lengkap
        /// </summary>
        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        [StringLength(100, ErrorMessage = "Nama maksimal 100 karakter")]
        [Display(Name = "Nama Lengkap")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Nomor telepon
        /// </summary>
        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [StringLength(20, ErrorMessage = "Nomor telepon maksimal 20 karakter")]
        [Display(Name = "Nomor Telepon")]
        public string? Phone { get; set; }

        /// <summary>
        /// Selected roles
        /// </summary>
        [Display(Name = "Role")]
        public List<string> SelectedRoles { get; set; } = new List<string>();

        /// <summary>
        /// Available roles untuk selection
        /// </summary>
        public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();
    }

    /// <summary>
    /// ViewModel untuk edit user
    /// </summary>
    public class EditUserViewModel
    {
        /// <summary>
        /// User ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Username
        /// </summary>
        [Required(ErrorMessage = "Username wajib diisi")]
        [StringLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email
        /// </summary>
        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [StringLength(100, ErrorMessage = "Email maksimal 100 karakter")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Nama lengkap
        /// </summary>
        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        [StringLength(100, ErrorMessage = "Nama maksimal 100 karakter")]
        [Display(Name = "Nama Lengkap")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Nomor telepon
        /// </summary>
        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [StringLength(20, ErrorMessage = "Nomor telepon maksimal 20 karakter")]
        [Display(Name = "Nomor Telepon")]
        public string? Phone { get; set; }

        /// <summary>
        /// Status aktif
        /// </summary>
        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Selected roles
        /// </summary>
        [Display(Name = "Role")]
        public List<string> SelectedRoles { get; set; } = new List<string>();

        /// <summary>
        /// Available roles untuk selection
        /// </summary>
        public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();
    }

    /// <summary>
    /// ViewModel untuk user list
    /// </summary>
    public class UserListViewModel
    {
        /// <summary>
        /// List of users
        /// </summary>
        public List<User> Users { get; set; } = new List<User>();

        /// <summary>
        /// User statistics
        /// </summary>
        public UserStatistics Statistics { get; set; } = new UserStatistics();

        /// <summary>
        /// Search filter
        /// </summary>
        [Display(Name = "Pencarian")]
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Role filter
        /// </summary>
        [Display(Name = "Filter Role")]
        public string? RoleFilter { get; set; }

        /// <summary>
        /// Status filter
        /// </summary>
        [Display(Name = "Filter Status")]
        public bool? StatusFilter { get; set; }
    }

    /// <summary>
    /// ViewModel untuk user details
    /// </summary>
    public class UserDetailsViewModel
    {
        /// <summary>
        /// User data
        /// </summary>
        public User User { get; set; } = new User();

        /// <summary>
        /// User roles
        /// </summary>
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// Recent activities (for future implementation)
        /// </summary>
        public List<string> RecentActivities { get; set; } = new List<string>();
    }

    /// <summary>
    /// ViewModel untuk user profile
    /// </summary>
    public class UserProfileViewModel
    {
        /// <summary>
        /// User ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Username
        /// </summary>
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email
        /// </summary>
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Nama lengkap
        /// </summary>
        [Display(Name = "Nama Lengkap")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Nomor telepon
        /// </summary>
        [Display(Name = "Nomor Telepon")]
        public string? Phone { get; set; }

        /// <summary>
        /// Last login date
        /// </summary>
        [Display(Name = "Login Terakhir")]
        public DateTime? LastLoginDate { get; set; }

        /// <summary>
        /// Company name
        /// </summary>
        [Display(Name = "Perusahaan")]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// User roles
        /// </summary>
        [Display(Name = "Role")]
        public List<string> Roles { get; set; } = new List<string>();
    }

    /// <summary>
    /// ViewModel untuk reset user password (admin function)
    /// </summary>
    public class ResetUserPasswordViewModel
    {
        /// <summary>
        /// User ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Username (for display)
        /// </summary>
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Full name (for display)
        /// </summary>
        [Display(Name = "Nama Lengkap")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// New password
        /// </summary>
        [Required(ErrorMessage = "Password baru wajib diisi")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Baru")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Confirm new password
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password Baru")]
        [Compare("NewPassword", ErrorMessage = "Konfirmasi password tidak cocok")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}