using WMS.Models;
using WMS.Utilities;

namespace WMS.Data.Repositories
{
    public interface IInventoryRepository : IRepository<Inventory>
    {
        Task<IEnumerable<Inventory>> GetAllWithDetailsAsync();
        Task<Inventory?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<Inventory>> GetByItemAsync(int itemId);
        Task<IEnumerable<Inventory>> GetByLocationAsync(int locationId);
        Task<IEnumerable<Inventory>> GetByStatusAsync(InventoryStatus status);
        Task<IEnumerable<Inventory>> GetAvailableInventoryAsync();
        Task<IEnumerable<Inventory>> GetLowStockInventoryAsync(int threshold = 10);
        Task<Inventory?> GetByItemAndLocationAsync(int itemId, int locationId);
        Task<decimal> GetTotalInventoryValueAsync();
        Task<Dictionary<string, int>> GetInventoryByStatusAsync();
        Task<Dictionary<int, int>> GetItemStockSummaryAsync();
        Task UpdateStockAsync(int itemId, int locationId, int quantity, decimal costPrice);
        Task<bool> CheckStockAvailabilityAsync(int itemId, int requiredQuantity);
        Task<IEnumerable<Inventory>> GetInventoryForPutawayAsync();

        // Override the base DeleteAsync to return bool indicating success/failure
        new Task<bool> DeleteAsync(int id);
    }
}