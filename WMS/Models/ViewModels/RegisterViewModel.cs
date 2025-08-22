using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username wajib diisi")]
        [MaxLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi")]
        [StringLength(100, ErrorMessage = "Password minimal {2} karakter", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password wajib diisi")]
        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password")]
        [Compare("Password", ErrorMessage = "Password dan konfirmasi password tidak sama")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        [MaxLength(100, ErrorMessage = "Nama maksimal 100 karakter")]
        [Display(Name = "Nama Lengkap")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [Display(Name = "Nomor Telepon")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Kode perusahaan wajib diisi")]
        [Display(Name = "Kode Perusahaan")]
        public string CompanyCode { get; set; } = string.Empty;
    }
}