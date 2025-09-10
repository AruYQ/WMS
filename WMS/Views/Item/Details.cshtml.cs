using Microsoft.AspNetCore.Mvc;
using WMS.Models.ViewModels;

namespace WMS.Views.Item
{
    public class DetailsModel
    {
        public ItemDetailsViewModel ViewModel { get; set; } = new ItemDetailsViewModel();

        public void OnGet()
        {
            // This method can be used for additional initialization if needed
            // The main data loading is handled in the controller
        }
    }
}
