using System.ComponentModel.DataAnnotations;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk role/jabatan dalam sistem
    /// </summary>
    public class Role : BaseEntityWithoutCompany
    {
        /// <summary>
        /// Nama role (Admin, Manager, User, dll)
        /// </summary>
        [Required(ErrorMessage = "Nama role wajib diisi")]
        [MaxLength(50, ErrorMessage = "Nama role maksimal 50 karakter")]
        [Display(Name = "Nama Role")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Deskripsi role
        /// </summary>
        [MaxLength(200, ErrorMessage = "Deskripsi maksimal 200 karakter")]
        [Display(Name = "Deskripsi")]
        public string? Description { get; set; }

        /// <summary>
        /// Permissions yang dimiliki role ini (stored as JSON)
        /// </summary>
        public string Permissions { get; set; } = "[]";

        /// <summary>
        /// Status aktif role
        /// </summary>
        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        /// <summary>
        /// Users yang memiliki role ini
        /// </summary>
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}