using Microsoft.AspNetCore.Mvc;
using WMS.Models;

namespace WMS.Views.Customer
{
    public class DeleteModel
    {
        public WMS.Models.Customer Customer { get; set; } = new WMS.Models.Customer();

        public void OnGet()
        {
            // This method can be used for additional initialization if needed
            // The main data loading is handled in the controller
        }

        public IActionResult OnPost()
        {
            // This method can be used for additional processing if needed
            // The main form submission is handled by the controller
            return new RedirectToActionResult("Index", "Customer", null);
        }
    }
}
