using Microsoft.AspNetCore.Mvc;
using WMS.Models.ViewModels;

namespace WMS.Views.Customer
{
    public class DetailsModel
    {
        public CustomerViewModel ViewModel { get; set; } = new CustomerViewModel();

        public void OnGet()
        {
            // This method can be used for additional initialization if needed
            // The main data loading is handled in the controller
        }
    }
}
