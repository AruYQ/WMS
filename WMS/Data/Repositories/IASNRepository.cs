using WMS.Models;
using WMS.Utilities;

namespace WMS.Data.Repositories
{
    public interface IASNRepository : IRepository<AdvancedShippingNotice>
    {
        Task<IEnumerable<AdvancedShippingNotice>> GetAllWithDetailsAsync();
        Task<AdvancedShippingNotice?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<AdvancedShippingNotice>> GetByPurchaseOrderAsync(int purchaseOrderId);
        Task<IEnumerable<AdvancedShippingNotice>> GetByStatusAsync(ASNStatus status);
        Task<IEnumerable<AdvancedShippingNotice>> GetArrivedASNsAsync();
        Task<bool> ExistsByASNNumberAsync(string asnNumber);
        Task<string> GenerateNextASNNumberAsync();
        Task<AdvancedShippingNotice> CreateWithDetailsAsync(AdvancedShippingNotice asn);
        Task UpdateStatusAsync(int id, ASNStatus status);
    }
}