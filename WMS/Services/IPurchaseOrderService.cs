using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Service interface untuk Purchase Order management
    /// Handles business logic untuk PO creation, editing, dan email sending
    /// </summary>
    public interface IPurchaseOrderService
    {
        // Basic CRUD Operations
        Task<IEnumerable<PurchaseOrder>> GetAllPurchaseOrdersAsync();
        Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int id);
        Task<PurchaseOrder> CreatePurchaseOrderAsync(PurchaseOrderViewModel viewModel);
        Task<PurchaseOrder> UpdatePurchaseOrderAsync(int id, PurchaseOrderViewModel viewModel);
        Task<bool> DeletePurchaseOrderAsync(int id);

        // Business Logic Operations
        Task<bool> SendPurchaseOrderEmailAsync(int id);
        Task<bool> CancelPurchaseOrderAsync(int id);
        Task<bool> UpdateStatusAsync(int id, PurchaseOrderStatus status);

        // Query Operations
        Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersBySupplierAsync(int supplierId);
        Task<IEnumerable<PurchaseOrder>> GetPurchaseOrdersByStatusAsync(PurchaseOrderStatus status);
        Task<IEnumerable<PurchaseOrder>> GetPendingPurchaseOrdersAsync();

        // Validation Operations
        Task<bool> ValidatePurchaseOrderAsync(PurchaseOrderViewModel viewModel);
        Task<string> GenerateNextPONumberAsync();
        Task<bool> IsPONumberUniqueAsync(string poNumber);
        Task<bool> CanEditPurchaseOrderAsync(int id);
        Task<bool> CanSendPurchaseOrderAsync(int id);
        Task<bool> CanCancelPurchaseOrderAsync(int id);

        // ViewModel Operations
        Task<PurchaseOrderViewModel> GetPurchaseOrderViewModelAsync(int? id = null);
        Task<PurchaseOrderViewModel> PopulatePurchaseOrderViewModelAsync(PurchaseOrderViewModel viewModel);

        // Calculation Operations
        Task<decimal> CalculateTotalAmountAsync(IEnumerable<PurchaseOrderDetailViewModel> details);
        Task<PurchaseOrder> RecalculateTotalAsync(PurchaseOrder purchaseOrder);

        // Email Operations
        Task<string> GeneratePurchaseOrderEmailContentAsync(PurchaseOrder purchaseOrder);
        Task<bool> IsEmailSentAsync(int id);
        Task MarkEmailAsSentAsync(int id);
        Task<bool> MarkAsClosedAsync(int id);
    }
}