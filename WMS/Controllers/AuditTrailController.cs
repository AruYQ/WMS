using Microsoft.AspNetCore.Mvc;
using WMS.Attributes;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller for Audit Trail viewing
    /// Hybrid MVC + API pattern
    /// Admin & WarehouseStaff can view
    /// </summary>
    [RequirePermission(Constants.AUDIT_VIEW)]
    public class AuditTrailController : Controller
    {
        private readonly IAuditTrailService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AuditTrailController> _logger;

        public AuditTrailController(
            IAuditTrailService auditService,
            ICurrentUserService currentUserService,
            ILogger<AuditTrailController> logger)
        {
            _auditService = auditService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        #region MVC Actions

        /// <summary>
        /// GET: /AuditTrail
        /// Audit trail viewer page
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        #endregion

        #region API Endpoints

        /// <summary>
        /// GET: api/audittrail
        /// Get audit logs with pagination and filtering
        /// </summary>
        [HttpGet("api/audittrail")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditSearchRequest request)
        {
            try
            {
                var result = await _auditService.GetAuditLogsAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs");
                return StatusCode(500, new { success = false, message = "Error loading audit logs" });
            }
        }

        /// <summary>
        /// GET: api/audittrail/{id}
        /// Get audit log details
        /// </summary>
        [HttpGet("api/audittrail/{id}")]
        public async Task<IActionResult> GetAuditLogDetails(int id)
        {
            try
            {
                var auditLog = await _auditService.GetAuditLogDetailsAsync(id);
                if (auditLog == null)
                {
                    return NotFound(new { success = false, message = "Audit log not found" });
                }

                return Ok(new { success = true, data = auditLog });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit log {AuditLogId}", id);
                return StatusCode(500, new { success = false, message = "Error loading audit log details" });
            }
        }

        /// <summary>
        /// GET: api/audittrail/statistics
        /// Get audit statistics for dashboard
        /// </summary>
        [HttpGet("api/audittrail/statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context" });
                }

                var stats = await _auditService.GetAuditStatisticsAsync(companyId.Value, fromDate, toDate);
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit statistics");
                return StatusCode(500, new { success = false, message = "Error loading statistics" });
            }
        }

        /// <summary>
        /// GET: api/audittrail/user-activities
        /// Get recent activities for current user
        /// </summary>
        [HttpGet("api/audittrail/user-activities")]
        public async Task<IActionResult> GetUserActivities([FromQuery] int take = 10)
        {
            try
            {
                var userId = _currentUserService.UserId;
                if (!userId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var activities = await _auditService.GetUserRecentActivitiesAsync(userId.Value, take);
                return Ok(new { success = true, data = activities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user activities");
                return StatusCode(500, new { success = false, message = "Error loading activities" });
            }
        }

        #endregion
    }
}

