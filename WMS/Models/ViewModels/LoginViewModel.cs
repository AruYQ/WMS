using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username wajib diisi")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ingat Saya")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}