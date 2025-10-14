using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Request model untuk search audit logs
    /// </summary>
    public class AuditSearchRequest
    {
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Username")]
        public string? Username { get; set; }

        [Display(Name = "Action")]
        public string? Action { get; set; }

        [Display(Name = "Module")]
        public string? Module { get; set; }

        [Display(Name = "Success Only")]
        public bool? IsSuccessOnly { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// DTO untuk audit log list
    /// </summary>
    public class AuditLogDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? EntityDescription { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsSuccess { get; set; }
        public string? Notes { get; set; }
        public string BadgeColor { get; set; } = "secondary";
        public string DisplayText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detail response untuk audit log
    /// </summary>
    public class AuditLogDetailResponse
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public int? UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? EntityDescription { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Notes { get; set; }
        public bool IsSuccess { get; set; }
    }

    /// <summary>
    /// Response model dengan pagination
    /// </summary>
    public class AuditLogPagedResponse
    {
        public List<AuditLogDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
    }

    /// <summary>
    /// Statistics untuk audit dashboard
    /// </summary>
    public class AuditStatistics
    {
        public int TotalActions { get; set; }
        public int TotalUsers { get; set; }
        public int CreateActions { get; set; }
        public int UpdateActions { get; set; }
        public int DeleteActions { get; set; }
        public int FailedActions { get; set; }
        public List<ModuleActivity> TopModules { get; set; } = new();

        public class ModuleActivity
        {
            public string Module { get; set; } = string.Empty;
            public int Count { get; set; }
        }
    }
}

