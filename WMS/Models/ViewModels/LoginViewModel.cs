using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk login page
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        /// Username atau email untuk login
        /// </summary>
        [Required(ErrorMessage = "Username atau email wajib diisi")]
        [Display(Name = "Username atau Email")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        /// <summary>
        /// Password untuk login
        /// </summary>
        [Required(ErrorMessage = "Password wajib diisi")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Remember me option
        /// </summary>
        [Display(Name = "Ingat saya")]
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// ViewModel untuk forgot password
    /// </summary>
    public class ForgotPasswordViewModel
    {
        /// <summary>
        /// Email untuk reset password
        /// </summary>
        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel untuk reset password
    /// </summary>
    public class ResetPasswordViewModel
    {
        /// <summary>
        /// Reset token
        /// </summary>
        [Required]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Password baru
        /// </summary>
        [Required(ErrorMessage = "Password baru wajib diisi")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Baru")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Konfirmasi password baru
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password Baru")]
        [Compare("NewPassword", ErrorMessage = "Konfirmasi password tidak cocok")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel untuk change password
    /// </summary>
    public class ChangePasswordViewModel
    {
        /// <summary>
        /// Password saat ini
        /// </summary>
        [Required(ErrorMessage = "Password saat ini wajib diisi")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Saat Ini")]
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// Password baru
        /// </summary>
        [Required(ErrorMessage = "Password baru wajib diisi")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Baru")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Konfirmasi password baru
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password Baru")]
        [Compare("NewPassword", ErrorMessage = "Konfirmasi password tidak cocok")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}