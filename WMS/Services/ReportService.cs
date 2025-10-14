using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models.ViewModels;

namespace WMS.Services
{
    /// <summary>
    /// Service untuk report generation
    /// Creates Inbound, Outbound, and Inventory reports for Admin
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            ApplicationDbContext context,
            ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Generate Inbound report (PO + ASN)
        /// </summary>
        public async Task<InboundReportData> GenerateInboundReportAsync(DateTime fromDate, DateTime toDate, int companyId)
        {
            try
            {
                var report = new InboundReportData
                {
                    FromDate = fromDate,
                    ToDate = toDate
                };

                // Get Purchase Orders
                var purchaseOrders = await _context.PurchaseOrders
                    .Where(po => po.CompanyId == companyId && 
                                 po.OrderDate >= fromDate && 
                                 po.OrderDate <= toDate)
                    .Include(po => po.Supplier)
                    .Include(po => po.PurchaseOrderDetails)
                    .ToListAsync();

                report.TotalPurchaseOrders = purchaseOrders.Count;
                report.TotalValue = purchaseOrders.Sum(po => po.TotalAmount);

                foreach (var po in purchaseOrders.OrderBy(p => p.OrderDate))
                {
                    report.Lines.Add(new InboundReportData.InboundReportLine
                    {
                        Date = po.OrderDate,
                        DocumentNumber = po.PONumber,
                        Type = "PO",
                        SupplierName = po.Supplier?.Name ?? "Unknown",
                        Status = po.Status,
                        TotalItems = po.PurchaseOrderDetails.Count,
                        TotalAmount = po.TotalAmount
                    });
                }

                // Get ASNs
                var asns = await _context.AdvancedShippingNotices
                    .Where(asn => asn.CompanyId == companyId &&
                                  asn.ExpectedArrivalDate >= fromDate &&
                                  asn.ExpectedArrivalDate <= toDate)
                    .Include(asn => asn.PurchaseOrder)
                        .ThenInclude(po => po!.Supplier)
                    .Include(asn => asn.ASNDetails)
                    .ToListAsync();

                report.TotalASN = asns.Count;
                report.TotalReceived = asns.Count(asn => asn.Status == "Completed" || asn.Status == "Processed");

                foreach (var asn in asns.OrderBy(a => a.ExpectedArrivalDate ?? a.ShipmentDate))
                {
                    report.Lines.Add(new InboundReportData.InboundReportLine
                    {
                        Date = asn.ExpectedArrivalDate ?? asn.ShipmentDate,
                        DocumentNumber = asn.ASNNumber,
                        Type = "ASN",
                        SupplierName = asn.PurchaseOrder?.Supplier?.Name ?? "Unknown",
                        Status = asn.Status,
                        TotalItems = asn.ASNDetails.Count,
                        TotalAmount = asn.ASNDetails.Sum(d => d.ShippedQuantity * d.ActualPricePerItem)
                    });
                }

                report.Lines = report.Lines.OrderBy(l => l.Date).ToList();

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inbound report for company {CompanyId}", companyId);
                throw;
            }
        }

        /// <summary>
        /// Generate Outbound report (SO + Picking)
        /// </summary>
        public async Task<OutboundReportData> GenerateOutboundReportAsync(DateTime fromDate, DateTime toDate, int companyId)
        {
            try
            {
                var report = new OutboundReportData
                {
                    FromDate = fromDate,
                    ToDate = toDate
                };

                // Get Sales Orders
                var salesOrders = await _context.SalesOrders
                    .Where(so => so.CompanyId == companyId &&
                                 so.OrderDate >= fromDate &&
                                 so.OrderDate <= toDate)
                    .Include(so => so.Customer)
                    .Include(so => so.SalesOrderDetails)
                    .ToListAsync();

                report.TotalSalesOrders = salesOrders.Count;
                report.TotalValue = salesOrders.Sum(so => so.TotalAmount);
                report.TotalShipped = salesOrders.Count(so => so.Status == "Shipped" || so.Status == "Completed");

                foreach (var so in salesOrders.OrderBy(s => s.OrderDate))
                {
                    report.Lines.Add(new OutboundReportData.OutboundReportLine
                    {
                        Date = so.OrderDate,
                        DocumentNumber = so.SONumber,
                        Type = "SO",
                        CustomerName = so.Customer?.Name ?? "Unknown",
                        Status = so.Status,
                        TotalItems = so.SalesOrderDetails.Count,
                        TotalAmount = so.TotalAmount
                    });
                }

                // Get Pickings
                var pickings = await _context.Pickings
                    .Where(p => p.CompanyId == companyId &&
                                p.PickingDate >= fromDate &&
                                p.PickingDate <= toDate)
                    .Include(p => p.SalesOrder)
                        .ThenInclude(so => so!.Customer)
                    .Include(p => p.PickingDetails)
                    .ToListAsync();

                report.TotalPickings = pickings.Count;

                foreach (var picking in pickings.OrderBy(p => p.PickingDate))
                {
                    report.Lines.Add(new OutboundReportData.OutboundReportLine
                    {
                        Date = picking.PickingDate,
                        DocumentNumber = picking.PickingNumber,
                        Type = "Picking",
                        CustomerName = picking.SalesOrder?.Customer?.Name ?? "Unknown",
                        Status = picking.Status,
                        TotalItems = picking.PickingDetails.Count,
                        TotalAmount = 0 // Picking doesn't have amount
                    });
                }

                report.Lines = report.Lines.OrderBy(l => l.Date).ToList();

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating outbound report for company {CompanyId}", companyId);
                throw;
            }
        }

        /// <summary>
        /// Generate Inventory Movement report
        /// </summary>
        public async Task<InventoryMovementReportData> GenerateInventoryMovementReportAsync(DateTime fromDate, DateTime toDate, int companyId)
        {
            try
            {
                var report = new InventoryMovementReportData
                {
                    FromDate = fromDate,
                    ToDate = toDate
                };

                // For now, return empty report
                // TODO: Implement inventory movement tracking in future
                _logger.LogWarning("Inventory movement report not yet implemented");

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory movement report for company {CompanyId}", companyId);
                throw;
            }
        }

        /// <summary>
        /// Export report to Excel (using ClosedXML)
        /// </summary>
        public async Task<byte[]> ExportToExcelAsync(string reportType, DateTime fromDate, DateTime toDate, int companyId)
        {
            try
            {
                // TODO: Implement Excel export using ClosedXML
                _logger.LogWarning("Excel export not yet implemented");
                await Task.CompletedTask; // Fix async warning
                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to Excel");
                throw;
            }
        }

        /// <summary>
        /// Export report to PDF
        /// </summary>
        public async Task<byte[]> ExportToPdfAsync(string reportType, DateTime fromDate, DateTime toDate, int companyId)
        {
            try
            {
                // TODO: Implement PDF export
                _logger.LogWarning("PDF export not yet implemented");
                await Task.CompletedTask; // Fix async warning
                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to PDF");
                throw;
            }
        }
    }
}

