using Microsoft.AspNetCore.Mvc;
using WMS.Models.ViewModels;

namespace WMS.Views.Item
{
    public class EditModel
    {
        public ItemViewModel ViewModel { get; set; } = new ItemViewModel();

        public void OnGet()
        {
            // This method can be used for additional initialization if needed
            // The main data loading is handled in the controller
        }

        public IActionResult OnPost()
        {
            // This method can be used for additional processing if needed
            // The main form submission is handled by the controller
            return new RedirectToActionResult("Index", "Item", null);
        }
    }
}
