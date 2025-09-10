using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WMS.Models.ViewModels;

namespace WMS.Views.Customer
{
    public class IndexModel
    {
        public CustomerIndexViewModel ViewModel { get; set; } = new CustomerIndexViewModel();

        public void OnGet()
        {
            // This method can be used for additional initialization if needed
            // The main data loading is handled in the controller
        }
    }
}
