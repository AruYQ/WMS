using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    public class CompanyRegistrationViewModel
    {
        // Company Info
        [Required(ErrorMessage = "Nama perusahaan wajib diisi")]
        [MaxLength(100, ErrorMessage = "Nama perusahaan maksimal 100 karakter")]
        [Display(Name = "Nama Perusahaan")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kode perusahaan wajib diisi")]
        [MaxLength(20, ErrorMessage = "Kode perusahaan maksimal 20 karakter")]
        [Display(Name = "Kode Perusahaan")]
        public string CompanyCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email perusahaan wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [Display(Name = "Email Perusahaan")]
        public string CompanyEmail { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [Display(Name = "Nomor Telepon Perusahaan")]
        public string? CompanyPhone { get; set; }

        [MaxLength(300, ErrorMessage = "Alamat maksimal 300 karakter")]
        [Display(Name = "Alamat Perusahaan")]
        public string? CompanyAddress { get; set; }

        [MaxLength(100, ErrorMessage = "Nama kontak maksimal 100 karakter")]
        [Display(Name = "Kontak Person")]
        public string? ContactPerson { get; set; }

        // Admin User Info
        [Required(ErrorMessage = "Username admin wajib diisi")]
        [MaxLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        [Display(Name = "Username Admin")]
        public string AdminUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email admin wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [Display(Name = "Email Admin")]
        public string AdminEmail { get; set; } = string.Empty;

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

        [Required(ErrorMessage = "Nama lengkap admin wajib diisi")]
        [MaxLength(100, ErrorMessage = "Nama maksimal 100 karakter")]
        [Display(Name = "Nama Lengkap Admin")]
        public string AdminFullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [Display(Name = "Nomor Telepon Admin")]
        public string? AdminPhone { get; set; }
    }
}
