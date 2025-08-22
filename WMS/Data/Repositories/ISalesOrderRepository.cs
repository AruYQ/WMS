using WMS.Models;
using WMS.Utilities;

namespace WMS.Data.Repositories
{
    public interface ISalesOrderRepository : IRepository<SalesOrder>
    {
        Task<IEnumerable<SalesOrder>> GetAllWithDetailsAsync();
        Task<SalesOrder?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<SalesOrder>> GetByCustomerAsync(int customerId);
        Task<IEnumerable<SalesOrder>> GetByStatusAsync(SalesOrderStatus status);
        Task<IEnumerable<SalesOrder>> GetConfirmedSalesOrdersAsync();
        Task<bool> ExistsBySONumberAsync(string soNumber);
        Task<string> GenerateNextSONumberAsync();
        Task<SalesOrder> CreateWithDetailsAsync(SalesOrder salesOrder);
        Task UpdateStatusAsync(int id, SalesOrderStatus status);
        new Task<bool> DeleteAsync(int id);
    }
}