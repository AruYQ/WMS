using WMS.Models;
using WMS.Models.ViewModels;

namespace WMS.Services
{
    /// <summary>
    /// Service interface untuk Picking management
    /// </summary>
    public interface IPickingService
    {
        // Basic CRUD Operations
        Task<IEnumerable<Picking>> GetAllPickingsAsync();
        Task<Picking?> GetPickingByIdAsync(int id);
        Task<Picking?> GetPickingByNumberAsync(string pickingNumber);
        Task<bool> DeletePickingAsync(int id);

        // Generate & Create Picking
        Task<Picking> GeneratePickingListAsync(int salesOrderId);
        Task<PickingViewModel> GetPickingViewModelAsync(int? pickingId = null);
        Task<PickingViewModel> PopulatePickingViewModelAsync(PickingViewModel viewModel);

        // Process Picking Operations
        Task<(bool Success, string ErrorMessage)> ProcessPickingAsync(PickingDetailViewModel detail);
        Task<bool> ProcessBulkPickingAsync(IEnumerable<PickingDetailViewModel> details);
        Task<bool> CompletePickingAsync(int pickingId);
        Task<bool> CancelPickingAsync(int pickingId);

        // Query Operations
        Task<IEnumerable<Picking>> GetPickingsBySalesOrderAsync(int salesOrderId);
        Task<IEnumerable<Picking>> GetPickingsByStatusAsync(string status);
        Task<IEnumerable<Picking>> GetPendingPickingsAsync();
        Task<IEnumerable<Picking>> GetInProgressPickingsAsync();
        Task<IEnumerable<PickingListViewModel>> GetPickingListSummaryAsync();

        // Location Suggestions (FIFO)
        Task<IEnumerable<LocationSuggestion>> GetPickingSuggestionsAsync(int itemId, int quantityRequired);
        Task<List<PickingDetailViewModel>> GeneratePickingDetailsAsync(int salesOrderId);

        // Validation
        Task<bool> ValidatePickingAsync(PickingDetailViewModel detail);
        Task<bool> CanGeneratePickingAsync(int salesOrderId);
        Task<bool> CanCompletePickingAsync(int pickingId);

        // Status helpers
        Task<bool> UpdatePickingStatusAsync(int pickingId, string status);
    }
}
