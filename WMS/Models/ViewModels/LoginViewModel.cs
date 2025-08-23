using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk login form
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        /// Username atau Email
        /// </summary>
        [Required(ErrorMessage = "Username atau Email wajib diisi")]
        [Display(Name = "Username / Email")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        /// <summary>
        /// Password
        /// </summary>
        [Required(ErrorMessage = "Password wajib diisi")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Remember Me checkbox
        /// </summary>
        [Display(Name = "Ingat saya")]
        public bool RememberMe { get; set; } = false;

        /// <summary>
        /// Return URL after successful login
        /// </summary>
        public string? ReturnUrl { get; set; }
    }
}