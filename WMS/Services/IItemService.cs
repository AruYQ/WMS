using WMS.Models;
using WMS.Models.ViewModels;

namespace WMS.Services
{
    /// <summary>
    /// Service interface untuk Item management
    /// Handles master data untuk barang/item yang disimpan di warehouse
    /// </summary>
    public interface IItemService
    {
        // Basic CRUD Operations
        Task<IEnumerable<Item>> GetAllItemsAsync();
        Task<Item?> GetItemByIdAsync(int id);
        Task<Item> CreateItemAsync(Item item);
        Task<Item> UpdateItemAsync(int id, Item item);
        Task<bool> DeleteItemAsync(int id);

        // Query Operations
        Task<IEnumerable<Item>> GetActiveItemsAsync();
        Task<Item?> GetItemByCodeAsync(string itemCode);
        Task<IEnumerable<Item>> SearchItemsAsync(string searchTerm);
        Task<IEnumerable<Item>> GetItemsWithInventoryAsync();
        Task<IEnumerable<Item>> GetItemsWithLowStockAsync(int threshold = 10);

        // Supplier-related Operations
        Task<IEnumerable<Item>> GetItemsBySupplierAsync(int supplierId);
        Task<IEnumerable<Item>> GetItemsWithoutSupplierAsync();
        Task<IEnumerable<Item>> SearchItemsBySupplierAsync(string searchTerm, int supplierId);

        // Validation Operations
        Task<bool> IsItemCodeUniqueAsync(string itemCode, int? excludeId = null);
        Task<bool> ValidateItemAsync(Item item);
        Task<bool> CanDeleteItemAsync(int id);

        // Stock Information
        Task<Dictionary<int, int>> GetItemStockSummaryAsync();
        Task<int> GetItemTotalStockAsync(int itemId);
        Task<decimal> GetItemTotalValueAsync(int itemId);
        Task<IEnumerable<object>> GetItemInventoryDetailsAsync(int itemId);

        // Business Logic Operations
        Task<bool> UpdateItemStatusAsync(int id, bool isActive);
        Task<IEnumerable<Item>> GetItemsForPurchaseOrderAsync();
        // Sales Order - DISABLED
        // Task<IEnumerable<Item>> GetItemsForSalesOrderAsync();
        // Task<IEnumerable<Item>> GetAvailableItemsAsync();

        // Reporting Operations
        // Sales Order - DISABLED
        // Task<IEnumerable<object>> GetItemUsageReportAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<IEnumerable<object>> GetItemPerformanceReportAsync();
        Task<Dictionary<string, object>> GetItemStatisticsAsync();
        // Task<IEnumerable<object>> GetTopSellingItemsAsync(int topCount = 10);
        Task<IEnumerable<object>> GetSlowMovingItemsAsync(int daysThreshold = 90);

        // Price Analysis
        Task<object> GetItemPriceHistoryAsync(int itemId);
        Task<decimal> GetItemAverageCostAsync(int itemId);
        Task<decimal> GetItemLastCostAsync(int itemId);
        Task<IEnumerable<object>> GetItemPriceVarianceReportAsync();

        // Integration Operations
        Task<bool> SyncItemWithInventoryAsync(int itemId);
        Task<IEnumerable<Item>> GetItemsNeedingRestockAsync();
        Task<object> GetItemSupplierInfoAsync(int itemId);
        Task<IEnumerable<object>> GetSuppliersForDropdownAsync(string? search = null, int limit = 20);
        Task<SupplierAdvancedSearchResponse> SearchSuppliersAdvancedAsync(SupplierAdvancedSearchRequest request);

        // ViewModel Operations (New - following Inventory pattern)
        Task<ItemViewModel> GetItemViewModelAsync(int? id = null);
        Task<ItemViewModel> PopulateItemViewModelAsync(ItemViewModel viewModel);
        Task<ItemIndexViewModel> GetItemIndexViewModelAsync(ItemIndexViewModel? model = null);
        Task<ItemDetailsViewModel> GetItemDetailsViewModelAsync(int id);
        Task<ItemSummaryViewModel> GetItemSummaryAsync();
    }
}