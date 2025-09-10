using Microsoft.AspNetCore.Mvc;
using WMS.Models;

namespace WMS.Views.Customer
{
    public class CreateModel
    {
        public WMS.Models.Customer Customer { get; set; } = new WMS.Models.Customer();

        public void OnGet()
        {
            // Initialize with default values
            Customer.IsActive = true;
        }

        public IActionResult OnPost()
        {
            // This method can be used for additional processing if needed
            // The main form submission is handled by the controller
            return new RedirectToActionResult("Index", "Customer", null);
        }
    }
}
