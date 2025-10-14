using Microsoft.AspNetCore.Mvc;
using WMS.Attributes;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller for Report generation (Admin only)
    /// Hybrid MVC + API pattern
    /// </summary>
    [RequirePermission(Constants.REPORT_CREATE)]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            IReportService reportService,
            ICurrentUserService currentUserService,
            ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        #region MVC Actions

        /// <summary>
        /// GET: /Report
        /// Report generation page
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        #endregion

        #region API Endpoints

        /// <summary>
        /// GET: api/report/inbound
        /// Generate inbound report (PO + ASN)
        /// </summary>
        [HttpGet("api/report/inbound")]
        public async Task<IActionResult> GetInboundReport([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context" });
                }

                var report = await _reportService.GenerateInboundReportAsync(fromDate, toDate, companyId.Value);
                return Ok(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inbound report");
                return StatusCode(500, new { success = false, message = "Error generating report" });
            }
        }

        /// <summary>
        /// GET: api/report/outbound
        /// Generate outbound report (SO + Picking)
        /// </summary>
        [HttpGet("api/report/outbound")]
        public async Task<IActionResult> GetOutboundReport([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context" });
                }

                var report = await _reportService.GenerateOutboundReportAsync(fromDate, toDate, companyId.Value);
                return Ok(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating outbound report");
                return StatusCode(500, new { success = false, message = "Error generating report" });
            }
        }

        /// <summary>
        /// GET: api/report/inventory-movement
        /// Generate inventory movement report
        /// </summary>
        [HttpGet("api/report/inventory-movement")]
        public async Task<IActionResult> GetInventoryMovementReport([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context" });
                }

                var report = await _reportService.GenerateInventoryMovementReportAsync(fromDate, toDate, companyId.Value);
                return Ok(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory movement report");
                return StatusCode(500, new { success = false, message = "Error generating report" });
            }
        }

        /// <summary>
        /// POST: api/report/export
        /// Export report to Excel/PDF
        /// </summary>
        [HttpPost("api/report/export")]
        public async Task<IActionResult> ExportReport([FromBody] ReportExportRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context" });
                }

                byte[] fileData;
                string contentType;
                string fileName;

                if (request.Format.ToLower() == "excel")
                {
                    fileData = await _reportService.ExportToExcelAsync(request.ReportType, request.FromDate, request.ToDate, companyId.Value);
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    fileName = $"{request.ReportType}_Report_{DateTime.Now:yyyyMMdd}.xlsx";
                }
                else if (request.Format.ToLower() == "pdf")
                {
                    fileData = await _reportService.ExportToPdfAsync(request.ReportType, request.FromDate, request.ToDate, companyId.Value);
                    contentType = "application/pdf";
                    fileName = $"{request.ReportType}_Report_{DateTime.Now:yyyyMMdd}.pdf";
                }
                else
                {
                    return BadRequest(new { success = false, message = "Invalid format" });
                }

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report");
                return StatusCode(500, new { success = false, message = "Error exporting report" });
            }
        }

        #endregion
    }
}

