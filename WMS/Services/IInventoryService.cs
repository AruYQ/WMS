using WMS.Models;
using WMS.Models.ViewModels;

namespace WMS.Services
{
    /// <summary>
    /// Interface service untuk Inventory management dan Putaway operations
    /// Menangani business logic untuk inventory tracking dan warehouse operations
    /// </summary>
    public interface IInventoryService
    {
        #region Basic CRUD Operations

        /// <summary>
        /// Get all inventories dengan details
        /// </summary>
        Task<IEnumerable<Inventory>> GetAllInventoriesAsync();

        /// <summary>
        /// Get inventory by ID dengan details
        /// </summary>
        Task<Inventory?> GetInventoryByIdAsync(int id);

        /// <summary>
        /// Create new inventory record
        /// </summary>
        Task<Inventory> CreateInventoryAsync(InventoryViewModel viewModel);

        /// <summary>
        /// Update existing inventory
        /// </summary>
        Task<Inventory> UpdateInventoryAsync(int id, InventoryViewModel viewModel);

        /// <summary>
        /// Delete inventory
        /// </summary>
        Task<bool> DeleteInventoryAsync(int id);

        #endregion

        #region Putaway Operations

        /// <summary>
        /// Get list ASN yang ready untuk putaway (status = Processed)
        /// </summary>
        Task<IEnumerable<AdvancedShippingNotice>> GetASNsReadyForPutawayAsync();

        /// <summary>
        /// Get ASN details yang siap untuk putaway
        /// </summary>
        Task<IEnumerable<ASNDetail>> GetASNDetailsForPutawayAsync(int asnId);

        /// <summary>
        /// Get putaway view model untuk specific ASN
        /// </summary>
        Task<PutawayViewModel> GetPutawayViewModelAsync(int asnId);

        /// <summary>
        /// Process putaway untuk ASN detail ke location tertentu
        /// </summary>
        Task<bool> ProcessPutawayAsync(PutawayDetailViewModel putawayDetail);

        /// <summary>
        /// Process multiple putaway items dalam satu transaction
        /// </summary>
        Task<bool> ProcessBulkPutawayAsync(IEnumerable<PutawayDetailViewModel> putawayDetails);

        /// <summary>
        /// Complete putaway process untuk specific ASN (update ASN status jika semua sudah putaway)
        /// </summary>
        Task<bool> CompletePutawayAsync(int asnId);

        /// <summary>
        /// Get available locations untuk putaway
        /// </summary>
        Task<IEnumerable<Location>> GetAvailableLocationsForPutawayAsync();

        /// <summary>
        /// Validate putaway request (check capacity, etc.)
        /// </summary>
        Task<bool> ValidatePutawayAsync(PutawayDetailViewModel putawayDetail);

        #endregion

        #region Stock Management

        /// <summary>
        /// Get total stock untuk specific item
        /// </summary>
        Task<int> GetTotalStockByItemAsync(int itemId);

        /// <summary>
        /// Get stock breakdown by location untuk specific item
        /// </summary>
        Task<IEnumerable<Inventory>> GetStockByItemAsync(int itemId);

        /// <summary>
        /// Add stock ke existing location (dari receiving/putaway)
        /// </summary>
        Task<Inventory> AddStockAsync(int itemId, int locationId, int quantity, decimal costPrice, string? sourceReference = null);

        /// <summary>
        /// Reduce stock dari location (untuk picking/shipping)
        /// </summary>
        Task<bool> ReduceStockAsync(int inventoryId, int quantity);

        /// <summary>
        /// Transfer stock dari satu location ke location lain
        /// </summary>
        Task<bool> TransferStockAsync(int fromInventoryId, int toLocationId, int quantity);

        /// <summary>
        /// Update inventory status
        /// </summary>
        Task<bool> UpdateInventoryStatusAsync(int inventoryId, string status, string? notes = null);

