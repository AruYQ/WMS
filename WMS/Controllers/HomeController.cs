using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Models.ViewModels;
using WMS.Services;

namespace WMS.Controllers
{
    [Authorize] // ADD THIS - Explicitly require authentication
    public class HomeController : Controller
    {
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly IASNService _asnService;
        private readonly ICurrentUserService _currentUserService; // ADD THIS
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IPurchaseOrderService purchaseOrderService,
            IASNService asnService,
            ICurrentUserService currentUserService, // ADD THIS
            ILogger<HomeController> logger)
        {
            _purchaseOrderService = purchaseOrderService;
            _asnService = asnService;
            _currentUserService = currentUserService; // ADD THIS
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Verify user is still authenticated and get company context
                if (!_currentUserService.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Account");
                }

                var dashboardViewModel = new DashboardViewModel();

                // Get basic statistics (these should be filtered by company automatically)
                var allPOs = await _purchaseOrderService.GetAllPurchaseOrdersAsync();
                var pendingPOs = await _purchaseOrderService.GetPendingPurchaseOrdersAsync();
                var allASNs = await _asnService.GetAllASNsAsync();
                var arrivedASNs = await _asnService.GetArrivedASNsAsync();

                // Populate KPI data
                dashboardViewModel.KPI.TotalPurchaseOrders = allPOs.Count();
                dashboardViewModel.KPI.PendingPurchaseOrders = pendingPOs.Count();
                dashboardViewModel.KPI.TotalPurchaseValue = allPOs.Sum(po => po.TotalAmount);
                dashboardViewModel.KPI.TotalASNs = allASNs.Count();
                dashboardViewModel.KPI.PendingASNs = arrivedASNs.Count();

                // Populate pending actions
                dashboardViewModel.PendingActions.PurchaseOrdersToSend = pendingPOs.Count(po => po.Status == "Draft");
                dashboardViewModel.PendingActions.ASNsToProcess = arrivedASNs.Count();

                // Add recent activities
                await PopulateRecentActivities(dashboardViewModel);

                // Add system alerts
                await PopulateSystemAlerts(dashboardViewModel);

                dashboardViewModel.LastRefresh = DateTime.Now;

                // Add user and company context to ViewBag for layout
               
                ViewBag.CompanyName = _currentUserService.CompanyId;

                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard for user {UserId}",
                    _currentUserService.UserId);
                return View(new DashboardViewModel());
            }
        }

        [AllowAnonymous] // This can be accessed without login
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous] // Error pages should be accessible
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

        private async Task PopulateRecentActivities(DashboardViewModel dashboard)
        {
            var activities = new List<RecentActivity>();

            try
            {
                // Get recent POs (filtered by company automatically in service)
                var recentPOs = (await _purchaseOrderService.GetAllPurchaseOrdersAsync())
                    .OrderByDescending(po => po.CreatedDate)
                    .Take(5);

                foreach (var po in recentPOs)
                {
                    activities.Add(new RecentActivity
                    {
                        ActivityDate = po.CreatedDate,
                        ActivityType = "PO_CREATED",
                        Title = "Purchase Order Created",
                        Description = $"PO {po.PONumber} created for {po.Supplier?.Name}",
                        ReferenceNumber = po.PONumber,
                        CreatedBy = po.CreatedBy ?? "System",
                        IconClass = "fas fa-shopping-cart",
                        BadgeClass = "badge bg-primary"
                    });
                }

                // Get recent ASNs (filtered by company automatically in service)
                var recentASNs = (await _asnService.GetAllASNsAsync())
                    .OrderByDescending(asn => asn.CreatedDate)
                    .Take(5);

                foreach (var asn in recentASNs)
                {
                    activities.Add(new RecentActivity
                    {
                        ActivityDate = asn.CreatedDate,
                        ActivityType = "ASN_CREATED",
                        Title = "ASN Created",
                        Description = $"ASN {asn.ASNNumber} created for PO {asn.PONumberDisplay}",
                        ReferenceNumber = asn.ASNNumber,
                        CreatedBy = asn.CreatedBy ?? "System",
                        IconClass = "fas fa-truck",
                        BadgeClass = "badge bg-info"
                    });
                }

                dashboard.RecentActivities = activities
                    .OrderByDescending(a => a.ActivityDate)
                    .Take(dashboard.MaxRecentActivities)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error populating recent activities for user {UserId}",
                    _currentUserService.UserId);
            }
        }

        private async Task PopulateSystemAlerts(DashboardViewModel dashboard)
        {
            var alerts = new List<SystemAlert>();

            try
            {
                // Check for draft POs that haven't been sent (company filtered automatically)
                var draftPOs = await _purchaseOrderService.GetPurchaseOrdersByStatusAsync(WMS.Utilities.PurchaseOrderStatus.Draft);
                if (draftPOs.Any())
                {
                    alerts.Add(new SystemAlert
                    {
                        Level = WMS.Utilities.NotificationLevel.Warning,
                        Title = "Draft Purchase Orders",
                        Message = $"You have {draftPOs.Count()} draft purchase orders that need to be sent.",
                        ActionUrl = "/PurchaseOrder?status=Draft",
                        ActionText = "View Draft POs",
                        CreatedDate = DateTime.Now
                    });
                }

                // Check for arrived ASNs that need processing (company filtered automatically)
                var arrivedASNs = await _asnService.GetArrivedASNsAsync();
                if (arrivedASNs.Any())
                {
                    alerts.Add(new SystemAlert
                    {
                        Level = WMS.Utilities.NotificationLevel.Info,
                        Title = "ASNs Ready for Processing",
                        Message = $"You have {arrivedASNs.Count()} ASNs that have arrived and are ready for processing.",
                        ActionUrl = "/ASN?status=Arrived",
                        ActionText = "Process ASNs",
                        CreatedDate = DateTime.Now
                    });
                }

                dashboard.SystemAlerts = alerts;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error populating system alerts for user {UserId}",
                    _currentUserService.UserId);
            }
        }
    }
}