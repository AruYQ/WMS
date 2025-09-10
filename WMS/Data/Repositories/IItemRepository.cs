using WMS.Models;

namespace WMS.Data.Repositories
{
    public interface IItemRepository : IRepository<Item>
    {
        // Basic CRUD with includes
        Task<IEnumerable<Item>> GetAllWithInventoryAsync();
        Task<Item?> GetByIdWithInventoryAsync(int id);
        Task<Item?> GetByItemCodeAsync(string itemCode);

        // Query Operations
        Task<IEnumerable<Item>> GetBySupplierIdAsync(int supplierId);
        Task<IEnumerable<Item>> GetActiveItemsAsync();
        Task<IEnumerable<Item>> SearchItemsAsync(string searchTerm);
        Task<IEnumerable<Item>> GetItemsWithoutSupplierAsync();
        Task<IEnumerable<Item>> SearchItemsBySupplierAsync(string searchTerm, int supplierId);
        Task<IEnumerable<Item>> GetItemsWithLowStockAsync(int threshold = 10);

        // Related Data
        Task<IEnumerable<Inventory>> GetInventoriesByItemIdAsync(int itemId);
        Task<IEnumerable<PurchaseOrderDetail>> GetPurchaseOrderDetailsByItemIdAsync(int itemId);
        Task<IEnumerable<ASNDetail>> GetASNDetailsByItemIdAsync(int itemId);
        Task<IEnumerable<SalesOrderDetail>> GetSalesOrderDetailsByItemIdAsync(int itemId);

        // Validation
        Task<bool> IsItemCodeUniqueAsync(string itemCode, int? excludeId = null);

        // Stock Information
        Task<Dictionary<int, int>> GetItemStockSummaryAsync();

        // Supplier Options for Dropdown
        Task<IEnumerable<Supplier>> GetActiveSuppliersForDropdownAsync();
    }
}