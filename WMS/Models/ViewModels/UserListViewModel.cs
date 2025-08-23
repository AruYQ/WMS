using System.ComponentModel.DataAnnotations;
using WMS.Data.Repositories;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// ViewModel untuk user list/index page
    /// </summary>
    public class UserListViewModel
    {
        /// <summary>
        /// Search term untuk filtering
        /// </summary>
        [Display(Name = "Cari User")]
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filter by role
        /// </summary>
        [Display(Name = "Filter Role")]
        public string? RoleFilter { get; set; }

        /// <summary>
        /// Filter by status
        /// </summary>
        [Display(Name = "Filter Status")]
        public string? StatusFilter { get; set; }

        /// <summary>
        /// Paged result users
        /// </summary>
        public PagedResult<UserViewModel> Users { get; set; } = new PagedResult<UserViewModel>();

        /// <summary>
        /// Available roles untuk filter dropdown
        /// </summary>
        public List<RoleViewModel> AvailableRoles { get; set; } = new List<RoleViewModel>();

        /// <summary>
        /// Available status options
        /// </summary>
        public List<StatusOption> StatusOptions { get; set; } = new List<StatusOption>
        {
            new StatusOption { Value = "", Text = "Semua Status" },
            new StatusOption { Value = "active", Text = "Aktif" },
            new StatusOption { Value = "inactive", Text = "Tidak Aktif" }
        };

        /// <summary>
        /// Pagination info
        /// </summary>
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// Company summary untuk header
        /// </summary>
        public CompanySummaryViewModel CompanySummary { get; set; } = new CompanySummaryViewModel();
    }

    /// <summary>
    /// Status option untuk dropdown
    /// </summary>
    public class StatusOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Company summary untuk user list header
    /// </summary>
    public class CompanySummaryViewModel
    {
        public string CompanyName { get; set; } = string.Empty;
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int MaxUsers { get; set; }
        public string SubscriptionPlan { get; set; } = string.Empty;
        public DateTime? SubscriptionEndDate { get; set; }

        /// <summary>
        /// Percentage of user slots used
        /// </summary>
        public double UserUsagePercentage => MaxUsers > 0 ? (double)TotalUsers / MaxUsers * 100 : 0;

        /// <summary>
        /// Is company near user limit?
        /// </summary>
        public bool IsNearUserLimit => UserUsagePercentage >= 80;

        /// <summary>
        /// Is subscription expiring soon?
        /// </summary>
        public bool IsSubscriptionExpiringSoon =>
            SubscriptionEndDate.HasValue && SubscriptionEndDate.Value <= DateTime.Now.AddDays(30);
    }
}