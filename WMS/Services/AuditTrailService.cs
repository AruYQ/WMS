using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WMS.Data;
using WMS.Models;
using WMS.Models.ViewModels;

namespace WMS.Services
{
    /// <summary>
    /// Service untuk audit trail logging dan retrieval
    /// </summary>
    public class AuditTrailService : IAuditTrailService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditTrailService> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditTrailService(
            ApplicationDbContext context,
            ILogger<AuditTrailService> logger,
            ICurrentUserService currentUserService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Log an action to audit trail
        /// </summary>
        public async Task LogActionAsync(string action, string module, int? entityId = null, string? entityDescription = null,
            object? oldValue = null, object? newValue = null, string? notes = null, bool isSuccess = true)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                
                var auditLog = new AuditLog
                {
                    CompanyId = _currentUserService.CompanyId,
                    UserId = _currentUserService.UserId,
                    Username = _currentUserService.Username,
                    Action = action,
                    Module = module,
                    EntityId = entityId,
                    EntityDescription = entityDescription,
                    OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
                    NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
                    IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                    Timestamp = DateTime.Now,
                    Notes = notes,
                    IsSuccess = isSuccess,
                    CreatedDate = DateTime.Now,
                    CreatedBy = _currentUserService.Username
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit action: {Action} {Module}", action, module);
                // Don't throw - audit logging should never break the application
            }
        }

        /// <summary>
        /// Get audit logs with filtering and pagination
        /// </summary>
        public async Task<AuditLogPagedResponse> GetAuditLogsAsync(AuditSearchRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                
                var query = _context.AuditLogs
                    .Where(a => a.CompanyId == companyId || companyId == null) // SuperAdmin can see all
                    .AsQueryable();

                // Apply filters
                if (request.FromDate.HasValue)
                    query = query.Where(a => a.Timestamp >= request.FromDate.Value);

                if (request.ToDate.HasValue)
                {
                    // Include entire end date (23:59:59)
                    var endDate = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(a => a.Timestamp <= endDate);
                }

                if (!string.IsNullOrEmpty(request.Username))
                    query = query.Where(a => a.Username.Contains(request.Username));

                if (!string.IsNullOrEmpty(request.Action))
                    query = query.Where(a => a.Action == request.Action);

                if (!string.IsNullOrEmpty(request.Module))
                    query = query.Where(a => a.Module == request.Module);

                if (request.IsSuccessOnly.HasValue && request.IsSuccessOnly.Value)
                    query = query.Where(a => a.IsSuccess);

                // Count total
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize);

                // Get page data
                var items = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(a => new AuditLogDto
                    {
                        Id = a.Id,
                        Username = a.Username,
                        Action = a.Action,
                        Module = a.Module,
                        EntityId = a.EntityId,
                        EntityDescription = a.EntityDescription,
                        Timestamp = a.Timestamp,
                        IsSuccess = a.IsSuccess,
                        Notes = a.Notes,
                        BadgeColor = a.Action == "CREATE" ? "success" : 
                                     a.Action == "UPDATE" ? "info" : 
                                     a.Action == "DELETE" ? "danger" : 
                                     a.Action == "VIEW" ? "secondary" : 
                                     a.Action == "EXPORT" ? "primary" : "light",
                        DisplayText = a.Action + " " + a.Module + (a.EntityDescription != null ? " - " + a.EntityDescription : "")
                    })
                    .ToListAsync();

                return new AuditLogPagedResponse
                {
                    Items = items,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    HasPrevious = request.Page > 1,
                    HasNext = request.Page < totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs");
                return new AuditLogPagedResponse();
            }
        }

        /// <summary>
        /// Get audit log details by ID
        /// </summary>
        public async Task<AuditLogDetailResponse?> GetAuditLogDetailsAsync(int id)
        {
            try
            {
                var auditLog = await _context.AuditLogs
                    .Include(a => a.Company)
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (auditLog == null)
                    return null;

                return new AuditLogDetailResponse
                {
                    Id = auditLog.Id,
                    CompanyId = auditLog.CompanyId,
                    CompanyName = auditLog.Company?.Name,
                    UserId = auditLog.UserId,
                    Username = auditLog.Username,
                    Action = auditLog.Action,
                    Module = auditLog.Module,
                    EntityId = auditLog.EntityId,
                    EntityDescription = auditLog.EntityDescription,
                    OldValue = auditLog.OldValue,
                    NewValue = auditLog.NewValue,
                    IpAddress = auditLog.IpAddress,
                    UserAgent = auditLog.UserAgent,
                    Timestamp = auditLog.Timestamp,
                    Notes = auditLog.Notes,
                    IsSuccess = auditLog.IsSuccess
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit log details for ID: {Id}", id);
                return null;
            }
        }

        /// <summary>
        /// Get audit statistics for dashboard
        /// </summary>
        public async Task<AuditStatistics> GetAuditStatisticsAsync(int companyId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Include entire end date (23:59:59)
                var endDate = toDate.Date.AddDays(1).AddTicks(-1);
                
                var logs = await _context.AuditLogs
                    .Where(a => a.CompanyId == companyId &&
                                a.Timestamp >= fromDate &&
                                a.Timestamp <= endDate)
                    .ToListAsync();

                var stats = new AuditStatistics
                {
                    TotalActions = logs.Count,
                    TotalUsers = logs.Select(a => a.UserId).Distinct().Count(),
                    CreateActions = logs.Count(a => a.Action == "CREATE"),
                    UpdateActions = logs.Count(a => a.Action == "UPDATE"),
                    DeleteActions = logs.Count(a => a.Action == "DELETE"),
                    FailedActions = logs.Count(a => !a.IsSuccess),
                    TopModules = logs
                        .GroupBy(a => a.Module)
                        .Select(g => new AuditStatistics.ModuleActivity
                        {
                            Module = g.Key,
                            Count = g.Count()
                        })
                        .OrderByDescending(m => m.Count)
                        .Take(5)
                        .ToList()
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit statistics for company {CompanyId}", companyId);
                return new AuditStatistics();
            }
        }

        /// <summary>
        /// Get recent activities for a user
        /// </summary>
        public async Task<List<AuditLogDto>> GetUserRecentActivitiesAsync(int userId, int take = 10)
        {
            try
            {
                var logs = await _context.AuditLogs
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(take)
                    .Select(a => new AuditLogDto
                    {
                        Id = a.Id,
                        Username = a.Username,
                        Action = a.Action,
                        Module = a.Module,
                        EntityId = a.EntityId,
                        EntityDescription = a.EntityDescription,
                        Timestamp = a.Timestamp,
                        IsSuccess = a.IsSuccess,
                        Notes = a.Notes,
                        BadgeColor = a.Action == "CREATE" ? "success" : 
                                     a.Action == "UPDATE" ? "info" : 
                                     a.Action == "DELETE" ? "danger" : 
                                     a.Action == "VIEW" ? "secondary" : 
                                     a.Action == "EXPORT" ? "primary" : "light",
                        DisplayText = a.Action + " " + a.Module + (a.EntityDescription != null ? " - " + a.EntityDescription : "")
                    })
                    .ToListAsync();

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user recent activities for user {UserId}", userId);
                return new List<AuditLogDto>();
            }
        }
    }
}

