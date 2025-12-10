using WMS.Models.ViewModels;

namespace WMS.Services
{
    /// <summary>
    /// Interface untuk Report Service
    /// Handles report generation for Admin (Inbound, Outbound, Inventory)
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Generate Inbound report (PO + ASN + Putaway) dengan filtering lengkap
        /// </summary>
        Task<InboundReportData> GenerateInboundReportAsync(
            InboundReportRequest request,
            int companyId);

        /// <summary>
        /// Generate Outbound report (SO + Picking)
        /// </summary>
        Task<OutboundReportData> GenerateOutboundReportAsync(OutboundReportRequest request, int companyId);

        /// <summary>
        /// Generate Inventory Movement report
        /// </summary>
        Task<InventoryMovementReportData> GenerateInventoryMovementReportAsync(InventoryMovementReportRequest request, int companyId);

        /// <summary>
        /// Generate Stock report (aggregated per item)
        /// </summary>
        Task<StockReportData> GenerateStockReportAsync(StockReportRequest request, int companyId);

        /// <summary>
        /// Generate Supplier report
        /// </summary>
        Task<SupplierReportData> GenerateSupplierReportAsync(SupplierReportRequest request, int companyId);

        /// <summary>
        /// Generate Customer report
        /// </summary>
        Task<CustomerReportData> GenerateCustomerReportAsync(CustomerReportRequest request, int companyId);

        /// <summary>
        /// Export report to Excel
        /// </summary>
        Task<byte[]> ExportToExcelAsync(ReportExportRequest request, int companyId);

        /// <summary>
        /// Export report to PDF dengan filtering
        /// </summary>
        Task<byte[]> ExportToPdfAsync(ReportExportRequest request, int companyId);

    }
}

