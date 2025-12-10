using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WMS.Attributes;
using WMS.Data;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;
using System.ComponentModel.DataAnnotations;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller untuk Inventory management - Hybrid MVC + API
    /// </summary>
    [RequirePermission(Constants.INVENTORY_VIEW)]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly IItemService _itemService;
        private readonly IASNService _asnService;
        private readonly IAuditTrailService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(
            ApplicationDbContext context,
            IInventoryService inventoryService,
            IItemService itemService,
            IASNService asnService,
            IAuditTrailService auditService,
            ICurrentUserService currentUserService,
            ILogger<InventoryController> logger)
        {
            _context = context;
            _inventoryService = inventoryService;
            _itemService = itemService;
            _asnService = asnService;
            _auditService = auditService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        #region MVC Actions

        /// <summary>
        /// GET: /Inventory
        /// Inventory management index page
        /// </summary>
        [HttpGet]
        [Route("Inventory")]
        [Route("Inventory/Index")]
        public IActionResult Index()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Inventory index page");
                return View("Error");
            }
        }

        /// <summary>
        /// GET: /Inventory/Putaway
        /// Putaway operations page
        /// </summary>
        [HttpGet]
        [Route("Inventory/Putaway")]
        [RequirePermission(Constants.INVENTORY_VIEW)]
        public IActionResult Putaway()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Putaway page");
                return View("Error");
            }
        }

        /// <summary>
        /// GET: /Inventory/ProcessPutaway
        /// Process putaway for specific ASN
        /// </summary>
        [RequirePermission(Constants.INVENTORY_VIEW)]
        public async Task<IActionResult> ProcessPutaway(int asnId, bool bulk = false)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;
                
                // Load ASN data with related entities
                var asn = await _context.AdvancedShippingNotices
                    .Where(a => a.Id == asnId && a.CompanyId == companyId && !a.IsDeleted)
                    .Include(a => a.PurchaseOrder)
                        .ThenInclude(po => po.Supplier)
                    .Include(a => a.ASNDetails)
                        .ThenInclude(ad => ad.Item)
                    .FirstOrDefaultAsync();

                if (asn == null)
                {
                    _logger.LogWarning("ASN {ASNId} not found for company {CompanyId}", asnId, companyId);
                    return NotFound("ASN not found");
                }

                // Load available locations for the company
                // Only show Storage locations for putaway (not holding locations)
                var locations = await _context.Locations
                    .Where(l => l.CompanyId == companyId && 
                               !l.IsDeleted && 
                               l.IsActive &&
                               l.Category == Constants.LOCATION_CATEGORY_STORAGE)
                    .OrderBy(l => l.Code)
                    .ToListAsync();

                // Load current inventory capacity for each location (actual inventory in database)
                var locationCapacityData = await _context.Inventories
                    .Where(i => i.CompanyId == companyId && !i.IsDeleted)
                    .GroupBy(i => i.LocationId)
                    .Select(g => new { LocationId = g.Key, CurrentCapacity = g.Sum(i => i.Quantity) })
                    .ToListAsync();

                var locationCapacityDict = locationCapacityData.ToDictionary(x => x.LocationId, x => x.CurrentCapacity);

                // Populate PutawayViewModel
                var viewModel = new PutawayViewModel
                {
                    ASNId = asn.Id,
                    ASNNumber = asn.ASNNumber,
                    PONumber = asn.PurchaseOrder?.PONumber ?? "N/A",
                    SupplierName = asn.PurchaseOrder?.Supplier?.Name ?? "N/A",
                    ShipmentDate = asn.ShipmentDate,
                    ProcessedDate = asn.ActualArrivalDate,
                    AvailableLocations = new SelectList(locations.Select(l => new { 
                        Id = l.Id, 
                        DisplayName = $"{l.Code} - {l.Name}" 
                    }), "Id", "DisplayName"),
                    LocationDropdownItems = locations.Select(l => {
                        var actualCurrentCapacity = locationCapacityDict.GetValueOrDefault(l.Id, 0);
                        return new LocationDropdownItem
                        {
                            Id = l.Id,
                            Code = l.Code,
                            Name = l.Name,
                            MaxCapacity = l.MaxCapacity,
                            CurrentCapacity = actualCurrentCapacity, // Updated with actual inventory
                            AvailableCapacity = Math.Max(0, l.MaxCapacity - actualCurrentCapacity),
                            DisplayText = $"{l.Code} - {l.Name}",
                            CssClass = actualCurrentCapacity >= l.MaxCapacity ? "text-danger" : 
                                       actualCurrentCapacity > l.MaxCapacity * 0.8 ? "text-warning" : "text-success",
                            StatusText = "Available",
                            CanAccommodate = (l.MaxCapacity - actualCurrentCapacity) > 0,
                            IsFull = actualCurrentCapacity >= l.MaxCapacity,
                            CapacityPercentage = l.MaxCapacity > 0 ? (double)actualCurrentCapacity / l.MaxCapacity * 100 : 0
                        };
                    }).ToList(),
                    PutawayDetails = asn.ASNDetails.Select(ad => new PutawayDetailViewModel
                        {
                            ASNId = ad.ASNId,
                            ASNDetailId = ad.Id,
                            ItemId = ad.ItemId,
                            ItemCode = ad.Item.ItemCode,
                            ItemName = ad.Item.Name,
                            ItemUnit = ad.Item.Unit,
                            TotalQuantity = ad.ShippedQuantity,
                            AlreadyPutAwayQuantity = ad.AlreadyPutAwayQuantity,
                            RemainingQuantity = ad.RemainingQuantity,
                            ActualPricePerItem = ad.ActualPricePerItem,
                            QuantityToPutaway = ad.RemainingQuantity > 0 ? Math.Min(ad.RemainingQuantity, 1) : 0,
                            LocationId = 0, // Will be set by user selection
                            Notes = string.Empty,
                            ItemDisplay = $"{ad.Item.ItemCode} - {ad.Item.Name}",
                            IsCompleted = ad.RemainingQuantity == 0,
                            CanPutaway = ad.RemainingQuantity > 0
                        }).ToList()
                };

                _logger.LogInformation("ProcessPutaway loaded for ASN {ASNNumber} with {ItemCount} items", 
                    asn.ASNNumber, viewModel.PutawayDetails.Count);

                return View("ProcessPutaway", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ProcessPutaway page for ASN {ASNId}", asnId);
                return View("Error");
            }
        }

        #endregion

        #region Dashboard & Statistics

        /// <summary>
        /// GET: api/Inventory/Dashboard
        /// Get Inventory dashboard statistics
        /// </summary>
        [HttpGet("api/Inventory/Dashboard")]
        [RequirePermission(Constants.INVENTORY_VIEW)]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var totalItems = await _context.Inventories
                    .Where(i => i.CompanyId == companyId && !i.IsDeleted)
                    .CountAsync();

                var availableStock = await _context.Inventories
                    .Where(i => i.CompanyId == companyId && !i.IsDeleted && i.Status == "Available" && i.Quantity > 0)
                    .SumAsync(i => (int)i.Quantity);

                var lowStockItems = await _context.Inventories
                    .Where(i => i.CompanyId == companyId && !i.IsDeleted && i.Quantity <= 10)
                    .CountAsync();

                var outOfStockItems = await _context.Inventories
                    .Where(i => i.CompanyId == companyId && !i.IsDeleted && i.Quantity == 0)
                    .CountAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        totalItems,
                        availableStock,
                        lowStockItems,
                        outOfStockItems
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Inventory dashboard statistics");
                return Json(new { success = false, message = "Error loading dashboard statistics" });
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// GET: api/Inventory/List
        /// Get paginated Inventory list
        /// </summary>
        [HttpGet("api/Inventory/List")]
        [RequirePermission(Constants.INVENTORY_VIEW)]
        public async Task<IActionResult> GetInventories([FromQuery] int page = 1, [FromQuery] int pageSize = 10, 
            [FromQuery] string? status = null, [FromQuery] string? search = null)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var query = _context.Inventories
                    .Where(i => i.CompanyId == companyId && !i.IsDeleted)
                    .Include(i => i.Item)
                    .Include(i => i.Location)
                    .AsQueryable();

                // Filter by status
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(i => i.Status == status);
                }

                // Search functionality
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(i => i.Item.ItemCode.Contains(search) ||
                                           i.Item.Name.Contains(search) ||
                                           i.Location.Code.Contains(search));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var inventories = await query
                    .OrderByDescending(i => i.LastUpdated)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(i => new
                    {
                        id = i.Id,
                        itemId = i.ItemId,
                        itemCode = i.Item.ItemCode,
                        itemName = i.Item.Name,
                        locationId = i.LocationId,
                        locationCode = i.Location.Code,
                        locationName = i.Location.Name,
                        locationCategory = i.Location.Category,
                        quantity = i.Quantity,
                        status = i.Quantity > 0 ? Constants.INVENTORY_STATUS_AVAILABLE : i.Status, // ✅ Auto-override untuk display real-time
                        lastUpdated = i.LastUpdated,
                        sourceReference = i.SourceReference,
                        notes = i.Notes
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = inventories,
                    pagination = new
                    {
                        currentPage = page,
                        totalPages,
                        totalCount,
                        pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Inventory list");
                return Json(new { success = false, message = "Error loading Inventory list" });
            }
        }

        /// <summary>
        /// GET: api/inventory/{id}
        /// Get Inventory details by ID
        /// </summary>
        [HttpGet("api/Inventory/{id}")]
        [RequirePermission(Constants.INVENTORY_VIEW)]
        public async Task<IActionResult> GetInventory(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var inventory = await _context.Inventories
                    .Where(i => i.Id == id && i.CompanyId == companyId && !i.IsDeleted)
                    .Include(i => i.Item)
                    .Include(i => i.Location)
                    .FirstOrDefaultAsync();
                
                if (inventory == null)
                {
                    return Json(new { success = false, message = "Inventory not found" });
                }

                var result = new
                {
                    id = inventory.Id,
                    itemId = inventory.ItemId,
                    itemCode = inventory.Item.ItemCode,
                    itemName = inventory.Item.Name,
                    locationId = inventory.LocationId,
                    locationCode = inventory.Location.Code,
                    locationName = inventory.Location.Name,
                    quantity = inventory.Quantity,
                    status = inventory.Quantity > 0 ? Constants.INVENTORY_STATUS_AVAILABLE : inventory.Status, // ✅ Auto-override untuk display real-time
                    lastUpdated = inventory.LastUpdated,
                    sourceReference = inventory.SourceReference,
                    notes = inventory.Notes
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Inventory details for ID: {InventoryId}", id);
                return Json(new { success = false, message = "Error loading Inventory details" });
            }
        }

        /// <summary>
        /// POST: api/Inventory
        /// Create new Inventory
        /// </summary>
        [HttpPost("api/Inventory")]
        [RequirePermission(Constants.INVENTORY_MANAGE)]
        public async Task<IActionResult> CreateInventory([FromBody] CreateInventoryRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid data provided" });
                }

                var companyId = _currentUserService.CompanyId.Value;
                var userId = _currentUserService.UserId;

                // Validate Item exists
                var item = await _context.Items
                    .Where(i => i.Id == request.ItemId && i.CompanyId == companyId && !i.IsDeleted)
                    .FirstOrDefaultAsync();

                if (item == null)
                {
                    return Json(new { success = false, message = "Item not found" });
                }

                // Validate Location exists
                var location = await _context.Locations
                    .Where(l => l.Id == request.LocationId && l.CompanyId == companyId && !l.IsDeleted)
                    .FirstOrDefaultAsync();

                if (location == null)
                {
                    return Json(new { success = false, message = "Location not found" });
                }

                // Auto-set status berdasarkan quantity
                var inventoryStatus = request.Quantity > 0 
                    ? Constants.INVENTORY_STATUS_AVAILABLE 
                    : Constants.INVENTORY_STATUS_EMPTY;

                var inventory = new Inventory
                {
                    ItemId = request.ItemId,
                    LocationId = request.LocationId,
                    Quantity = request.Quantity,
                    Status = inventoryStatus,
                    SourceReference = request.SourceReference,
                    Notes = request.Notes,
                    CompanyId = companyId,
                    CreatedBy = userId?.ToString() ?? "0",
                    CreatedDate = DateTime.Now,
                    LastUpdated = DateTime.Now
                };

                _context.Inventories.Add(inventory);
                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync("CREATE", "Inventory", inventory.Id, 
                    $"Created Inventory for Item {item.ItemCode} at Location {location.Code}");

                return Json(new { success = true, message = "Inventory created successfully", data = new { id = inventory.Id } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Inventory");
                return Json(new { success = false, message = "Error creating Inventory" });
            }
        }

        /// <summary>
        /// PUT: api/inventory/{id}
        /// Update Inventory
        /// </summary>
        [HttpPut("api/Inventory/{id}")]
        [RequirePermission(Constants.INVENTORY_MANAGE)]
        public async Task<IActionResult> UpdateInventory(int id, [FromBody] UpdateInventoryRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid data provided" });
                }

                var companyId = _currentUserService.CompanyId.Value;
                var userId = _currentUserService.UserId;

                var inventory = await _context.Inventories
                    .Where(i => i.Id == id && i.CompanyId == companyId && !i.IsDeleted)
                    .Include(i => i.Item)
                    .Include(i => i.Location)
                    .FirstOrDefaultAsync();

                if (inventory == null)
                {
                    return Json(new { success = false, message = "Inventory not found" });
                }

                // Update Inventory properties
                inventory.Quantity = request.Quantity;
                
                // Auto-update status berdasarkan quantity
                if (inventory.Quantity > 0)
                {
                    inventory.Status = Constants.INVENTORY_STATUS_AVAILABLE;
                }
                else if (inventory.Quantity == 0)
                {
                    inventory.Status = Constants.INVENTORY_STATUS_EMPTY;
                }
                else
                {
                    inventory.Status = request.Status; // Fallback jika ada status custom
                }
                
                inventory.Notes = request.Notes;
                inventory.ModifiedBy = userId?.ToString() ?? "0";
                inventory.ModifiedDate = DateTime.Now;
                inventory.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync("UPDATE", "Inventory", inventory.Id, 
                    $"Updated Inventory for Item {inventory.Item.ItemCode} at Location {inventory.Location.Code}");

                return Json(new { success = true, message = "Inventory updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Inventory with ID: {InventoryId}", id);
                return Json(new { success = false, message = "Error updating Inventory" });
            }
        }

        /// <summary>
        /// PATCH: api/Inventory/{id}/adjust
        /// Adjust inventory quantity
        /// </summary>
        [HttpPatch("api/Inventory/{id}/adjust")]
        [RequirePermission(Constants.INVENTORY_MANAGE)]
        public async Task<IActionResult> AdjustInventory(int id, [FromBody] AdjustInventoryRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;
                var userId = _currentUserService.UserId;

                var inventory = await _context.Inventories
                    .Where(i => i.Id == id && i.CompanyId == companyId && !i.IsDeleted)
                    .Include(i => i.Item)
                    .Include(i => i.Location)
                    .FirstOrDefaultAsync();

                if (inventory == null)
                {
                    return Json(new { success = false, message = "Inventory not found" });
                }

                var oldQuantity = inventory.Quantity;
                var newQuantity = request.AdjustmentType == "Add" ? 
                    inventory.Quantity + request.Quantity : 
                    inventory.Quantity - request.Quantity;

                if (newQuantity < 0)
                {
                    return Json(new { success = false, message = "Insufficient inventory for adjustment" });
                }

                inventory.Quantity = newQuantity;
                
                // Auto-update status berdasarkan quantity
                if (inventory.Quantity > 0)
                {
                    inventory.Status = Constants.INVENTORY_STATUS_AVAILABLE;
                }
                else
                {
                    inventory.Status = Constants.INVENTORY_STATUS_EMPTY;
                }
                
                inventory.LastUpdated = DateTime.Now;
                inventory.ModifiedBy = userId?.ToString() ?? "0";
                inventory.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync("ADJUST", "Inventory", inventory.Id, 
                    $"Adjusted Inventory for Item {inventory.Item.ItemCode} from {oldQuantity} to {newQuantity}");

                return Json(new { success = true, message = "Inventory adjusted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting Inventory for ID: {InventoryId}", id);
                return Json(new { success = false, message = "Error adjusting Inventory" });
            }
        }

        /// <summary>
        /// POST: api/Inventory/FixStatus
        /// Manual fix status untuk semua inventory existing berdasarkan quantity
        /// </summary>
        [HttpPost("api/Inventory/FixStatus")]
        [RequirePermission(Constants.INVENTORY_MANAGE)]
        public async Task<IActionResult> FixInventoryStatus()
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;
                
                // Update semua inventory yang quantity > 0 tapi status != Available
                var inventoriesToFix = await _context.Inventories
                    .Where(i => i.CompanyId == companyId && 
                               !i.IsDeleted && 
                               i.Quantity > 0 && 
                               i.Status != Constants.INVENTORY_STATUS_AVAILABLE)
                    .ToListAsync();
                
                foreach (var inventory in inventoriesToFix)
                {
                    inventory.Status = Constants.INVENTORY_STATUS_AVAILABLE;
                    inventory.ModifiedDate = DateTime.Now;
                    inventory.LastUpdated = DateTime.Now;
                }

                // Update semua inventory yang quantity = 0 tapi status != Empty
                var emptyInventoriesToFix = await _context.Inventories
                    .Where(i => i.CompanyId == companyId && 
                               !i.IsDeleted && 
                               i.Quantity == 0 && 
                               i.Status != Constants.INVENTORY_STATUS_EMPTY)
                    .ToListAsync();
                
                foreach (var inventory in emptyInventoriesToFix)
                {
                    inventory.Status = Constants.INVENTORY_STATUS_EMPTY;
                    inventory.ModifiedDate = DateTime.Now;
                    inventory.LastUpdated = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Fixed {inventoriesToFix.Count} Available and {emptyInventoriesToFix.Count} Empty inventory records",
                    fixedAvailable = inventoriesToFix.Count,
                    fixedEmpty = emptyInventoriesToFix.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing inventory status");
                return Json(new { success = false, message = "Error fixing inventory status" });
            }
        }

        #endregion

        #region Putaway API Endpoints

        /// <summary>
        /// GET: api/Putaway/Dashboard
        /// Get Putaway dashboard statistics
        /// </summary>
        [HttpGet("api/Putaway/Dashboard")]
        [RequirePermission(Constants.INVENTORY_VIEW)]
        public async Task<IActionResult> GetPutawayDashboard()
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;
                
                // Count ASNs ready for putaway (Status = "Processed")
                var totalProcessedASNs = await _context.AdvancedShippingNotices
                    .Where(a => a.CompanyId == companyId && !a.IsDeleted && a.Status == "Processed")
                    .CountAsync();
                    
                // Count pending items (ASNDetails with RemainingQuantity > 0)
                var totalPendingItems = await _context.ASNDetails
                    .Where(ad => ad.ASN.CompanyId == companyId && !ad.ASN.IsDeleted && 
                                ad.ASN.Status == "Processed" && ad.RemainingQuantity > 0)
                    .CountAsync();
                    
                // Sum total pending quantity
                var totalPendingQuantity = await _context.ASNDetails
                    .Where(ad => ad.ASN.CompanyId == companyId && !ad.ASN.IsDeleted && 
                                ad.ASN.Status == "Processed" && ad.RemainingQuantity > 0)
                    .SumAsync(ad => ad.RemainingQuantity);
                    
                // Today's ASNs
                var today = DateTime.Today;
                var todayPutawayCount = await _context.AdvancedShippingNotices
                    .Where(a => a.CompanyId == companyId && !a.IsDeleted && 
                                a.Status == "Processed" && 
                                a.ActualArrivalDate.HasValue && 
                                a.ActualArrivalDate.Value.Date == today)
                    .CountAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        totalProcessedASNs,
                        totalPendingItems,
                        totalPendingQuantity,
                        todayPutawayCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Putaway dashboard statistics");
                return Json(new { success = false, message = "Error loading dashboard statistics" });
            }
        }

        /// <summary>
        /// GET: api/Putaway
        /// Get ASNs ready for putaway with pagination
        /// </summary>
        [HttpGet("api/Putaway")]
        [RequirePermission(Constants.INVENTORY_VIEW)]
        public async Task<IActionResult> GetPutawayASNs([FromQuery] int page = 1, [FromQuery] int pageSize = 10, 
            [FromQuery] string? statusFilter = null, [FromQuery] bool showTodayOnly = false)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var query = _context.AdvancedShippingNotices
                    .Where(a => a.CompanyId == companyId && !a.IsDeleted && a.Status == "Processed")
                    .Include(a => a.PurchaseOrder)
                        .ThenInclude(po => po.Supplier)
                    .Include(a => a.ASNDetails)
                        .ThenInclude(ad => ad.Item)
                    .AsQueryable();

                // Apply filters
                if (showTodayOnly)
                {
                    var today = DateTime.Today;
                    query = query.Where(a => a.ActualArrivalDate.HasValue && 
                                           a.ActualArrivalDate.Value.Date == today);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var asns = await query
                    .OrderBy(a => a.ActualArrivalDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new
                    {
                        asnId = a.Id,
                        asnNumber = a.ASNNumber,
                        poNumber = a.PurchaseOrder.PONumber,
                        supplierName = a.PurchaseOrder.Supplier.Name,
                        actualArrivalDate = a.ActualArrivalDate,
                        status = a.Status,
                        totalItemTypes = a.ASNDetails.Count(),
                        totalQuantity = a.ASNDetails.Sum(ad => ad.ShippedQuantity),
                        pendingPutawayCount = a.ASNDetails.Count(ad => ad.RemainingQuantity > 0),
                        completionPercentage = a.ASNDetails.Any() ? 
                            (double)a.ASNDetails.Count(ad => ad.RemainingQuantity == 0) / 
                            a.ASNDetails.Count * 100 : 0,
                        isCompleted = a.ASNDetails.All(ad => ad.RemainingQuantity == 0)
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = asns,
                    pagination = new
                    {
                        currentPage = page,
                        totalPages,
                        totalCount,
                        pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Putaway ASNs");
                return Json(new { success = false, message = "Error loading ASNs" });
            }
        }

        /// <summary>
        /// POST: /Inventory/ProcessPutaway
        /// Process putaway for specific ASN item
        /// </summary>
        [HttpPost]
        [RequirePermission(Constants.INVENTORY_MANAGE)]
        public async Task<IActionResult> ProcessPutaway([FromForm] int asnDetailId, [FromForm] int quantityToPutaway, [FromForm] int locationId, [FromForm] int asnId, [FromForm] int itemId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var companyId = _currentUserService.CompanyId.Value;
                var userId = _currentUserService.UserId;

                _logger.LogInformation("Processing putaway for ASNDetailId: {ASNDetailId}, Quantity: {Quantity}, LocationId: {LocationId}", 
                    asnDetailId, quantityToPutaway, locationId);

                // Validate input parameters
                if (quantityToPutaway <= 0)
                {
                    return Json(new { success = false, message = "Quantity must be greater than 0" });
                }

                if (locationId <= 0)
                {
                    return Json(new { success = false, message = "Location must be selected" });
                }

                // Load and validate ASNDetail
                var asnDetail = await _context.ASNDetails
                    .Where(ad => ad.Id == asnDetailId && ad.ASN.CompanyId == companyId && !ad.ASN.IsDeleted)
                    .Include(ad => ad.ASN)
                        .ThenInclude(a => a.PurchaseOrder)
                    .Include(ad => ad.Item)
                    .FirstOrDefaultAsync();

                if (asnDetail == null)
                {
                    return Json(new { success = false, message = "ASN Detail not found" });
                }

                // Validate quantity doesn't exceed remaining
                if (quantityToPutaway > asnDetail.RemainingQuantity)
                {
                    return Json(new { success = false, message = $"Cannot putaway {quantityToPutaway} units. Only {asnDetail.RemainingQuantity} remaining." });
                }

                // Load and validate Location
                // Must be Storage category (not holding location)
                var location = await _context.Locations
                    .Where(l => l.Id == locationId && l.CompanyId == companyId && !l.IsDeleted && l.IsActive)
                    .FirstOrDefaultAsync();

                if (location == null)
                {
                    return Json(new { success = false, message = "Location not found or inactive" });
                }

                // Validate location is Storage category
                if (location.Category != Constants.LOCATION_CATEGORY_STORAGE)
                {
                    return Json(new { success = false, message = "Putaway can only be done to Storage locations, not holding locations" });
                }

                // Check location capacity (optional - can be removed if not needed)
                var availableCapacity = location.MaxCapacity - location.CurrentCapacity;
                if (availableCapacity < quantityToPutaway && location.MaxCapacity > 0)
                {
                    return Json(new { success = false, message = $"Insufficient location capacity. Available: {availableCapacity}, Required: {quantityToPutaway}" });
                }

                // Get ASN holding location
                var asn = await _context.AdvancedShippingNotices
                    .FirstOrDefaultAsync(a => a.Id == asnId && a.CompanyId == companyId);

                if (asn?.HoldingLocationId == null)
                {
                    return Json(new { success = false, message = "ASN holding location not found" });
                }

                // Find inventory in holding location
                var holdingInventory = await _context.Inventories
                    .Where(i => i.ItemId == itemId && i.LocationId == asn.HoldingLocationId && i.CompanyId == companyId && !i.IsDeleted)
                    .FirstOrDefaultAsync();

                if (holdingInventory == null)
                {
                    return Json(new { success = false, message = "Item not found in holding location" });
                }

                if (holdingInventory.Quantity < quantityToPutaway)
                {
                    return Json(new { success = false, message = $"Insufficient quantity in holding location. Available: {holdingInventory.Quantity}, Required: {quantityToPutaway}" });
                }

                // Load or create Inventory record for final location
                var existingInventory = await _context.Inventories
                    .Where(i => i.ItemId == itemId && i.LocationId == locationId && i.CompanyId == companyId && !i.IsDeleted)
                    .FirstOrDefaultAsync();

                int inventoryId = 0; // Track inventory ID for audit logging

                Inventory? newInventory = null;
                
                if (existingInventory != null)
                {
                    // Update existing inventory
                    existingInventory.Quantity += quantityToPutaway;
                    
                    // Auto-update status berdasarkan quantity
                    if (existingInventory.Quantity > 0)
                    {
                        existingInventory.Status = Constants.INVENTORY_STATUS_AVAILABLE;
                    }
                    
                    existingInventory.LastUpdated = DateTime.Now;
                    existingInventory.ModifiedBy = userId?.ToString() ?? "0";
                    existingInventory.ModifiedDate = DateTime.Now;
                    existingInventory.SourceReference = asnDetail.ASN.ASNNumber;
                    inventoryId = existingInventory.Id;
                }
                else
                {
                    // Create new inventory record
                    newInventory = new Inventory
                    {
                        ItemId = itemId,
                        LocationId = locationId,
                        Quantity = quantityToPutaway,
                        Status = "Available",
                        SourceReference = asnDetail.ASN.ASNNumber,
                        Notes = $"Putaway from ASN {asnDetail.ASN.ASNNumber}",
                        CompanyId = companyId,
                        CreatedBy = userId?.ToString() ?? "0",
                        CreatedDate = DateTime.Now,
                        LastUpdated = DateTime.Now
                    };

                    _context.Inventories.Add(newInventory);
                }

                // Reduce quantity from holding location
                holdingInventory.Quantity -= quantityToPutaway;
                
                // Auto-update status berdasarkan quantity
                if (holdingInventory.Quantity > 0)
                {
                    holdingInventory.Status = Constants.INVENTORY_STATUS_AVAILABLE;
                }
                
                holdingInventory.ModifiedBy = userId?.ToString() ?? "0";
                holdingInventory.ModifiedDate = DateTime.Now;

                // If holding inventory becomes empty, remove it
                if (holdingInventory.Quantity <= 0)
                {
                    _context.Inventories.Remove(holdingInventory);
                }
                else
                {
                    _context.Inventories.Update(holdingInventory);
                }

                // Update ASNDetail - add to AlreadyPutAwayQuantity and reduce RemainingQuantity
                asnDetail.AddPutawayQuantity(quantityToPutaway);
                asnDetail.ModifiedBy = userId?.ToString() ?? "0";
                asnDetail.ModifiedDate = DateTime.Now;

                // Update Final Location CurrentCapacity
                location.CurrentCapacity += quantityToPutaway;
                location.ModifiedBy = userId?.ToString() ?? "0";
                location.ModifiedDate = DateTime.Now;

                // Update Holding Location CurrentCapacity (reduce)
                var holdingLocation = await _context.Locations
                    .FirstOrDefaultAsync(l => l.Id == asn.HoldingLocationId);
                
                if (holdingLocation != null)
                {
                    holdingLocation.CurrentCapacity -= quantityToPutaway;
                    holdingLocation.ModifiedBy = userId?.ToString() ?? "0";
                    holdingLocation.ModifiedDate = DateTime.Now;
                    _context.Locations.Update(holdingLocation);
                }

                // Save all changes in single transaction
                await _context.SaveChangesAsync();
                
                // Get the new inventory ID after save
                if (newInventory != null)
                {
                    inventoryId = newInventory.Id;
                }

                // Log audit trail
                await _auditService.LogActionAsync("PUTAWAY", "ASNDetail", asnDetailId, 
                    $"Transfer {quantityToPutaway} units of {asnDetail.Item.ItemCode} from holding location to {location.Code} from ASN {asnDetail.ASN.ASNNumber}");

                if (inventoryId > 0)
                {
                    if (existingInventory != null)
                    {
                        await _auditService.LogActionAsync("UPDATE", "Inventory", inventoryId, 
                            $"Updated inventory for {asnDetail.Item.ItemCode} at location {location.Code} (+{quantityToPutaway} units from ASN {asnDetail.ASN.ASNNumber})");
                    }
                    else
                    {
                        await _auditService.LogActionAsync("CREATE", "Inventory", inventoryId, 
                            $"Created inventory record for {asnDetail.Item.ItemCode} at location {location.Code} ({quantityToPutaway} units from ASN {asnDetail.ASN.ASNNumber})");
                    }
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Successfully processed putaway for ASNDetailId: {ASNDetailId}, Quantity: {Quantity}, LocationId: {LocationId}", 
                    asnDetailId, quantityToPutaway, locationId);

                return Json(new { 
                    success = true, 
                    message = $"Successfully putaway {quantityToPutaway} units of {asnDetail.Item.ItemCode} to {location.Code}",
                    data = new {
                        asnDetailId = asnDetail.Id,
                        totalQuantity = asnDetail.ShippedQuantity,
                        remainingQuantity = asnDetail.RemainingQuantity,
                        alreadyPutAwayQuantity = asnDetail.AlreadyPutAwayQuantity,
                        isCompleted = asnDetail.RemainingQuantity == 0
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing putaway for ASNDetailId: {ASNDetailId}, Quantity: {Quantity}, LocationId: {LocationId}", 
                    asnDetailId, quantityToPutaway, locationId);
                return Json(new { success = false, message = "Error processing putaway: " + ex.Message });
            }
        }

        #endregion

        #region Request Models

        public class CreateInventoryRequest
        {
            [Required]
            public int ItemId { get; set; }
            
            [Required]
            public int LocationId { get; set; }
            
            [Required]
            [Range(0, int.MaxValue)]
            public int Quantity { get; set; }
            
            [Required]
            [StringLength(50)]
            public string Status { get; set; } = "Available";
            
            [StringLength(100)]
            public string? SourceReference { get; set; }
            
            [StringLength(500)]
            public string? Notes { get; set; }
        }

        public class UpdateInventoryRequest
        {
            [Required]
            [Range(0, int.MaxValue)]
            public int Quantity { get; set; }
            
            [Required]
            [StringLength(50)]
            public string Status { get; set; } = "Available";
            
            [StringLength(500)]
            public string? Notes { get; set; }
        }

        public class AdjustInventoryRequest
        {
            [Required]
            [Range(1, int.MaxValue)]
            public int Quantity { get; set; }
            
            [Required]
            [StringLength(10)]
            public string AdjustmentType { get; set; } = "Add"; // "Add" or "Subtract"
            
            [StringLength(500)]
            public string? Reason { get; set; }
        }

        /// <summary>
        /// GET: /inventory/putaway/{asnId}/details
        /// Show putaway details view
        /// </summary>
        [HttpGet("putaway/{asnId}/details")]
        [RequirePermission(Constants.INVENTORY_VIEW)]
        public IActionResult PutawayDetails(int asnId)
        {
            return View(asnId);
        }

        /// <summary>
        /// GET: api/inventory/putaway/{asnId}/details
        /// Get putaway details for detail view
        /// </summary>
        [HttpGet("api/inventory/putaway/{asnId}/details")]
        [RequirePermission(Constants.INVENTORY_VIEW)]
        public async Task<IActionResult> GetPutawayDetails(int asnId)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var asnDetails = await _context.ASNDetails
                    .Where(ad => ad.ASNId == asnId && ad.ASN.CompanyId == companyId && !ad.ASN.IsDeleted)
                    .Include(ad => ad.Item)
                    .Include(ad => ad.ASN)
                    .ThenInclude(a => a.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                    .Select(ad => new
                    {
                        id = ad.Id,
                        itemId = ad.ItemId,
                        itemCode = ad.Item.ItemCode,
                        itemName = ad.Item.Name,
                        itemUnit = ad.Item.Unit,
                        shippedQuantity = ad.ShippedQuantity,
                        alreadyPutAwayQuantity = ad.AlreadyPutAwayQuantity,
                        remainingQuantity = ad.RemainingQuantity,
                        actualPricePerItem = ad.ActualPricePerItem,
                        totalActualValue = ad.TotalActualValue,
                        status = ad.RemainingQuantity == 0 ? "Completed" : "Partial",
                        statusIndonesia = ad.RemainingQuantity == 0 ? "Selesai" : "Sebagian",
                        asnNumber = ad.ASN.ASNNumber,
                        purchaseOrderNumber = ad.ASN.PurchaseOrder.PONumber,
                        supplierName = ad.ASN.PurchaseOrder.Supplier.Name,
                        shipmentDate = ad.ASN.ShipmentDate,
                        expectedArrivalDate = ad.ASN.ExpectedArrivalDate,
                        actualArrivalDate = ad.ASN.ActualArrivalDate,
                        asnStatus = ad.ASN.Status,
                        asnStatusIndonesia = ad.ASN.StatusIndonesia,
                        canBeProcessed = ad.RemainingQuantity > 0,
                        isOnTime = ad.ASN.IsOnTime,
                        delayDays = ad.ASN.DelayDays,
                        createdDate = ad.CreatedDate,
                        modifiedDate = ad.ModifiedDate
                    })
                    .ToListAsync();

                return Json(new { success = true, data = asnDetails });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting putaway details for ASN {ASNId}", asnId);
                return Json(new { success = false, message = "Error loading putaway details" });
            }
        }

        #endregion
    }
}