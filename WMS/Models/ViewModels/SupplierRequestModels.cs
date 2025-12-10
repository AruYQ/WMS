using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    public class SupplierCreateRequest
    {
        [StringLength(20, ErrorMessage = "Code cannot exceed 20 characters")]
        public string? Code { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone format")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
        public string? Phone { get; set; }

        [StringLength(100, ErrorMessage = "Contact Person cannot exceed 100 characters")]
        public string? ContactPerson { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string? Address { get; set; }

        [StringLength(50, ErrorMessage = "City cannot exceed 50 characters")]
        public string? City { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class SupplierUpdateRequest : SupplierCreateRequest
    {
        [Required]
        public int Id { get; set; }
    }
}
