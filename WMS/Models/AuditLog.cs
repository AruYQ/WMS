using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk audit trail / activity logging
    /// Tracks all important user actions in the system
    /// </summary>
    public class AuditLog : BaseEntityWithoutCompany
    {
        /// <summary>
        /// Company ID (optional - SuperAdmin actions may not have company)
        /// </summary>
        public int? CompanyId { get; set; }

        /// <summary>
        /// User ID who performed the action
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Username who performed the action
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Action performed (CREATE, UPDATE, DELETE, etc)
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "Action")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Module/Entity affected (PurchaseOrder, Item, Customer, etc)
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "Module")]
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// Entity ID that was affected
        /// </summary>
        public int? EntityId { get; set; }

        /// <summary>
        /// Entity description (e.g. "PO-20231201-001")
        /// </summary>
        [MaxLength(200)]
        public string? EntityDescription { get; set; }

        /// <summary>
        /// Old value (JSON format for detailed changes)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? OldValue { get; set; }

        /// <summary>
        /// New value (JSON format for detailed changes)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? NewValue { get; set; }

        /// <summary>
        /// IP Address of the user
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent / browser information
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Timestamp when the action occurred
        /// </summary>
        [Required]
        [Display(Name = "Timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Additional notes or error messages
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Success status of the operation
        /// </summary>
        public bool IsSuccess { get; set; } = true;

        // Navigation Properties
        /// <summary>
        /// Company related to this audit log
        /// </summary>
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        /// <summary>
        /// User who performed the action
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        // Calculated Properties
        /// <summary>
        /// Display text for UI
        /// </summary>
        [NotMapped]
        public string DisplayText => $"{Action} {Module}" + (EntityDescription != null ? $" - {EntityDescription}" : "");

        /// <summary>
        /// Badge color based on action
        /// </summary>
        [NotMapped]
        public string BadgeColor => Action switch
        {
            "CREATE" => "success",
            "UPDATE" => "info",
            "DELETE" => "danger",
            "VIEW" => "secondary",
            "EXPORT" => "primary",
            _ => "light"
        };
    }
}

