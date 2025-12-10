using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Request model untuk cancel Purchase Order, ASN, atau Sales Order
    /// </summary>
    public class CancelRequest
    {
        /// <summary>
        /// Reason untuk cancellation (akan ditambahkan ke Notes dengan format "Cancelation reason : [Reason]")
        /// </summary>
        [Required(ErrorMessage = "Reason is required")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; } = string.Empty;
    }
}
