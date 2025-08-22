
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models
{
    /// <summary>
    /// Junction table untuk Many-to-Many relationship User dan Role
    /// </summary>
    public class UserRole : BaseEntityWithoutCompany
    {
        /// <summary>
        /// User ID
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Role ID
        /// </summary>
        [Required]
        public int RoleId { get; set; }

        /// <summary>
        /// Tanggal role diberikan ke user
        /// </summary>
        [Required]
        [Display(Name = "Tanggal Diberikan")]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// User yang memberikan role ini
        /// </summary>
        [MaxLength(50)]
        [Display(Name = "Diberikan Oleh")]
        public string? AssignedBy { get; set; }

        // Navigation Properties
        /// <summary>
        /// User yang memiliki role
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        /// <summary>
        /// Role yang dimiliki user
        /// </summary>
        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }
    }
}