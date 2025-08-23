using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk edit user form
    /// </summary>
    public class EditUserViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Username wajib diisi")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username harus antara 3-50 karakter")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username hanya boleh mengandung huruf, angka, dan underscore")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [StringLength(100, ErrorMessage = "Email maksimal 100 karakter")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Nama lengkap harus antara 2-100 karakter")]
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

        /// <summary>
        /// Available roles untuk assignment
        /// </summary>
        [Display(Name = "Roles")]
        public List<RoleSelectionViewModel> AvailableRoles { get; set; } = new List<RoleSelectionViewModel>();

        /// <summary>
        /// Selected role IDs
        /// </summary>
        public List<int> SelectedRoleIds { get; set; } = new List<int>();

        /// <summary>
        /// Current user roles untuk display
        /// </summary>
        public List<UserRoleViewModel> CurrentRoles { get; set; } = new List<UserRoleViewModel>();

        /// <summary>
        /// Audit information
        /// </summary>
        [Display(Name = "Tanggal Dibuat")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Dibuat Oleh")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Terakhir Login")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", NullDisplayText = "Belum pernah login")]
        public DateTime? LastLoginDate { get; set; }
    }
}