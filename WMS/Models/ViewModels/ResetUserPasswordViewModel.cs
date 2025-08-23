using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk admin reset user password
    /// </summary>
    public class ResetUserPasswordViewModel
    {
        [Required]
        public int UserId { get; set; }

        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Nama Lengkap")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password baru wajib diisi")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password baru harus antara 6-100 karakter")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Baru")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password wajib diisi")]
        [DataType(DataType.Password)]
        [Display(Name = "Konfirmasi Password Baru")]
        [Compare("NewPassword", ErrorMessage = "Konfirmasi password tidak cocok")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Beri tahu user via email")]
        public bool NotifyUser { get; set; } = true;
    }
}

// Models/ViewModels/PagedResult.cs (if not already defined elsewhere)
namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Generic paged result untuk ViewModel
    /// </summary>
    /// <typeparam name="T">Type of items</typeparam>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// Calculate starting item number untuk display
        /// </summary>
        public int StartItem => (PageNumber - 1) * PageSize + 1;

        /// <summary>
        /// Calculate ending item number untuk display
        /// </summary>
        public int EndItem => Math.Min(StartItem + PageSize - 1, TotalItems);

        /// <summary>
        /// Get page numbers untuk pagination display
        /// </summary>
        public IEnumerable<int> GetPageNumbers(int maxPages = 5)
        {
            var startPage = Math.Max(1, PageNumber - maxPages / 2);
            var endPage = Math.Min(TotalPages, startPage + maxPages - 1);

            // Adjust start page if we're near the end
            if (endPage - startPage + 1 < maxPages)
            {
                startPage = Math.Max(1, endPage - maxPages + 1);
            }

            return Enumerable.Range(startPage, endPage - startPage + 1);
        }
    }
}