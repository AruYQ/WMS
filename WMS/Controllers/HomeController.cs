using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Data;
using WMS.Utilities;

namespace WMS.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly IASNService _asnService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IPurchaseOrderService purchaseOrderService,
            IASNService asnService,
            ICurrentUserService currentUserService,
            ApplicationDbContext context,
            ILogger<HomeController> logger)
        {
            _purchaseOrderService = purchaseOrderService;
            _asnService = asnService;
            _currentUserService = currentUserService;
            _context = context;
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

                // Verify company context exists (skip for SuperAdmin)
                if (!_currentUserService.CompanyId.HasValue && !_currentUserService.IsInRole("SuperAdmin"))
                {
                    _logger.LogWarning("User {UserId} has no company context, redirecting to login", _currentUserService.UserId);
                    return RedirectToAction("Login", "Account");
                }

                var dashboardViewModel = new DashboardViewModel();
                var isSuperAdmin = _currentUserService.IsInRole("SuperAdmin");
                var isAdmin = _currentUserService.IsInRole("Admin");
                var isWarehouseStaff = _currentUserService.IsInRole("WarehouseStaff");
                var companyId = _currentUserService.CompanyId;
                var today = DateTime.Today;
                var todayEnd = today.AddDays(1);

                if (isSuperAdmin)
                {
                    dashboardViewModel.IsSuperAdminView = true;
                    await PopulateSuperAdminOverview(dashboardViewModel);

                    dashboardViewModel.LastRefresh = DateTime.Now;
                    ViewBag.CompanyName = "All Companies";
                    ViewBag.IsAdmin = false;
                    ViewBag.IsWarehouseStaff = false;
                    ViewBag.IsSuperAdmin = true;

                    return View(dashboardViewModel);
                }

                if (!companyId.HasValue)
                {
                    _logger.LogWarning("User {UserId} missing company context for non-SuperAdmin role", _currentUserService.UserId);
                    return RedirectToAction("Login", "Account");
                }

                dashboardViewModel.IsSuperAdminView = false;

                _logger.LogInformation("Dashboard loaded for User {UserId}, Company {CompanyId}, Role {Role}", 
                    _currentUserService.UserId, companyId.Value, 
                    isAdmin ? "Admin" : isWarehouseStaff ? "WarehouseStaff" : "User");

                // ===== INBOUND OPERATIONS STATISTICS =====
                await PopulateInboundStatistics(dashboardViewModel, companyId.Value, today, todayEnd, isAdmin, isWarehouseStaff);

                // ===== OUTBOUND OPERATIONS STATISTICS =====
                await PopulateOutboundStatistics(dashboardViewModel, companyId.Value, today, todayEnd, isAdmin, isWarehouseStaff);

                // ===== INVENTORY STATISTICS =====
                await PopulateInventoryStatistics(dashboardViewModel, companyId.Value, isAdmin);

                // ===== MASTER DATA STATISTICS (Admin only) =====
                if (isAdmin)
                {
                    await PopulateMasterDataStatistics(dashboardViewModel, companyId.Value);
                }

                // ===== OPERATIONAL INSIGHTS =====
                await PopulateOperationalInsights(dashboardViewModel, companyId.Value, today, todayEnd);

                // ===== RECENT ACTIVITIES =====
                await PopulateRecentActivities(dashboardViewModel, companyId.Value, isAdmin, isWarehouseStaff);

                // ===== SYSTEM ALERTS =====
                await PopulateSystemAlerts(dashboardViewModel, companyId.Value);

                dashboardViewModel.LastRefresh = DateTime.Now;
                ViewBag.CompanyName = companyId.Value;
                ViewBag.IsAdmin = isAdmin;
                ViewBag.IsWarehouseStaff = isWarehouseStaff;
                ViewBag.IsSuperAdmin = false;

                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard for user {UserId}", _currentUserService.UserId);
                return View(new DashboardViewModel());
            }
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

        #region Statistics Population Methods

        private async Task PopulateSuperAdminOverview(DashboardViewModel dashboard)
        {
            try
            {
                var companies = await _context.Companies
                    .OrderBy(c => c.Name)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Code,
                        c.IsActive,
                        c.MaxUsers
                    })
                    .ToListAsync();

                var userData = await _context.Users
                    .Where(u => !u.IsDeleted && u.CompanyId.HasValue)
                    .Select(u => new
                    {
                        u.Id,
                        CompanyId = u.CompanyId!.Value,
                        u.FullName,
                        u.Username,
                        u.IsActive,
                        u.LastLoginDate,
                        CompanyName = u.Company != null ? u.Company.Name : string.Empty,
                        CompanyCode = u.Company != null ? u.Company.Code : string.Empty
                    })
                    .ToListAsync();

                dashboard.CompanyUserSummaries = companies
                    .Select(company =>
                    {
                        var companyUsers = userData
                            .Where(user => user.CompanyId == company.Id)
                            .ToList();

                        var activeUsers = companyUsers.Count(user => user.IsActive);
                        var totalUsers = companyUsers.Count;
                        var inactiveUsers = totalUsers - activeUsers;
                        var latestLoginUser = companyUsers
                            .Where(user => user.LastLoginDate.HasValue)
                            .OrderByDescending(user => user.LastLoginDate)
                            .FirstOrDefault();

                        return new CompanyUserSummary
                        {
                            CompanyId = company.Id,
                            CompanyName = company.Name,
                            CompanyCode = company.Code,
                            IsActive = company.IsActive,
                            MaxUsers = company.MaxUsers,
                            TotalUsers = totalUsers,
                            ActiveUsers = activeUsers,
                            InactiveUsers = inactiveUsers,
                            LastLoginDate = latestLoginUser?.LastLoginDate,
                            LastLoginUser = latestLoginUser?.FullName ?? "-"
                        };
                    })
                    .ToList();

                dashboard.RecentLoginSummaries = userData
                    .Where(user => user.LastLoginDate.HasValue)
                    .OrderByDescending(user => user.LastLoginDate)
                    .Take(15)
                    .Select(user => new UserLoginSummary
                    {
                        UserId = user.Id,
                        FullName = user.FullName,
                        Username = user.Username,
                        CompanyName = user.CompanyName,
                        CompanyCode = user.CompanyCode,
                        LastLoginDate = user.LastLoginDate
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error populating SuperAdmin dashboard overview");
                dashboard.CompanyUserSummaries = new List<CompanyUserSummary>();
                dashboard.RecentLoginSummaries = new List<UserLoginSummary>();
            }
        }

        private async Task PopulateInboundStatistics(DashboardViewModel dashboard, int companyId, DateTime today, DateTime todayEnd, bool isAdmin, bool isWarehouseStaff)
        {
            try
            {
                // Purchase Order Statistics
                var allPOs = await _context.PurchaseOrders
                    .Where(po => po.CompanyId == companyId && !po.IsDeleted)
                    .ToListAsync();

                dashboard.InboundStats.TotalPurchaseOrders = allPOs.Count;
                dashboard.InboundStats.DraftPOs = allPOs.Count(po => po.Status == Constants.PO_STATUS_DRAFT);
                dashboard.InboundStats.SentPOs = allPOs.Count(po => po.Status == Constants.PO_STATUS_SENT);
                dashboard.InboundStats.ReceivedPOs = allPOs.Count(po => po.Status == Constants.PO_STATUS_RECEIVED);
                dashboard.InboundStats.CompletedPOs = allPOs.Count(po => po.Status == Constants.PO_STATUS_COMPLETED);
                dashboard.InboundStats.CancelledPOs = allPOs.Count(po => po.Status == Constants.PO_STATUS_CANCELLED);
                dashboard.InboundStats.TotalPurchaseValue = allPOs.Sum(po => po.TotalAmount);
                dashboard.InboundStats.TodaysPOs = allPOs.Count(po => po.CreatedDate >= today && po.CreatedDate < todayEnd);
                dashboard.InboundStats.TodaysPurchaseValue = allPOs.Where(po => po.CreatedDate >= today && po.CreatedDate < todayEnd)
                    .Sum(po => po.TotalAmount);
                dashboard.InboundStats.AverageOrderValue = allPOs.Any() 
                    ? allPOs.Sum(po => po.TotalAmount) / allPOs.Count 
                    : 0;

                // ASN Statistics
                var allASNs = await _context.AdvancedShippingNotices
                    .Where(asn => asn.CompanyId == companyId && !asn.IsDeleted)
                    .ToListAsync();

                dashboard.InboundStats.TotalASNs = allASNs.Count;
                dashboard.InboundStats.PendingASNs = allASNs.Count(asn => asn.Status == Constants.ASN_STATUS_PENDING);
                dashboard.InboundStats.InTransitASNs = allASNs.Count(asn => asn.Status == Constants.ASN_STATUS_IN_TRANSIT);
                dashboard.InboundStats.ArrivedASNs = allASNs.Count(asn => asn.Status == Constants.ASN_STATUS_ARRIVED);
                dashboard.InboundStats.ProcessedASNs = allASNs.Count(asn => asn.Status == Constants.ASN_STATUS_PROCESSED);
                dashboard.InboundStats.CompletedASNs = allASNs.Count(asn => asn.Status == Constants.ASN_STATUS_COMPLETED);
                dashboard.InboundStats.CancelledASNs = allASNs.Count(asn => asn.Status == Constants.ASN_STATUS_CANCELLED);
                dashboard.InboundStats.TodaysArrivedASNs = allASNs.Count(asn => 
                    asn.Status == Constants.ASN_STATUS_ARRIVED && 
                    asn.ExpectedArrivalDate.HasValue && 
                    asn.ExpectedArrivalDate.Value >= today && 
                    asn.ExpectedArrivalDate.Value < todayEnd);

                // Putaway Statistics
                var putawayInventories = await _context.Inventories
                    .Where(inv => inv.CompanyId == companyId && 
                                 !inv.IsDeleted && 
                                 inv.Status == Constants.INVENTORY_STATUS_AVAILABLE &&
                                 inv.Location.Category == Constants.LOCATION_CATEGORY_OTHER)
                    .ToListAsync();

                dashboard.InboundStats.ItemsWaitingPutaway = putawayInventories.Sum(inv => inv.Quantity);
                dashboard.InboundStats.TodaysPutawayCompleted = await _context.Inventories
                    .Where(inv => inv.CompanyId == companyId && 
                                 !inv.IsDeleted && 
                                 inv.LastUpdated >= today && 
                                 inv.LastUpdated < todayEnd &&
                                 inv.Location.Category == Constants.LOCATION_CATEGORY_STORAGE)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error populating inbound statistics");
            }
        }

        private async Task PopulateOutboundStatistics(DashboardViewModel dashboard, int companyId, DateTime today, DateTime todayEnd, bool isAdmin, bool isWarehouseStaff)
        {
            try
            {
                // Sales Order Statistics
                var allSOs = await _context.SalesOrders
                    .Where(so => so.CompanyId == companyId && !so.IsDeleted)
                    .ToListAsync();

                dashboard.OutboundStats.TotalSalesOrders = allSOs.Count;
                dashboard.OutboundStats.DraftSOs = allSOs.Count(so => so.Status == Constants.SO_STATUS_DRAFT || so.Status == "Pending");
                dashboard.OutboundStats.PendingSOs = allSOs.Count(so => so.Status == "Pending");
                dashboard.OutboundStats.InProgressSOs = allSOs.Count(so => so.Status == "In Progress");
                dashboard.OutboundStats.PickedSOs = allSOs.Count(so => so.Status == "Picked");
                dashboard.OutboundStats.ShippedSOs = allSOs.Count(so => so.Status == Constants.SO_STATUS_SHIPPED);
                dashboard.OutboundStats.CompletedSOs = allSOs.Count(so => so.Status == Constants.SO_STATUS_COMPLETED);
                dashboard.OutboundStats.CancelledSOs = allSOs.Count(so => so.Status == Constants.SO_STATUS_CANCELLED);
                dashboard.OutboundStats.TotalSalesValue = allSOs.Sum(so => so.TotalAmount);
                dashboard.OutboundStats.TodaysSOs = allSOs.Count(so => so.CreatedDate >= today && so.CreatedDate < todayEnd);
                dashboard.OutboundStats.TodaysSalesValue = allSOs.Where(so => so.CreatedDate >= today && so.CreatedDate < todayEnd)
                    .Sum(so => so.TotalAmount);
                dashboard.OutboundStats.AverageOrderValue = allSOs.Any() 
                    ? allSOs.Sum(so => so.TotalAmount) / allSOs.Count 
                    : 0;

                // Picking Statistics
                var allPickings = await _context.Pickings
                    .Where(p => p.CompanyId == companyId && !p.IsDeleted)
                    .ToListAsync();

                dashboard.OutboundStats.TotalPickings = allPickings.Count;
                dashboard.OutboundStats.PendingPickings = allPickings.Count(p => p.Status == Constants.PICKING_STATUS_PENDING);
                dashboard.OutboundStats.InProgressPickings = allPickings.Count(p => p.Status == Constants.PICKING_STATUS_IN_PROGRESS || p.Status == "In Progress");
                dashboard.OutboundStats.CompletedPickings = allPickings.Count(p => p.Status == Constants.PICKING_STATUS_COMPLETED);
                dashboard.OutboundStats.CancelledPickings = allPickings.Count(p => p.Status == Constants.PICKING_STATUS_CANCELLED);
                dashboard.OutboundStats.TodaysCompletedPickings = allPickings.Count(p => 
                    p.Status == Constants.PICKING_STATUS_COMPLETED && 
                    p.CompletedDate.HasValue && 
                    p.CompletedDate.Value >= today && 
                    p.CompletedDate.Value < todayEnd);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error populating outbound statistics");
            }
        }

        private async Task PopulateInventoryStatistics(DashboardViewModel dashboard, int companyId, bool isAdmin)
        {
            try
            {
                // Overall Inventory
                var allItems = await _context.Items
                    .Where(i => i.CompanyId == companyId && !i.IsDeleted)
                    .ToListAsync();

                dashboard.InventoryStats.TotalItems = allItems.Count;
                dashboard.InventoryStats.ActiveItems = allItems.Count(i => i.IsActive);
                dashboard.InventoryStats.ItemsWithStock = 0;
                dashboard.InventoryStats.ItemsWithoutStock = 0;

                var inventories = await _context.Inventories
                    .Where(inv => inv.CompanyId == companyId && !inv.IsDeleted)
                    .Include(inv => inv.Item)
                    .Include(inv => inv.Location)
                    .ToListAsync();

                dashboard.InventoryStats.TotalInventoryRecords = inventories.Count;
                dashboard.InventoryStats.TotalInventoryValue = inventories.Sum(inv => inv.Quantity * (inv.Item?.StandardPrice ?? 0));

                // Stock Level Metrics
                var itemsWithStockDict = inventories
                    .Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE && inv.Quantity > 0)
                    .GroupBy(inv => inv.ItemId)
                    .ToDictionary(g => g.Key, g => g.Sum(inv => inv.Quantity));

                dashboard.InventoryStats.ItemsInStock = itemsWithStockDict.Count;
                dashboard.InventoryStats.ItemsWithStock = itemsWithStockDict.Count;
                dashboard.InventoryStats.ItemsWithoutStock = allItems.Count - itemsWithStockDict.Count;

                dashboard.InventoryStats.LowStockItems = itemsWithStockDict.Count(kv => kv.Value <= Constants.LOW_STOCK_THRESHOLD && kv.Value > Constants.CRITICAL_STOCK_THRESHOLD);
                dashboard.InventoryStats.CriticalStockItems = itemsWithStockDict.Count(kv => kv.Value <= Constants.CRITICAL_STOCK_THRESHOLD && kv.Value > 0);
                dashboard.InventoryStats.OutOfStockItems = allItems.Count - itemsWithStockDict.Count;

                // Inventory by Location Category
                dashboard.InventoryStats.StorageLocationsValue = inventories
                    .Where(inv => inv.Location.Category == Constants.LOCATION_CATEGORY_STORAGE)
                    .Sum(inv => inv.Quantity * (inv.Item?.StandardPrice ?? 0));

                dashboard.InventoryStats.HoldingLocationsValue = inventories
                    .Where(inv => inv.Location.Category == Constants.LOCATION_CATEGORY_OTHER)
                    .Sum(inv => inv.Quantity * (inv.Item?.StandardPrice ?? 0));

                // Inventory Status Breakdown
                dashboard.InventoryStats.AvailableItems = inventories.Count(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE);
                dashboard.InventoryStats.ReservedItems = inventories.Count(inv => inv.Status == Constants.INVENTORY_STATUS_RESERVED);
                dashboard.InventoryStats.DamagedItems = inventories.Count(inv => inv.Status == Constants.INVENTORY_STATUS_DAMAGED);
                dashboard.InventoryStats.QuarantineItems = inventories.Count(inv => inv.Status == Constants.INVENTORY_STATUS_QUARANTINE);
                dashboard.InventoryStats.BlockedItems = inventories.Count(inv => inv.Status == Constants.INVENTORY_STATUS_BLOCKED);

                // Storage Locations Utilization
                var storageLocations = await _context.Locations
                    .Where(l => l.CompanyId == companyId && 
                               !l.IsDeleted && 
                               l.Category == Constants.LOCATION_CATEGORY_STORAGE)
                    .ToListAsync();

                if (storageLocations.Any())
                {
                    dashboard.InventoryStats.StorageLocationsUtilization = storageLocations
                        .Where(l => l.MaxCapacity > 0)
                        .Average(l => (double)l.CurrentCapacity / l.MaxCapacity * 100);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error populating inventory statistics");
            }
        }

        private async Task PopulateMasterDataStatistics(DashboardViewModel dashboard, int companyId)
        {
            try
            {
                // Items
                var items = await _context.Items
                    .Where(i => i.CompanyId == companyId && !i.IsDeleted)
                    .ToListAsync();

                dashboard.MasterDataStats.TotalItems = items.Count;
                dashboard.MasterDataStats.ActiveItems = items.Count(i => i.IsActive);
                dashboard.MasterDataStats.InactiveItems = items.Count(i => !i.IsActive);

                var itemsWithStock = await _context.Inventories
                    .Where(inv => inv.CompanyId == companyId && 
                                 !inv.IsDeleted && 
                                 inv.Status == Constants.INVENTORY_STATUS_AVAILABLE && 
                                 inv.Quantity > 0)
                    .Select(inv => inv.ItemId)
                    .Distinct()
                    .ToListAsync();

                dashboard.MasterDataStats.ItemsWithStock = itemsWithStock.Count;
                dashboard.MasterDataStats.ItemsWithoutStock = items.Count - itemsWithStock.Count;

                // Locations
                var locations = await _context.Locations
                    .Where(l => l.CompanyId == companyId && !l.IsDeleted)
                    .ToListAsync();

                dashboard.MasterDataStats.TotalLocations = locations.Count;
                dashboard.MasterDataStats.StorageLocations = locations.Count(l => l.Category == Constants.LOCATION_CATEGORY_STORAGE);
                dashboard.MasterDataStats.HoldingLocations = locations.Count(l => l.Category == Constants.LOCATION_CATEGORY_OTHER);
                dashboard.MasterDataStats.ActiveLocations = locations.Count(l => l.IsActive);

                var fullLocations = locations.Where(l => l.IsFull || (l.MaxCapacity > 0 && l.CurrentCapacity >= l.MaxCapacity)).ToList();
                dashboard.MasterDataStats.FullLocations = fullLocations.Count;

                var locationsWithCapacity = locations.Where(l => l.MaxCapacity > 0).ToList();
                if (locationsWithCapacity.Any())
                {
                    dashboard.MasterDataStats.AverageCapacityUtilization = locationsWithCapacity
                        .Average(l => (double)l.CurrentCapacity / l.MaxCapacity * 100);
                    dashboard.MasterDataStats.NearCapacityLocations = locationsWithCapacity
                        .Count(l => (double)l.CurrentCapacity / l.MaxCapacity >= 0.8 && (double)l.CurrentCapacity / l.MaxCapacity < 1.0);
                }

                // Customers
                var customers = await _context.Customers
                    .Where(c => c.CompanyId == companyId && !c.IsDeleted)
                    .Include(c => c.SalesOrders)
                    .ToListAsync();

                dashboard.MasterDataStats.TotalCustomers = customers.Count;
                dashboard.MasterDataStats.ActiveCustomers = customers.Count(c => c.IsActive);
                dashboard.MasterDataStats.CustomersWithOrders = customers.Count(c => c.SalesOrders.Any());

                // Suppliers
                var suppliers = await _context.Suppliers
                    .Where(s => s.CompanyId == companyId && !s.IsDeleted)
                    .Include(s => s.PurchaseOrders)
                    .ToListAsync();

                dashboard.MasterDataStats.TotalSuppliers = suppliers.Count;
                dashboard.MasterDataStats.ActiveSuppliers = suppliers.Count(s => s.IsActive);
                dashboard.MasterDataStats.SuppliersWithOrders = suppliers.Count(s => s.PurchaseOrders.Any());

                // Users
                var users = await _context.Users
                    .Where(u => u.CompanyId == companyId && !u.IsDeleted)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .ToListAsync();

                dashboard.MasterDataStats.TotalUsers = users.Count;
                dashboard.MasterDataStats.ActiveUsers = users.Count(u => u.IsActive);
                dashboard.MasterDataStats.AdminUsers = users.Count(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == Constants.Roles.Admin));
                dashboard.MasterDataStats.WarehouseStaffUsers = users.Count(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == Constants.Roles.WarehouseStaff));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error populating master data statistics");
            }
        }

        private async Task PopulateOperationalInsights(DashboardViewModel dashboard, int companyId, DateTime today, DateTime todayEnd)
        {
            try
            {
                // Pending Actions
                var draftPOs = await _context.PurchaseOrders
                    .Where(po => po.CompanyId == companyId && 
                                !po.IsDeleted && 
                                po.Status == Constants.PO_STATUS_DRAFT)
                    .CountAsync();

                var arrivedASNs = await _context.AdvancedShippingNotices
                    .Where(asn => asn.CompanyId == companyId && 
                                 !asn.IsDeleted && 
                                 asn.Status == Constants.ASN_STATUS_ARRIVED)
                    .CountAsync();

                var pickedSOs = await _context.SalesOrders
                    .Where(so => so.CompanyId == companyId && 
                               !so.IsDeleted && 
                               so.Status == "Picked")
                    .CountAsync();

                var itemsToPutaway = await _context.Inventories
                    .Where(inv => inv.CompanyId == companyId && 
                                 !inv.IsDeleted && 
                                 inv.Status == Constants.INVENTORY_STATUS_AVAILABLE &&
                                 inv.Location.Category == Constants.LOCATION_CATEGORY_OTHER)
                    .CountAsync();

                var lowStockItems = await _context.Inventories
                    .Where(inv => inv.CompanyId == companyId && 
                                 !inv.IsDeleted && 
                                 inv.Status == Constants.INVENTORY_STATUS_AVAILABLE)
                    .GroupBy(inv => inv.ItemId)
                    .Select(g => new { ItemId = g.Key, TotalQuantity = g.Sum(inv => inv.Quantity) })
                    .CountAsync(x => x.TotalQuantity <= Constants.LOW_STOCK_THRESHOLD && x.TotalQuantity > Constants.CRITICAL_STOCK_THRESHOLD);

                var locations = await _context.Locations
                    .Where(l => l.CompanyId == companyId && 
                               !l.IsDeleted && 
                               l.MaxCapacity > 0)
                    .ToListAsync();

                var overCapacityLocations = locations.Count(l => l.CurrentCapacity >= l.MaxCapacity);

                var pendingPickings = await _context.Pickings
                    .Where(p => p.CompanyId == companyId && 
                               !p.IsDeleted && 
                               p.Status == Constants.PICKING_STATUS_PENDING)
                    .CountAsync();

                dashboard.PendingActions.PurchaseOrdersToSend = draftPOs;
                dashboard.PendingActions.ASNsToProcess = arrivedASNs;
                dashboard.PendingActions.SalesOrdersToShip = pickedSOs;
                dashboard.PendingActions.ItemsToPutaway = itemsToPutaway;
                dashboard.PendingActions.LowStockAlerts = lowStockItems;
                dashboard.PendingActions.OverCapacityLocations = overCapacityLocations;
                dashboard.PendingActions.PendingPickings = pendingPickings;

                // Today's Performance
                // Note: AdvancedShippingNotice doesn't have ProcessedDate, using ModifiedDate instead
                // ModifiedDate is updated when status changes to Processed
                dashboard.OperationalInsights.TodaysProcessedASN = await _context.AdvancedShippingNotices
                    .Where(asn => asn.CompanyId == companyId && 
                             !asn.IsDeleted && 
                             asn.Status == Constants.ASN_STATUS_PROCESSED &&
                             asn.ModifiedDate.HasValue &&
                             asn.ModifiedDate.Value.Date == today)
                    .CountAsync();

                dashboard.OperationalInsights.TodaysPutawayCompleted = await _context.Inventories
                    .Where(inv => inv.CompanyId == companyId && 
                                 !inv.IsDeleted && 
                                 inv.LastUpdated >= today && 
                                 inv.LastUpdated < todayEnd &&
                                 inv.Location.Category == Constants.LOCATION_CATEGORY_STORAGE)
                    .CountAsync();

                dashboard.OperationalInsights.TodaysCompletedPicking = await _context.Pickings
                    .Where(p => p.CompanyId == companyId && 
                               !p.IsDeleted && 
                               p.Status == Constants.PICKING_STATUS_COMPLETED &&
                               p.CompletedDate.HasValue &&
                               p.CompletedDate.Value >= today && 
                               p.CompletedDate.Value < todayEnd)
                    .CountAsync();

                dashboard.OperationalInsights.TodaysShippedSO = await _context.SalesOrders
                    .Where(so => so.CompanyId == companyId && 
                               !so.IsDeleted && 
                               so.Status == Constants.SO_STATUS_SHIPPED &&
                               so.ModifiedDate >= today && 
                               so.ModifiedDate < todayEnd)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error populating operational insights");
            }
        }

        private async Task PopulateRecentActivities(DashboardViewModel dashboard, int companyId, bool isAdmin, bool isWarehouseStaff)
        {
            var activities = new List<RecentActivity>();

            try
            {
                // Recent Purchase Orders
                var recentPOs = await _context.PurchaseOrders
                    .Where(po => po.CompanyId == companyId && !po.IsDeleted)
                    .Include(po => po.Supplier)
                    .OrderByDescending(po => po.CreatedDate)
                    .Take(5)
                    .ToListAsync();

                foreach (var po in recentPOs)
                {
                    activities.Add(new RecentActivity
                    {
                        ActivityDate = po.CreatedDate,
                        ActivityType = "PO_CREATED",
                        Title = "Purchase Order Created",
                        Description = $"PO {po.PONumber} created for {po.Supplier?.Name ?? "Unknown"}",
                        ReferenceNumber = po.PONumber,
                        CreatedBy = po.CreatedBy ?? "System",
                        IconClass = "fas fa-shopping-cart",
                        BadgeClass = "badge bg-primary"
                    });
                }

                // Recent ASNs
                var recentASNs = await _context.AdvancedShippingNotices
                    .Where(asn => asn.CompanyId == companyId && !asn.IsDeleted)
                    .Include(asn => asn.PurchaseOrder)
                    .OrderByDescending(asn => asn.CreatedDate)
                    .Take(5)
                    .ToListAsync();

                foreach (var asn in recentASNs)
                {
                    activities.Add(new RecentActivity
                    {
                        ActivityDate = asn.CreatedDate,
                        ActivityType = "ASN_CREATED",
                        Title = "ASN Created",
                        Description = $"ASN {asn.ASNNumber} created for PO {asn.PurchaseOrder?.PONumber ?? "Unknown"}",
                        ReferenceNumber = asn.ASNNumber,
                        CreatedBy = asn.CreatedBy ?? "System",
                        IconClass = "fas fa-truck",
                        BadgeClass = "badge bg-info"
                    });
                }

                // Recent Sales Orders
                if (isAdmin || isWarehouseStaff)
                {
                    var recentSOs = await _context.SalesOrders
                        .Where(so => so.CompanyId == companyId && !so.IsDeleted)
                        .Include(so => so.Customer)
                        .OrderByDescending(so => so.CreatedDate)
                        .Take(5)
                        .ToListAsync();

                    foreach (var so in recentSOs)
                    {
                        activities.Add(new RecentActivity
                        {
                            ActivityDate = so.CreatedDate,
                            ActivityType = "SO_CREATED",
                            Title = "Sales Order Created",
                            Description = $"SO {so.SONumber} created for {so.Customer?.Name ?? "Unknown"}",
                            ReferenceNumber = so.SONumber,
                            CreatedBy = so.CreatedBy ?? "System",
                            IconClass = "fas fa-receipt",
                            BadgeClass = "badge bg-success"
                        });
                    }
                }

                // Recent Pickings
                if (isAdmin || isWarehouseStaff)
                {
                    var recentPickings = await _context.Pickings
                        .Where(p => p.CompanyId == companyId && !p.IsDeleted)
                        .Include(p => p.SalesOrder)
                        .OrderByDescending(p => p.CreatedDate)
                        .Take(5)
                        .ToListAsync();

                    foreach (var picking in recentPickings)
                    {
                        activities.Add(new RecentActivity
                        {
                            ActivityDate = picking.CreatedDate,
                            ActivityType = "PICKING_CREATED",
                            Title = "Picking Created",
                            Description = $"Picking {picking.PickingNumber} created for SO {picking.SalesOrder?.SONumber ?? "Unknown"}",
                            ReferenceNumber = picking.PickingNumber,
                            CreatedBy = picking.CreatedBy ?? "System",
                            IconClass = "fas fa-clipboard-list",
                            BadgeClass = "badge bg-warning"
                        });
                    }
                }

                dashboard.RecentActivities = activities
                    .OrderByDescending(a => a.ActivityDate)
                    .Take(dashboard.MaxRecentActivities)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error populating recent activities for user {UserId}", _currentUserService.UserId);
            }
        }

        private async Task PopulateSystemAlerts(DashboardViewModel dashboard, int companyId)
        {
            var alerts = new List<SystemAlert>();

            try
            {
                // Draft POs
                var draftPOs = await _context.PurchaseOrders
                    .Where(po => po.CompanyId == companyId && 
                                !po.IsDeleted && 
                                po.Status == Constants.PO_STATUS_DRAFT)
                    .CountAsync();

                if (draftPOs > 0)
                {
                    alerts.Add(new SystemAlert
                    {
                        Level = NotificationLevel.Warning,
                        Title = "Draft Purchase Orders",
                        Message = $"You have {draftPOs} draft purchase orders that need to be sent.",
                        ActionUrl = "/PurchaseOrder?status=Draft",
                        ActionText = "View Draft POs",
                        CreatedDate = DateTime.Now
                    });
                }

                // Arrived ASNs
                var arrivedASNs = await _context.AdvancedShippingNotices
                    .Where(asn => asn.CompanyId == companyId && 
                                 !asn.IsDeleted && 
                                 asn.Status == Constants.ASN_STATUS_ARRIVED)
                    .CountAsync();

                if (arrivedASNs > 0)
                {
                    alerts.Add(new SystemAlert
                    {
                        Level = NotificationLevel.Info,
                        Title = "ASNs Ready for Processing",
                        Message = $"You have {arrivedASNs} ASNs that have arrived and are ready for processing.",
                        ActionUrl = "/ASN?status=Arrived",
                        ActionText = "Process ASNs",
                        CreatedDate = DateTime.Now
                    });
                }

                // Picked SOs to Ship
                var pickedSOs = await _context.SalesOrders
                    .Where(so => so.CompanyId == companyId && 
                               !so.IsDeleted && 
                               so.Status == "Picked")
                    .CountAsync();

                if (pickedSOs > 0)
                {
                    alerts.Add(new SystemAlert
                    {
                        Level = NotificationLevel.Info,
                        Title = "Sales Orders Ready to Ship",
                        Message = $"You have {pickedSOs} sales orders that are picked and ready to ship.",
                        ActionUrl = "/SalesOrder?status=Picked",
                        ActionText = "View Picked SOs",
                        CreatedDate = DateTime.Now
                    });
                }

                // Low Stock Alerts
                var lowStockCount = await _context.Inventories
                    .Where(inv => inv.CompanyId == companyId && 
                                 !inv.IsDeleted && 
                                 inv.Status == Constants.INVENTORY_STATUS_AVAILABLE)
                    .GroupBy(inv => inv.ItemId)
                    .Select(g => new { ItemId = g.Key, TotalQuantity = g.Sum(inv => inv.Quantity) })
                    .CountAsync(x => x.TotalQuantity <= Constants.LOW_STOCK_THRESHOLD && x.TotalQuantity > 0);

                if (lowStockCount > 0)
                {
                    alerts.Add(new SystemAlert
                    {
                        Level = NotificationLevel.Warning,
                        Title = "Low Stock Alert",
                        Message = $"You have {lowStockCount} items with low stock levels that need attention.",
                        ActionUrl = "/Inventory?stockLevel=Low",
                        ActionText = "View Low Stock Items",
                        CreatedDate = DateTime.Now
                    });
                }

                dashboard.SystemAlerts = alerts;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error populating system alerts for user {UserId}", _currentUserService.UserId);
            }
        }

        #endregion
    }
}
