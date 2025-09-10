using WMS.Models;
using System.Linq.Expressions;

namespace WMS.Data.Repositories
{
    /// <summary>
    /// Interface repository untuk Inventory management
    /// Menangani operasi CRUD dan query khusus untuk inventory
    /// </summary>
    public interface IInventoryRepository : IRepository<Inventory>
    {
        /// <summary>
        /// Get all inventories dengan include Item dan Location
        /// </summary>
        Task<IEnumerable<Inventory>> GetAllWithDetailsAsync();

        /// <summary>
        /// Get inventory by ID dengan include Item dan Location
        /// </summary>
        Task<Inventory?> GetByIdWithDetailsAsync(int id);

        /// <summary>
        /// Get inventory by Item ID dan Location ID
        /// </summary>
        Task<Inventory?> GetByItemAndLocationAsync(int itemId, int locationId);

        /// <summary>
        /// Get all inventories by Item ID
        /// </summary>
        Task<IEnumerable<Inventory>> GetByItemIdAsync(int itemId);

        /// <summary>
        /// Get all inventories by Location ID
        /// </summary>
        Task<IEnumerable<Inventory>> GetByLocationIdAsync(int locationId);

        /// <summary>
        /// Get inventories by status
        /// </summary>
        Task<IEnumerable<Inventory>> GetByStatusAsync(string status);

        /// <summary>
        /// Get low stock inventories
        /// </summary>
        Task<IEnumerable<Inventory>> GetLowStockInventoriesAsync(int threshold = 10);

        /// <summary>
        /// Get empty locations (inventories with quantity = 0)
        /// </summary>
        Task<IEnumerable<Inventory>> GetEmptyLocationsAsync();

        /// <summary>
        /// Get inventories by source reference (untuk tracking dari ASN)
        /// </summary>
        Task<IEnumerable<Inventory>> GetBySourceReferenceAsync(string sourceReference);

        /// <summary>
        /// Check if inventory exists for item at location
        /// </summary>
        Task<bool> ExistsAtLocationAsync(int itemId, int locationId);

        /// <summary>
        /// Get total stock for an item across all locations
        /// </summary>
        Task<int> GetTotalStockByItemAsync(int itemId);

        /// <summary>
        /// Get total value of all inventories
        /// </summary>
        Task<decimal> GetTotalInventoryValueAsync();

        /// <summary>
        /// Update inventory quantity (untuk putaway/picking)
        /// </summary>
        Task<bool> UpdateQuantityAsync(int inventoryId, int newQuantity);

        /// <summary>
        /// Add stock to existing inventory atau create new
        /// </summary>
        Task<Inventory> AddOrUpdateStockAsync(int itemId, int locationId, int quantity, decimal costPrice, string? sourceReference = null);

        /// <summary>
        /// Reduce stock from inventory
        /// </summary>
        Task<bool> ReduceStockAsync(int inventoryId, int quantity);

        /// <summary>
        /// Get inventory statistics summary
        /// </summary>
        Task<Dictionary<string, object>> GetInventoryStatisticsAsync();

        /// <summary>
        /// Get available locations for putaway (locations yang belum penuh)
        /// </summary>
        Task<IEnumerable<Location>> GetAvailableLocationsForPutawayAsync();

        /// <summary>
        /// Get available locations untuk putaway dengan filter quantity
        /// </summary>
        Task<IEnumerable<Location>> GetAvailableLocationsForQuantityAsync(int requiredQuantity);

        /// <summary>
        /// Bulk update inventory status
        /// </summary>
        Task<bool> BulkUpdateStatusAsync(IEnumerable<int> inventoryIds, string status);

        /// <summary>
        /// Get inventory movements/history (berdasarkan ModifiedDate)
        /// </summary>
        Task<IEnumerable<Inventory>> GetInventoryMovementsAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get items yang available untuk sale (stock > 0, status Available)
        /// </summary>
        Task<IEnumerable<Inventory>> GetAvailableForSaleAsync();

        /// <summary>
        /// Search inventory by item code atau nama
        /// </summary>
        Task<IEnumerable<Inventory>> SearchInventoryAsync(string searchTerm);
    }
}