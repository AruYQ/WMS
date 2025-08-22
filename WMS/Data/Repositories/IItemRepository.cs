using WMS.Models;

namespace WMS.Data.Repositories
{
    public interface IItemRepository : IRepository<Item>
    {
        Task<IEnumerable<Item>> GetAllWithInventoryAsync();
        Task<Item?> GetByIdWithInventoryAsync(int id);
        Task<Item?> GetByItemCodeAsync(string itemCode);
        Task<IEnumerable<Item>> GetActiveItemsAsync();
        Task<bool> ExistsByItemCodeAsync(string itemCode);
        Task<IEnumerable<Item>> SearchItemsAsync(string searchTerm);
        Task<Dictionary<int, int>> GetItemStockSummaryAsync();
        Task<IEnumerable<Item>> GetItemsWithLowStockAsync(int threshold = 10);
        new Task<bool> DeleteAsync(int id);
    }
}