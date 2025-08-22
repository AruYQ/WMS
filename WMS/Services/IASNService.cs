using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Service interface untuk Advanced Shipping Notice management
    /// Handles warehouse fee calculation dan actual price tracking
    /// </summary>
    public interface IASNService
    {
        // Basic CRUD Operations
        Task<IEnumerable<AdvancedShippingNotice>> GetAllASNsAsync();
        Task<AdvancedShippingNotice?> GetASNByIdAsync(int id);
        Task<AdvancedShippingNotice> CreateASNAsync(ASNViewModel viewModel);
        Task<AdvancedShippingNotice> UpdateASNAsync(int id, ASNViewModel viewModel);
        Task<bool> DeleteASNAsync(int id);

        // Business Logic Operations
        Task<bool> UpdateStatusAsync(int id, ASNStatus status);
        Task<bool> ProcessASNAsync(int id);
        Task<bool> MarkAsArrivedAsync(int id);
        Task<bool> MarkAsProcessedAsync(int id);
        Task<bool> CancelASNAsync(int id);

        // Query Operations
        Task<IEnumerable<AdvancedShippingNotice>> GetASNsByPurchaseOrderAsync(int purchaseOrderId);
        Task<IEnumerable<AdvancedShippingNotice>> GetASNsByStatusAsync(ASNStatus status);
        Task<IEnumerable<AdvancedShippingNotice>> GetArrivedASNsAsync();
        Task<IEnumerable<AdvancedShippingNotice>> GetInTransitASNsAsync();

        // Validation Operations
        Task<bool> ValidateASNAsync(ASNViewModel viewModel);
        Task<string> GenerateNextASNNumberAsync();
        Task<bool> IsASNNumberUniqueAsync(string asnNumber);
        Task<bool> CanEditASNAsync(int id);
        Task<bool> CanProcessASNAsync(int id);
        Task<bool> CanCancelASNAsync(int id);

        // ViewModel Operations
        Task<ASNViewModel> GetASNViewModelAsync(int? id = null);
        Task<ASNViewModel> PopulateASNViewModelAsync(ASNViewModel viewModel);

        // Warehouse Fee Calculation
        Task<decimal> CalculateWarehouseFeeRateAsync(decimal actualPrice);
        Task<decimal> CalculateWarehouseFeeAmountAsync(decimal actualPrice);
        Task<ASNDetail> CalculateWarehouseFeeForDetailAsync(ASNDetail detail);
        Task<AdvancedShippingNotice> RecalculateWarehouseFeesAsync(AdvancedShippingNotice asn);

        // Price Analysis
        Task<Dictionary<string, object>> GetPriceVarianceAnalysisAsync(int asnId);
        Task<IEnumerable<ASNDetail>> GetHighWarehouseFeeItemsAsync(decimal threshold = 0.05m);
        Task<Dictionary<string, decimal>> GetWarehouseFeeStatisticsAsync();

        // Purchase Order Integration
        Task<IEnumerable<PurchaseOrder>> GetAvailablePurchaseOrdersAsync();
        Task<PurchaseOrder?> GetPurchaseOrderForASNAsync(int purchaseOrderId);
        Task<IEnumerable<PurchaseOrderDetailViewModel>> GetPODetailsForASNAsync(int purchaseOrderId);
        Task<bool> ValidateASNAgainstPOAsync(ASNViewModel viewModel);
    }
}