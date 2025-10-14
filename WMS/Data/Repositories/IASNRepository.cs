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
        Task<bool> UpdateStatusAsync(int asnId, ASNStatus status);
        Task<IEnumerable<AdvancedShippingNotice>> GetProcessedASNsAsync();
        Task<IEnumerable<ASNDetail>> GetASNDetailsForPutawayAsync(int asnId);
        Task<ASNDetail?> GetASNDetailByIdAsync(int asnDetailId);
        Task<int> GetPutAwayQuantityByASNDetailAsync(int asnDetailId);

        /// <summary>
        /// Advanced search untuk ASN
        /// </summary>
        Task<IEnumerable<AdvancedShippingNotice>> SearchAsync(WMS.Models.ViewModels.ASNSearchRequest request);

        /// <summary>
        /// Quick search untuk ASN
        /// </summary>
        Task<IEnumerable<AdvancedShippingNotice>> QuickSearchAsync(string query);
    }
}