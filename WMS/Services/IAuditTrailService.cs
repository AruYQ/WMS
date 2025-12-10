using WMS.Models.ViewModels;

namespace WMS.Services
{
    /// <summary>
    /// Interface untuk Audit Trail Service
    /// Handles audit logging and retrieval
    /// </summary>
    public interface IAuditTrailService
    {
        /// <summary>
        /// Log an action to audit trail
        /// </summary>
        Task LogActionAsync(string action, string module, int? entityId = null, string? entityDescription = null, 
            object? oldValue = null, object? newValue = null, string? notes = null, bool isSuccess = true);

        /// <summary>
        /// Get audit logs with filtering and pagination
        /// </summary>
        Task<AuditLogPagedResponse> GetAuditLogsAsync(AuditSearchRequest request);

        /// <summary>
        /// Get audit log details by ID
        /// </summary>
        Task<AuditLogDetailResponse?> GetAuditLogDetailsAsync(int id);

        /// <summary>
        /// Get audit statistics for dashboard
        /// </summary>
        Task<AuditStatistics> GetAuditStatisticsAsync(int companyId, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get recent activities for a user
        /// </summary>
        Task<List<AuditLogDto>> GetUserRecentActivitiesAsync(int userId, int take = 10);

        /// <summary>
        /// Get unique actions and modules for filter dropdowns
        /// </summary>
        Task<Dictionary<string, List<string>>> GetUniqueActionsAndModulesAsync(int? companyId = null);
    }
}

