using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Service interface untuk Inventory management
    /// "The Stage Design" - mengatur lokasi dan tracking item
    /// </summary>
    public interface IInventoryService
    {
        // Basic CRUD Operations
        Task<IEnumerable<Inventory>> GetAllInventoryAsync();
        Task<Inventory?> GetInventoryByIdAsync(int id);
        Task<Inventory> CreateInventoryAsync(InventoryViewModel viewModel);
        Task<Inventory> UpdateInventoryAsync(int id, InventoryViewModel viewModel);
        Task<bool> DeleteInventoryAsync(int id);

        // Query Operations
        Task<IEnumerable<Inventory>> GetInventoryByItemAsync(int itemId);
        Task<IEnumerable<Inventory>> GetInventoryByLocationAsync(int locationId);
        Task<IEnumerable<Inventory>> GetInventoryByStatusAsync(InventoryStatus status);
        Task<IEnumerable<Inventory>> GetAvailableInventoryAsync();
        Task<IEnumerable<Inventory>> GetLowStockInventoryAsync(int threshold = 10);
        Task<Inventory?> GetInventoryByItemAndLocationAsync(int itemId, int locationId);

        // Putaway Operations
        Task<bool> ProcessPutawayAsync(PutawayViewModel viewModel);
        Task<IEnumerable<AdvancedShippingNotice>> GetASNsForPutawayAsync();
        Task<IEnumerable<ASNDetail>> GetASNDetailsForPutawayAsync(int asnId);
        Task<PutawayViewModel> GetPutawayViewModelAsync(int? asnId = null, int? asnDetailId = null);
        Task<bool> ValidatePutawayAsync(PutawayViewModel viewModel);

        // Stock Management Operations
        Task<bool> AddStockAsync(int itemId, int locationId, int quantity, decimal costPrice);
        Task<bool> ReduceStockAsync(int itemId, int locationId, int quantity);
        Task<bool> TransferStockAsync(StockTransferViewModel viewModel);
        Task<bool> AdjustStockAsync(InventoryAdjustmentViewModel viewModel);
        Task<bool> UpdateStockStatusAsync(int inventoryId, InventoryStatus status, string? notes = null);

        // Stock Validation Operations
        Task<bool> CheckStockAvailabilityAsync(int itemId, int requiredQuantity);
        Task<Dictionary<int, int>> GetAvailableStockByItemsAsync(IEnumerable<int> itemIds);
        Task<bool> IsLocationSuitableForPutawayAsync(int locationId, int quantity);
        Task<IEnumerable<Location>> GetAvailableLocationsForPutawayAsync(int requiredCapacity);

        // ViewModel Operations
        Task<InventoryViewModel> GetInventoryViewModelAsync(int? id = null);
        Task<InventoryViewModel> PopulateInventoryViewModelAsync(InventoryViewModel viewModel);
        Task<StockTransferViewModel> GetStockTransferViewModelAsync(int? fromInventoryId = null);
        Task<StockTransferViewModel> PopulateStockTransferViewModelAsync(StockTransferViewModel viewModel);
        Task<InventoryAdjustmentViewModel> GetInventoryAdjustmentViewModelAsync(int inventoryId);

        // Reporting Operations
        Task<decimal> GetTotalInventoryValueAsync();
        Task<Dictionary<string, int>> GetInventoryByStatusSummaryAsync();
        Task<Dictionary<int, int>> GetItemStockSummaryAsync();
        Task<IEnumerable<object>> GetLowStockReportAsync(int threshold = 10);
        Task<IEnumerable<object>> GetInventoryMovementReportAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<Dictionary<string, object>> GetInventoryStatisticsAsync();

        // Location Management
        Task<bool> UpdateLocationCapacityAsync(int locationId);
        Task<Dictionary<string, object>> GetLocationUtilizationAsync();
        Task<IEnumerable<Location>> GetOverCapacityLocationsAsync();

        // Item Tracking Operations
        Task<IEnumerable<object>> GetItemLocationHistoryAsync(int itemId);
        Task<IEnumerable<object>> GetLocationInventoryDetailsAsync(int locationId);
        Task<object> GetItemCurrentLocationsAsync(int itemId);

        // Business Logic Operations
        Task<bool> ProcessASNReceiptAsync(int asnDetailId, int locationId, int quantity);
        Task<bool> ProcessSalesOrderPickingAsync(int salesOrderId);
        Task<IEnumerable<object>> GetPickingListAsync(int salesOrderId);
        Task<bool> ValidatePickingCapabilityAsync(int salesOrderId);

        // Inventory Optimization
        Task<IEnumerable<object>> GetInventoryOptimizationSuggestionsAsync();
        Task<IEnumerable<object>> GetSlowMovingInventoryAsync(int daysThreshold = 90);
        Task<IEnumerable<object>> GetFastMovingInventoryAsync(int daysThreshold = 30);
    }
}