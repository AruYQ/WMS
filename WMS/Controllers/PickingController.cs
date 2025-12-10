using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Attributes;
using WMS.Data;
using WMS.Models;
using WMS.Services;
using WMS.Utilities;
using System.ComponentModel.DataAnnotations;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller untuk Picking management - Similar to Putaway but from Source to Holding Location
    /// </summary>
    [RequirePermission(Constants.PICKING_VIEW)]
    public class PickingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditTrailService _auditService;
        private readonly ILogger<PickingController> _logger;

        public PickingController(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAuditTrailService auditService,
            ILogger<PickingController> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _auditService = auditService;
            _logger = logger;
        }

        #region MVC Actions

        /// <summary>
        /// GET: /Picking
        /// Picking management index page
        /// </summary>
        public IActionResult Index()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Picking index page");
                return View("Error");
            }
        }

        /// <summary>
        /// GET: /Picking/Details/{id}
        /// Picking details page
        /// Uses conventional routing: /Picking/Details/3
        /// </summary>
        [RequirePermission(Constants.PICKING_VIEW)]
        public IActionResult Details(int id)
        {
            try
            {
                return View(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Picking details page for ID: {PickingId}", id);
                return View("Error");
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// GET: api/picking
        /// Get paginated Picking list
        /// </summary>
        [HttpGet("api/picking")]
        [RequirePermission(Constants.PICKING_VIEW)]
        public async Task<IActionResult> GetPickings(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var query = _context.Pickings
                    .Where(p => p.CompanyId == companyId && !p.IsDeleted)
                    .Include(p => p.SalesOrder)
                        .ThenInclude(so => so.Customer)
                    .AsQueryable();

                // Filter by status
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                // Search functionality
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.PickingNumber.Contains(search) ||
                                           (p.SalesOrder != null && p.SalesOrder.SONumber.Contains(search)) ||
                                           (p.SalesOrder != null && p.SalesOrder.Customer != null && p.SalesOrder.Customer.Name.Contains(search)));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var pickings = await query
                    .OrderByDescending(p => p.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        id = p.Id,
                        pickingNumber = p.PickingNumber,
                        salesOrderId = p.SalesOrderId,
                        salesOrderNumber = p.SalesOrder != null ? p.SalesOrder.SONumber : null,
                        customerName = p.SalesOrder != null && p.SalesOrder.Customer != null ? p.SalesOrder.Customer.Name : null,
                        pickingDate = p.PickingDate,
                        completedDate = p.CompletedDate,
                        status = p.Status,
                        totalQuantityRequired = p.PickingDetails.Sum(pd => pd.QuantityRequired),
                        totalQuantityPicked = p.PickingDetails.Sum(pd => pd.QuantityPicked),
                        completionPercentage = p.PickingDetails.Any() ? 
                            (decimal)p.PickingDetails.Sum(pd => pd.QuantityPicked) / p.PickingDetails.Sum(pd => pd.QuantityRequired) * 100 : 0,
                        createdDate = p.CreatedDate
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = pickings,
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
                _logger.LogError(ex, "Error getting Picking list");
                return Json(new { success = false, message = "Error loading Picking list" });
            }
        }

        /// <summary>
        /// GET: api/picking/locations/{itemId}
        /// Get available locations for an item based on inventory
        /// </summary>
        [HttpGet("api/picking/locations/{itemId}")]
        [RequirePermission(Constants.PICKING_VIEW)]
        public async Task<IActionResult> GetAvailableLocations(int itemId, [FromQuery] int? quantityRequired = null)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                // Get all inventories for this item that have available stock
                // Only from Storage locations (not holding locations)
                var inventories = await _context.Inventories
                    .Where(i => i.ItemId == itemId &&
                               i.CompanyId == companyId &&
                               !i.IsDeleted &&
                               i.Status == Constants.INVENTORY_STATUS_AVAILABLE &&
                               i.Quantity > 0 &&
                               i.Location.Category == Constants.LOCATION_CATEGORY_STORAGE)
                    .Include(i => i.Location)
                    .OrderBy(i => i.Location.Code) // Sort by location code
                    .ToListAsync();

                var locations = inventories
                    .Where(i => !quantityRequired.HasValue || i.Quantity >= quantityRequired.Value)
                    .Select(i => new
                    {
                        locationId = i.LocationId,
                        locationCode = i.Location.Code,
                        locationName = i.Location.Name,
                        availableStock = i.Quantity,
                        status = i.Status,
                        currentCapacity = i.Location.CurrentCapacity,
                        maxCapacity = i.Location.MaxCapacity
                    })
                    .ToList();

                return Json(new { success = true, data = locations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available locations for item {ItemId}", itemId);
                return Json(new { success = false, message = "Error loading available locations" });
            }
        }

        /// <summary>
        /// GET: api/picking/{id}
        /// Get Picking details by ID
        /// </summary>
        [HttpGet("api/picking/{id}")]
        [RequirePermission(Constants.PICKING_VIEW)]
        public async Task<IActionResult> GetPicking(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var picking = await _context.Pickings
                    .Where(p => p.Id == id && p.CompanyId == companyId && !p.IsDeleted)
                    .Include(p => p.SalesOrder)
                        .ThenInclude(so => so.Customer)
                    .Include(p => p.SalesOrder)
                        .ThenInclude(so => so.HoldingLocation)
                    .Include(p => p.PickingDetails)
                        .ThenInclude(pd => pd.Item)
                    .Include(p => p.PickingDetails)
                        .ThenInclude(pd => pd.Location)
                    .Include(p => p.PickingDetails)
                        .ThenInclude(pd => pd.SalesOrderDetail)
                    .FirstOrDefaultAsync();

                if (picking == null)
                {
                    return Json(new { success = false, message = "Picking not found" });
                }

                var result = new
                {
                    id = picking.Id,
                    pickingNumber = picking.PickingNumber,
                    salesOrderId = picking.SalesOrderId,
                    salesOrderNumber = picking.SalesOrder?.SONumber ?? "N/A",
                    customerName = picking.SalesOrder?.Customer?.Name ?? "N/A",
                    pickingDate = picking.PickingDate,
                    completedDate = picking.CompletedDate,
                    status = picking.Status,
                    holdingLocationId = picking.SalesOrder?.HoldingLocationId,
                    holdingLocationName = picking.SalesOrder?.HoldingLocation?.Name ?? "N/A",
                    notes = picking.Notes,
                    details = picking.PickingDetails.Select(pd => new
                    {
                        id = pd.Id,
                        salesOrderDetailId = pd.SalesOrderDetailId,
                        itemId = pd.ItemId,
                        itemCode = pd.Item.ItemCode,
                        itemName = pd.Item.Name,
                        itemUnit = pd.Item.Unit,
                        locationId = pd.LocationId,
                        locationCode = pd.Location?.Code ?? "N/A",
                        locationName = pd.Location?.Name ?? "N/A",
                        quantityRequired = pd.QuantityRequired,
                        quantityToPick = pd.QuantityToPick,
                        quantityPicked = pd.QuantityPicked,
                        remainingQuantity = pd.RemainingQuantity,
                        status = pd.Status,
                        notes = pd.Notes
                    })
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Picking details for ID: {PickingId}", id);
                return Json(new { success = false, message = "Error loading Picking details" });
            }
        }

        /// <summary>
        /// POST: api/picking
        /// Create new Picking from Sales Order
        /// </summary>
        [HttpPost("api/picking")]
        [RequirePermission(Constants.PICKING_MANAGE)]
        public async Task<IActionResult> CreatePicking([FromBody] CreatePickingRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var companyId = _currentUserService.CompanyId.Value;
                var userId = _currentUserService.UserId;

                _logger.LogInformation("Creating Picking for Sales Order {SalesOrderId}", request.SalesOrderId);

                // Validate Sales Order exists
                var salesOrder = await _context.SalesOrders
                    .Where(so => so.Id == request.SalesOrderId && so.CompanyId == companyId && !so.IsDeleted)
                    .Include(so => so.SalesOrderDetails)
                        .ThenInclude(sod => sod.Item)
                    .Include(so => so.HoldingLocation)
                    .FirstOrDefaultAsync();

                if (salesOrder == null)
                {
                    return Json(new { success = false, message = "Sales Order not found" });
                }

                // Validate Sales Order status
                if (salesOrder.Status != Constants.SO_STATUS_DRAFT && salesOrder.Status != "Pending" && salesOrder.Status != "In Progress")
                {
                    return Json(new { success = false, message = "Picking can only be created for Sales Orders with status Pending or In Progress" });
                }

                // Check if picking already exists for this Sales Order (only if not completed)
                var existingPicking = await _context.Pickings
                    .Where(p => p.SalesOrderId == request.SalesOrderId && 
                               p.CompanyId == companyId && 
                               !p.IsDeleted &&
                               p.Status != Constants.PICKING_STATUS_COMPLETED)
                    .FirstOrDefaultAsync();

                if (existingPicking != null)
                {
                    return Json(new { success = false, message = "An active picking already exists for this Sales Order" });
                }

                // Validate holding location
                if (!salesOrder.HoldingLocationId.HasValue)
                {
                    return Json(new { success = false, message = "Sales Order must have a holding location" });
                }

                var holdingLocation = salesOrder.HoldingLocation;
                if (holdingLocation == null || !holdingLocation.IsActive)
                {
                    return Json(new { success = false, message = "Holding location is invalid or inactive" });
                }

                // Generate Picking Number
                var pickingNumber = await GeneratePickingNumber(companyId);

                // Create Picking
                var picking = new Picking
                {
                    PickingNumber = pickingNumber,
                    SalesOrderId = request.SalesOrderId,
                    PickingDate = DateTime.Now,
                    Status = Constants.PICKING_STATUS_PENDING,
                    Notes = request.Notes,
                    CompanyId = companyId,
                    CreatedBy = userId?.ToString() ?? "0",
                    CreatedDate = DateTime.Now
                };

                _context.Pickings.Add(picking);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Picking saved successfully with ID: {PickingId}", picking.Id);

                // Create Picking Details from Sales Order Details
                // For each SO Detail, find available inventory locations and create picking details
                foreach (var soDetail in salesOrder.SalesOrderDetails)
                {
                    // Get available inventory for this item
                    // Only from Storage locations (not holding locations)
                    var availableInventories = await _context.Inventories
                        .Where(i => i.ItemId == soDetail.ItemId &&
                                   i.CompanyId == companyId &&
                                   !i.IsDeleted &&
                                   i.Status == Constants.INVENTORY_STATUS_AVAILABLE &&
                                   i.Quantity > 0 &&
                                   i.Location.Category == Constants.LOCATION_CATEGORY_STORAGE)
                        .Include(i => i.Location)
                        .OrderBy(i => i.CreatedDate) // FIFO
                        .ToListAsync();

                    if (!availableInventories.Any())
                    {
                        _logger.LogWarning("No available inventory found for ItemId {ItemId} in Sales Order {SOId}", soDetail.ItemId, request.SalesOrderId);
                        continue;
                    }

                    int remainingQty = soDetail.Quantity;
                    
                    // Distribute required quantity across available locations
                    foreach (var inventory in availableInventories)
                    {
                        if (remainingQty <= 0) break;

                        int qtyToPickFromLocation = Math.Min(remainingQty, inventory.Quantity);

                        var pickingDetail = new PickingDetail
                        {
                            PickingId = picking.Id,
                            SalesOrderDetailId = soDetail.Id,
                            ItemId = soDetail.ItemId,
                            LocationId = inventory.LocationId,
                            QuantityRequired = soDetail.Quantity, // Total required from SO
                            QuantityToPick = 0, // Will be set during picking process
                            QuantityPicked = 0,
                            RemainingQuantity = soDetail.Quantity, // Will be reduced as picking happens
                            Status = Constants.PICKING_DETAIL_STATUS_PENDING,
                            CompanyId = companyId,
                            CreatedBy = userId?.ToString() ?? "0",
                            CreatedDate = DateTime.Now
                        };

                        _context.PickingDetails.Add(pickingDetail);
                        remainingQty -= qtyToPickFromLocation;
                    }

                    // If we couldn't allocate all quantity, create a detail anyway with the shortage
                    if (remainingQty > 0)
                    {
                        _logger.LogWarning("Insufficient inventory for ItemId {ItemId}. Required: {Required}, Short: {Short}", 
                            soDetail.ItemId, soDetail.Quantity, remainingQty);
                    }
                }

                await _context.SaveChangesAsync();

                // Update Sales Order status
                salesOrder.Status = "In Progress"; // Keep as is - SO status "In Progress" is not in constants
                salesOrder.ModifiedBy = userId?.ToString() ?? "0";
                salesOrder.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync("CREATE", "Picking", picking.Id, 
                    $"Created Picking {picking.PickingNumber} for Sales Order {salesOrder.SONumber}");

                transaction.Commit();
                _logger.LogInformation("Transaction committed successfully");

                return Json(new { success = true, message = "Picking created successfully", data = new { id = picking.Id } });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating Picking");
                return Json(new { success = false, message = "Error creating Picking" });
            }
        }

        /// <summary>
        /// POST: api/picking/{id}/process
        /// Process picking - execute the picks (move from source to holding location)
        /// </summary>
        [HttpPost("api/picking/{id}/process")]
        [RequirePermission(Constants.PICKING_MANAGE)]
        public async Task<IActionResult> ProcessPicking(int id, [FromBody] ProcessPickingRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var companyId = _currentUserService.CompanyId.Value;
                var userId = _currentUserService.UserId;

                _logger.LogInformation("Processing Picking {PickingId}", id);

                var picking = await _context.Pickings
                    .Where(p => p.Id == id && p.CompanyId == companyId && !p.IsDeleted)
                    .Include(p => p.SalesOrder)
                        .ThenInclude(so => so.HoldingLocation)
                    .Include(p => p.PickingDetails)
                        .ThenInclude(pd => pd.Location)
                    .Include(p => p.PickingDetails)
                        .ThenInclude(pd => pd.Item)
                    .Include(p => p.PickingDetails)
                        .ThenInclude(pd => pd.SalesOrderDetail)
                    .FirstOrDefaultAsync();

                if (picking == null)
                {
                    return Json(new { success = false, message = "Picking not found" });
                }

                // Validate: Cannot process cancelled Picking
                if (picking.Status == Constants.PICKING_STATUS_CANCELLED)
                {
                    return Json(new { success = false, message = "Cannot process a cancelled Picking" });
                }

                // Validate: Cannot process if Sales Order is cancelled
                if (picking.SalesOrder.Status == Constants.SO_STATUS_CANCELLED)
                {
                    return Json(new { success = false, message = "Cannot process Picking for a cancelled Sales Order" });
                }

                // Validate holding location
                if (!picking.SalesOrder.HoldingLocationId.HasValue)
                {
                    return Json(new { success = false, message = "Holding location not set in Sales Order" });
                }

                var holdingLocation = picking.SalesOrder.HoldingLocation;
                if (holdingLocation == null || !holdingLocation.IsActive)
                {
                    return Json(new { success = false, message = "Holding location is invalid or inactive" });
                }

                // Process each picking detail
                foreach (var detailRequest in request.Details)
                {
                    if (detailRequest.QuantityToPick <= 0)
                        continue;

                    var pickingDetail = picking.PickingDetails
                        .FirstOrDefault(pd => pd.Id == detailRequest.PickingDetailId);

                    if (pickingDetail == null)
                    {
                        _logger.LogWarning("Picking Detail {PickingDetailId} not found", detailRequest.PickingDetailId);
                        continue;
                    }

                    // Validate quantity
                    if (detailRequest.QuantityToPick > pickingDetail.RemainingQuantity)
                    {
                        return Json(new { success = false, message = $"Quantity to pick ({detailRequest.QuantityToPick}) exceeds remaining quantity ({pickingDetail.RemainingQuantity}) for item {pickingDetail.Item.ItemCode}" });
                    }

                    // Validate location is selected (if provided in request)
                    int? sourceLocationId = detailRequest.LocationId ?? pickingDetail.LocationId;
                    if (!sourceLocationId.HasValue || sourceLocationId.Value <= 0)
                    {
                        return Json(new { success = false, message = $"Source location must be selected for item {pickingDetail.Item.ItemCode}" });
                    }

                    // Get source inventory using selected location
                    var sourceInventory = await _context.Inventories
                        .Where(i => i.ItemId == pickingDetail.ItemId &&
                                   i.LocationId == sourceLocationId.Value &&
                                   i.CompanyId == companyId &&
                                   !i.IsDeleted &&
                                   i.Status == Constants.INVENTORY_STATUS_AVAILABLE)
                        .Include(i => i.Location)
                        .FirstOrDefaultAsync();

                    if (sourceInventory == null || sourceInventory.Quantity < detailRequest.QuantityToPick)
                    {
                        var locationCode = sourceInventory?.Location?.Code ?? "Unknown";
                        return Json(new { success = false, message = $"Insufficient stock at location {locationCode} for item {pickingDetail.Item.ItemCode}. Available: {sourceInventory?.Quantity ?? 0}, Required: {detailRequest.QuantityToPick}" });
                    }

                    // Get source location reference (declare once for whole scope)
                    // Prefer location from inventory, fallback to picking detail location
                    var sourceLocation = sourceInventory.Location ?? pickingDetail.Location;

                    // Update picking detail location if it was changed
                    if (pickingDetail.LocationId != sourceLocationId.Value && sourceInventory.Location != null)
                    {
                        pickingDetail.LocationId = sourceLocationId.Value;
                        pickingDetail.Location = sourceInventory.Location;
                        sourceLocation = sourceInventory.Location; // Update reference
                    }

                    // Reduce from source location
                    sourceInventory.Quantity -= detailRequest.QuantityToPick;
                    
                    // Auto-update status berdasarkan quantity
                    if (sourceInventory.Quantity > 0)
                    {
                        sourceInventory.Status = Constants.INVENTORY_STATUS_AVAILABLE;
                    }
                    else
                    {
                        sourceInventory.Status = Constants.INVENTORY_STATUS_EMPTY;
                    }
                    
                    sourceInventory.ModifiedBy = userId?.ToString() ?? "0";
                    sourceInventory.ModifiedDate = DateTime.Now;

                    // Update source location capacity
                    if (sourceLocation != null)
                    {
                        sourceLocation.CurrentCapacity -= detailRequest.QuantityToPick;
                        sourceLocation.ModifiedBy = userId?.ToString() ?? "0";
                        sourceLocation.ModifiedDate = DateTime.Now;
                    }

                    // Add to holding location
                    var holdingInventory = await _context.Inventories
                        .Where(i => i.ItemId == pickingDetail.ItemId &&
                                   i.LocationId == holdingLocation.Id &&
                                   i.CompanyId == companyId &&
                                   !i.IsDeleted)
                        .FirstOrDefaultAsync();

                    if (holdingInventory != null)
                    {
                        holdingInventory.Quantity += detailRequest.QuantityToPick;
                        
                        // Auto-update status berdasarkan quantity
                        if (holdingInventory.Quantity > 0)
                        {
                            holdingInventory.Status = Constants.INVENTORY_STATUS_AVAILABLE;
                        }
                        
                        holdingInventory.ModifiedBy = userId?.ToString() ?? "0";
                        holdingInventory.ModifiedDate = DateTime.Now;
                    }
                    else
                    {
                        holdingInventory = new Inventory
                        {
                            ItemId = pickingDetail.ItemId,
                            LocationId = holdingLocation.Id,
                            Quantity = detailRequest.QuantityToPick,
                            Status = Constants.INVENTORY_STATUS_AVAILABLE,
                            LastCostPrice = sourceInventory.LastCostPrice,
                            SourceReference = $"SO-{picking.SalesOrderId}-{pickingDetail.SalesOrderDetailId}",
                            Notes = $"Picked from {sourceLocation?.Code ?? "Unknown"} for SO {picking.SalesOrder.SONumber}",
                            CompanyId = companyId,
                            CreatedBy = userId?.ToString() ?? "0",
                            CreatedDate = DateTime.Now
                        };
                        _context.Inventories.Add(holdingInventory);
                    }

                    // Update holding location capacity
                    holdingLocation.CurrentCapacity += detailRequest.QuantityToPick;
                    holdingLocation.ModifiedBy = userId?.ToString() ?? "0";
                    holdingLocation.ModifiedDate = DateTime.Now;

                    // Update picking detail
                    pickingDetail.QuantityPicked += detailRequest.QuantityToPick;
                    pickingDetail.RemainingQuantity = pickingDetail.QuantityRequired - pickingDetail.QuantityPicked;
                    
                    if (pickingDetail.RemainingQuantity == 0)
                    {
                        pickingDetail.Status = Constants.PICKING_DETAIL_STATUS_PICKED;
                    }
                    else
                    {
                        pickingDetail.Status = Constants.PICKING_DETAIL_STATUS_SHORT;
                    }
                    
                    pickingDetail.ModifiedBy = userId?.ToString() ?? "0";
                    pickingDetail.ModifiedDate = DateTime.Now;
                }

                // Update picking status
                var allDetailsComplete = picking.PickingDetails.All(pd => pd.Status == Constants.PICKING_DETAIL_STATUS_PICKED);
                if (allDetailsComplete && picking.PickingDetails.Any())
                {
                    picking.Status = Constants.PICKING_STATUS_COMPLETED;
                    picking.CompletedDate = DateTime.Now;
                }
                else if (picking.PickingDetails.Any(pd => pd.QuantityPicked > 0))
                {
                    picking.Status = Constants.PICKING_STATUS_IN_PROGRESS;
                }

                picking.ModifiedBy = userId?.ToString() ?? "0";
                picking.ModifiedDate = DateTime.Now;

                // Update Sales Order status if picking is complete
                if (picking.Status == Constants.PICKING_STATUS_COMPLETED)
                {
                    picking.SalesOrder.Status = "Picked"; // SO status "Picked" - keep as is
                    picking.SalesOrder.ModifiedBy = userId?.ToString() ?? "0";
                    picking.SalesOrder.ModifiedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditService.LogActionAsync("PROCESS", "Picking", picking.Id, 
                    $"Processed Picking {picking.PickingNumber}");

                return Json(new { success = true, message = "Picking processed successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing Picking {PickingId}", id);
                return Json(new { success = false, message = "Error processing Picking" });
            }
        }

        /// <summary>
        /// GET: api/picking/salesorder/{salesOrderId}
        /// Get picking for a specific Sales Order
        /// </summary>
        [HttpGet("api/picking/salesorder/{salesOrderId}")]
        [RequirePermission(Constants.PICKING_VIEW)]
        public async Task<IActionResult> GetPickingBySalesOrder(int salesOrderId)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var picking = await _context.Pickings
                    .Where(p => p.SalesOrderId == salesOrderId && p.CompanyId == companyId && !p.IsDeleted)
                    .Include(p => p.SalesOrder)
                        .ThenInclude(so => so.Customer)
                    .FirstOrDefaultAsync();

                if (picking == null)
                {
                    return Json(new { success = false, message = "No picking found for this Sales Order" });
                }

                return Json(new { success = true, data = new { id = picking.Id, pickingNumber = picking.PickingNumber } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Picking for Sales Order {SalesOrderId}", salesOrderId);
                return Json(new { success = false, message = "Error loading Picking" });
            }
        }

        #endregion

        #region Helper Methods

        private async Task<string> GeneratePickingNumber(int companyId)
        {
            try
            {
                var today = DateTime.Now;
                var prefix = $"PKG{today:yyyyMMdd}";
                
                var lastPicking = await _context.Pickings
                    .Where(p => p.CompanyId == companyId &&
                               p.PickingNumber.StartsWith(prefix) &&
                               !p.IsDeleted)
                    .OrderByDescending(p => p.PickingNumber)
                    .FirstOrDefaultAsync();

                if (lastPicking == null)
                {
                    return $"{prefix}001";
                }

                var lastNumber = lastPicking.PickingNumber.Substring(prefix.Length);
                if (int.TryParse(lastNumber, out int number))
                {
                    return $"{prefix}{(number + 1):D3}";
                }

                return $"{prefix}001";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Picking number for company {CompanyId}", companyId);
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                return $"PKG{timestamp}";
            }
        }

        #endregion

        #region Request Models

        public class CreatePickingRequest
        {
            [Required]
            public int SalesOrderId { get; set; }
            
            [StringLength(500)]
            public string? Notes { get; set; }
        }

        public class ProcessPickingRequest
        {
            public List<PickingDetailProcessRequest> Details { get; set; } = new List<PickingDetailProcessRequest>();
        }

        public class PickingDetailProcessRequest
        {
            [Required]
            public int PickingDetailId { get; set; }

            [Required]
            [Range(1, int.MaxValue, ErrorMessage = "Quantity to pick must be greater than 0")]
            public int QuantityToPick { get; set; }

            /// <summary>
            /// Source location ID (optional - will use existing LocationId if not provided)
            /// </summary>
            public int? LocationId { get; set; }
        }

        #endregion
    }
}