        /// <summary>
        /// Update inventory quantity
        /// </summary>
        Task<bool> UpdateQuantityAsync(int inventoryId, int newQuantity);

        #endregion

        #region Tracking and Analytics

        /// <summary>
        /// Get inventory movements dalam periode tertentu
        /// </summary>
        Task<IEnumerable<Inventory>> GetInventoryMovementsAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get low stock inventories yang perlu reorder
        /// </summary>
        Task<IEnumerable<Inventory>> GetLowStockInventoriesAsync(int threshold = 10);

        /// <summary>
        /// Get empty locations yang available untuk putaway
        /// </summary>
        Task<IEnumerable<Inventory>> GetEmptyLocationsAsync();

        /// <summary>
        /// Get inventory statistics untuk dashboard
        /// </summary>
        Task<Dictionary<string, object>> GetInventoryStatisticsAsync();

        /// <summary>
        /// Get inventory valuation report
        /// </summary>
        Task<Dictionary<string, object>> GetInventoryValuationAsync();

        /// <summary>
        /// Track inventory dari specific source (ASN)
        /// </summary>
        Task<IEnumerable<Inventory>> TrackInventoryBySourceAsync(string sourceReference);

        #endregion

        #region Location Management

        /// <summary>
        /// Get all inventories di specific location
        /// </summary>
        Task<IEnumerable<Inventory>> GetInventoriesByLocationAsync(int locationId);

        /// <summary>
        /// Get location utilization statistics
        /// </summary>
        Task<Dictionary<string, object>> GetLocationUtilizationAsync();

        /// <summary>
        /// Suggest optimal location untuk putaway based on item type
        /// </summary>
        Task<Location?> SuggestOptimalLocationAsync(int itemId, int quantity);

        /// <summary>
        /// Check location capacity untuk putaway
        /// </summary>
        Task<bool> CheckLocationCapacityAsync(int locationId, int additionalQuantity);

        /// <summary>
        /// Get all locations untuk dropdown
        /// </summary>
        Task<IEnumerable<Location>> GetAllLocationsAsync();

        #endregion

        #region Search and Filter

        /// <summary>
        /// Search inventory by berbagai criteria
        /// </summary>
        Task<IEnumerable<Inventory>> SearchInventoryAsync(string searchTerm);

        /// <summary>
        /// Filter inventory by status
        /// </summary>
        Task<IEnumerable<Inventory>> GetInventoriesByStatusAsync(string status);

        /// <summary>
        /// Get available inventory untuk sales (Available status, quantity > 0)
        /// </summary>
        Task<IEnumerable<Inventory>> GetAvailableInventoryForSalesAsync();

        #endregion

        #region ViewModel Operations

        /// <summary>
        /// Get inventory view model untuk create/edit
        /// </summary>
        Task<InventoryViewModel> GetInventoryViewModelAsync(int? id = null);

        /// <summary>
        /// Populate dropdown lists untuk inventory view model
        /// </summary>
        Task<InventoryViewModel> PopulateInventoryViewModelAsync(InventoryViewModel viewModel);

        /// <summary>
        /// Validate inventory business rules
        /// </summary>
        Task<bool> ValidateInventoryAsync(InventoryViewModel viewModel);

        #endregion

        #region Reporting

        /// <summary>
        /// Generate inventory aging report
        /// </summary>
        Task<IEnumerable<object>> GetInventoryAgingReportAsync();

        /// <summary>
        /// Generate ABC analysis untuk inventory
        /// </summary>
        Task<Dictionary<string, object>> GetABCAnalysisAsync();

        /// <summary>
        /// Generate turnover analysis
        /// </summary>
        Task<Dictionary<string, object>> GetInventoryTurnoverAnalysisAsync();

        /// <summary>
        /// Move stock between locations using simplified pattern
        /// </summary>
        Task<bool> MoveStockAsync(int itemId, int fromLocationId, int toLocationId, int quantity, string sourceReference);

        #endregion
    }
}