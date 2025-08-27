using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk data user/pengguna aplikasi
    /// </summary>
    public class User : BaseEntity
    {
        /// <summary>
        /// Username untuk login (unik dalam company)
        /// </summary>
        [Required(ErrorMessage = "Username wajib diisi")]
        [MaxLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email user (unik global)
        /// </summary>
        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [MaxLength(100, ErrorMessage = "Email maksimal 100 karakter")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Password hash (never store plain password)
        /// Using HashedPassword as per existing structure
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string HashedPassword { get; set; } = string.Empty;

        /// <summary>
        /// Nama lengkap user
        /// </summary>
        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        [MaxLength(100, ErrorMessage = "Nama maksimal 100 karakter")]
        [Display(Name = "Nama Lengkap")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Nomor telepon user
        /// </summary>
        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [MaxLength(20, ErrorMessage = "Nomor telepon maksimal 20 karakter")]
        [Display(Name = "Nomor Telepon")]
        public string? Phone { get; set; }

        /// <summary>
        /// Status aktif user
        /// </summary>
        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Terakhir login
        /// </summary>
        [Display(Name = "Terakhir Login")]
        public DateTime? LastLoginDate { get; set; }

        /// <summary>
        /// Token untuk reset password
        /// </summary>
        [MaxLength(200)]
        public string? ResetPasswordToken { get; set; }

        /// <summary>
        /// Expiry time untuk reset password token
        /// </summary>
        public DateTime? ResetPasswordTokenExpiry { get; set; }

        /// <summary>
        /// Apakah akun sudah diverifikasi email
        /// </summary>
        [Display(Name = "Email Terverifikasi")]
        public bool EmailVerified { get; set; } = false;

        /// <summary>
        /// Token untuk verifikasi email
        /// </summary>
        [MaxLength(200)]
        public string? EmailVerificationToken { get; set; }

        // Navigation Properties
        // Company navigation property is already inherited from BaseEntity

        /// <summary>
        /// Roles yang dimiliki user ini
        /// </summary>
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        // Calculated Properties
        /// <summary>
        /// Display name untuk UI
        /// </summary>
        [NotMapped]
        public string DisplayName => $"{FullName} ({Username})";

        /// <summary>
        /// List nama roles user ini
        /// </summary>
        [NotMapped]
        public IEnumerable<string> RoleNames => UserRoles?.Select(ur => ur.Role?.Name ?? "") ?? new List<string>();

        /// <summary>
        /// Check apakah user memiliki role tertentu
        /// </summary>
        [NotMapped]
        public bool IsAdmin => UserRoles?.Any(ur => ur.Role?.Name == "Admin" || ur.Role?.Name == "SuperAdmin") == true;

        /// <summary>
        /// Check apakah user adalah manager atau admin
        /// </summary>
        [NotMapped]
        public bool IsManagerOrAdmin => UserRoles?.Any(ur => ur.Role?.Name is "Manager" or "Admin" or "SuperAdmin") == true;
    }
}