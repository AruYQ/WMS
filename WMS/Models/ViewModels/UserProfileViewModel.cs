using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk user profile page
    /// </summary>
    public class UserProfileViewModel
    {
        [Required]
        public int Id { get; set; }

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

        [Display(Name = "Company")]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "Roles")]
        public List<string> RoleNames { get; set; } = new List<string>();

        [Display(Name = "Terakhir Login")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", NullDisplayText = "Belum pernah login")]
        public DateTime? LastLoginDate { get; set; }

        [Display(Name = "Member Sejak")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Role names sebagai string untuk display
        /// </summary>
        public string RoleNamesDisplay => string.Join(", ", RoleNames);
    }
}