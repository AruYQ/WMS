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
        /// Generate Inbound report (PO + ASN)
        /// </summary>
        Task<InboundReportData> GenerateInboundReportAsync(DateTime fromDate, DateTime toDate, int companyId);

        /// <summary>
        /// Generate Outbound report (SO + Picking)
        /// </summary>
        Task<OutboundReportData> GenerateOutboundReportAsync(DateTime fromDate, DateTime toDate, int companyId);

        /// <summary>
        /// Generate Inventory Movement report
        /// </summary>
        Task<InventoryMovementReportData> GenerateInventoryMovementReportAsync(DateTime fromDate, DateTime toDate, int companyId);

        /// <summary>
        /// Export report to Excel
        /// </summary>
        Task<byte[]> ExportToExcelAsync(string reportType, DateTime fromDate, DateTime toDate, int companyId);

        /// <summary>
        /// Export report to PDF
        /// </summary>
        Task<byte[]> ExportToPdfAsync(string reportType, DateTime fromDate, DateTime toDate, int companyId);
    }
}

