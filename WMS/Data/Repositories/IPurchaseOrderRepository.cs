using WMS.Models;
using WMS.Utilities;

namespace WMS.Data.Repositories
{
    public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
    {
        Task<IEnumerable<PurchaseOrder>> GetAllWithDetailsAsync();
        Task<PurchaseOrder?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<PurchaseOrder>> GetBySupplierAsync(int supplierId);
        Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(PurchaseOrderStatus status);
        Task<IEnumerable<PurchaseOrder>> GetPendingPurchaseOrdersAsync();
        Task<bool> ExistsByPONumberAsync(string poNumber);
        Task<string> GenerateNextPONumberAsync();
        Task<PurchaseOrder> CreateWithDetailsAsync(PurchaseOrder purchaseOrder);
        Task UpdateStatusAsync(int id, PurchaseOrderStatus status);

        new Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Advanced search untuk Purchase Order
        /// </summary>
        Task<IEnumerable<PurchaseOrder>> SearchAsync(PurchaseOrderSearchRequest request);

        /// <summary>
        /// Quick search untuk Purchase Order
        /// </summary>
        Task<IEnumerable<PurchaseOrder>> QuickSearchAsync(string query);
    }
}