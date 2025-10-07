using WMS.Models;

namespace WMS.Data.Repositories
{
    /// <summary>
    /// Repository interface untuk Picking operations
    /// </summary>
    public interface IPickingRepository : IRepository<Picking>
    {
        // Basic CRUD with details
        Task<Picking?> GetByIdWithDetailsAsync(int id);
        Task<Picking?> GetByPickingNumberAsync(string pickingNumber);
        Task<IEnumerable<Picking>> GetAllWithDetailsAsync();
        Task<Picking> CreateWithDetailsAsync(Picking picking);
        
        // Query operations
        Task<IEnumerable<Picking>> GetBySalesOrderIdAsync(int salesOrderId);
        Task<IEnumerable<Picking>> GetByStatusAsync(string status);
        Task<IEnumerable<Picking>> GetPendingPickingsAsync();
        Task<IEnumerable<Picking>> GetInProgressPickingsAsync();
        Task<IEnumerable<Picking>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        
        // Status operations
        Task<bool> UpdateStatusAsync(int id, string status);
        Task<bool> CompletePickingAsync(int id);
        Task<bool> CancelPickingAsync(int id);
        
        // Business logic helpers
        Task<bool> ExistsForSalesOrderAsync(int salesOrderId);
        Task<string> GeneratePickingNumberAsync();
        Task<int> GetPickingCountByDateAsync(DateTime date);
    }
}
