using Microsoft.AspNetCore.Mvc;
using WMS.Models.ViewModels;

namespace WMS.Views.Item
{
    public class CreateModel
    {
        public ItemViewModel ViewModel { get; set; } = new ItemViewModel();

        public void OnGet()
        {
            // Initialize with default values
            ViewModel.IsActive = true;
        }

        public IActionResult OnPost()
        {
            // This method can be used for additional processing if needed
            // The main form submission is handled by the controller
            return new RedirectToActionResult("Index", "Item", null);
        }
    }
}
