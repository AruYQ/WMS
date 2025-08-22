using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password baru wajib diisi")]
        [StringLength(100, ErrorMessage = "Password minimal {2} karakter", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password Baru")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password wajib diisi")]
        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password")]
        [Compare("NewPassword", ErrorMessage = "Password dan konfirmasi password tidak sama")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}