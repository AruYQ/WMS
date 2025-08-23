using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk user management
    /// </summary>
    public class UserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Username wajib diisi")]
        [StringLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [StringLength(100, ErrorMessage = "Email maksimal 100 karakter")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        [StringLength(100, ErrorMessage = "Nama lengkap maksimal 100 karakter")]
        [Display(Name = "Nama Lengkap")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [StringLength(20, ErrorMessage = "Nomor telepon maksimal 20 karakter")]
        [Display(Name = "Nomor Telepon")]
        public string? Phone { get; set; }

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

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
        /// Roles assigned to user
        /// </summary>
        [Display(Name = "Roles")]
        public List<UserRoleViewModel> UserRoles { get; set; } = new List<UserRoleViewModel>();

        /// <summary>
        /// Available roles for assignment (used in create/edit forms)
        /// </summary>
        public List<RoleViewModel> AvailableRoles { get; set; } = new List<RoleViewModel>();

        /// <summary>
        /// Selected role IDs for form binding
        /// </summary>
        public List<int> SelectedRoleIds { get; set; } = new List<int>();

        /// <summary>
        /// Role names as comma-separated string for display
        /// </summary>
        public string RoleNamesDisplay => string.Join(", ", UserRoles.Select(ur => ur.RoleName));
    }

    /// <summary>
    /// ViewModel untuk user role relationship
    /// </summary>
    public class UserRoleViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }
        public DateTime AssignedDate { get; set; }
        public string? AssignedBy { get; set; }
    }

    /// <summary>
    /// ViewModel untuk role selection
    /// </summary>
    public class RoleViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsSelected { get; set; }
    }
}