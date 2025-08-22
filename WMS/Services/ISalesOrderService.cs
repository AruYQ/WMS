using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Service interface untuk Sales Order management
    /// "The Climax" - dimana warehouse fee diterapkan dan stok dikurangi
    /// </summary>
    public interface ISalesOrderService
    {
        // Basic CRUD Operations
        Task<IEnumerable<SalesOrder>> GetAllSalesOrdersAsync();
        Task<SalesOrder?> GetSalesOrderByIdAsync(int id);
        Task<SalesOrder> CreateSalesOrderAsync(SalesOrderViewModel viewModel);
        Task<SalesOrder> UpdateSalesOrderAsync(int id, SalesOrderViewModel viewModel);
        Task<bool> DeleteSalesOrderAsync(int id);

        // Business Logic Operations
        Task<bool> UpdateStatusAsync(int id, SalesOrderStatus status);
        Task<bool> ConfirmSalesOrderAsync(int id);
        Task<bool> ShipSalesOrderAsync(int id);
        Task<bool> CompleteSalesOrderAsync(int id);
        Task<bool> CancelSalesOrderAsync(int id);

        // Stock Management Operations
        Task<bool> ValidateStockAvailabilityAsync(SalesOrderViewModel viewModel);
        Task<Dictionary<int, int>> CheckItemStockAsync(IEnumerable<SalesOrderDetailViewModel> details);
        Task<bool> ReserveStockAsync(int salesOrderId);
        Task<bool> ReleaseReservedStockAsync(int salesOrderId);
        Task<bool> ProcessStockReductionAsync(int salesOrderId);

        // Query Operations
        Task<IEnumerable<SalesOrder>> GetSalesOrdersByCustomerAsync(int customerId);
        Task<IEnumerable<SalesOrder>> GetSalesOrdersByStatusAsync(SalesOrderStatus status);
        Task<IEnumerable<SalesOrder>> GetConfirmedSalesOrdersAsync();
        Task<IEnumerable<SalesOrder>> GetPendingSalesOrdersAsync();

        // Validation Operations
        Task<bool> ValidateSalesOrderAsync(SalesOrderViewModel viewModel);
        Task<string> GenerateNextSONumberAsync();
        Task<bool> IsSONumberUniqueAsync(string soNumber);
        Task<bool> CanEditSalesOrderAsync(int id);
        Task<bool> CanConfirmSalesOrderAsync(int id);
        Task<bool> CanShipSalesOrderAsync(int id);
        Task<bool> CanCompleteSalesOrderAsync(int id);
        Task<bool> CanCancelSalesOrderAsync(int id);

        // ViewModel Operations
        Task<SalesOrderViewModel> GetSalesOrderViewModelAsync(int? id = null);
        Task<SalesOrderViewModel> PopulateSalesOrderViewModelAsync(SalesOrderViewModel viewModel);
        Task<SalesOrderViewModel> ValidateAndPopulateStockInfoAsync(SalesOrderViewModel viewModel);

        // Warehouse Fee Operations
        Task<SalesOrder> CalculateWarehouseFeesAsync(SalesOrder salesOrder);
        Task<SalesOrderDetail> CalculateWarehouseFeeForDetailAsync(SalesOrderDetail detail);
        Task<decimal> GetWarehouseFeeForItemAsync(int itemId);
        Task<SalesOrder> RecalculateAllFeesAndTotalsAsync(SalesOrder salesOrder);

        // Financial Operations
        Task<decimal> CalculateTotalAmountAsync(IEnumerable<SalesOrderDetailViewModel> details);
        Task<decimal> CalculateTotalWarehouseFeeAsync(IEnumerable<SalesOrderDetailViewModel> details);
        Task<decimal> CalculateGrandTotalAsync(decimal totalAmount, decimal totalWarehouseFee);

        // Reporting Operations
        Task<Dictionary<string, object>> GetSalesOrderSummaryAsync(int salesOrderId);
        Task<Dictionary<string, decimal>> GetWarehouseFeeRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<IEnumerable<object>> GetTopSellingItemsAsync(int topCount = 10);
        Task<Dictionary<string, object>> GetSalesStatisticsAsync();
    }
}