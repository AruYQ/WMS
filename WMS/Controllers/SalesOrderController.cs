using Microsoft.AspNetCore.Mvc;
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
    /// Controller untuk Sales Order management - Hybrid MVC + API
    /// </summary>
    [RequirePermission(Constants.SO_VIEW)]
    public class SalesOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditTrailService _auditService;
        private readonly ILogger<SalesOrderController> _logger;

        public SalesOrderController(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAuditTrailService auditService,
            ILogger<SalesOrderController> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _auditService = auditService;
            _logger = logger;
        }

        #region MVC Actions

        /// <summary>
        /// GET: /SalesOrder
        /// Sales Order management index page
        /// </summary>
        public IActionResult Index()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Sales Order index page");
                return View("Error");
            }
        }

        /// <summary>
        /// GET: /SalesOrder/Details/{id}
        /// Sales Order details page
        /// </summary>
        [RequirePermission(Constants.SO_VIEW)]
        public IActionResult Details(int id)
        {
            try
            {
                return View(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Sales Order details page for ID: {SalesOrderId}", id);
                return View("Error");
            }
        }

        #endregion

        #region Dashboard & Statistics

        /// <summary>
        /// GET: api/salesorder/dashboard
        /// Get Sales Order dashboard statistics
        /// </summary>
        [HttpGet("api/salesorder/dashboard")]
        [RequirePermission(Constants.SO_VIEW)]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var totalSOs = await _context.SalesOrders
                    .Where(so => so.CompanyId == companyId && !so.IsDeleted)
                    .CountAsync();

                var pendingSOs = await _context.SalesOrders
                    .Where(so => so.CompanyId == companyId && !so.IsDeleted && so.Status == "Pending")
                    .CountAsync();

                var inProgressSOs = await _context.SalesOrders
                    .Where(so => so.CompanyId == companyId && !so.IsDeleted && so.Status == "In Progress")
                    .CountAsync();

                var pickedSOs = await _context.SalesOrders
                    .Where(so => so.CompanyId == companyId && !so.IsDeleted && so.Status == "Picked")
                    .CountAsync();

                var shippedSOs = await _context.SalesOrders
                    .Where(so => so.CompanyId == companyId && !so.IsDeleted && so.Status == "Shipped")
                    .CountAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        totalSOs,
                        pendingSOs,
                        inProgressSOs,
                        pickedSOs,
                        shippedSOs
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Sales Order dashboard statistics");
                return Json(new { success = false, message = "Error loading dashboard statistics" });
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// GET: api/salesorder
        /// Get paginated Sales Order list
        /// </summary>
        [HttpGet("api/salesorder")]
        [RequirePermission(Constants.SO_VIEW)]
        public async Task<IActionResult> GetSalesOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var query = _context.SalesOrders
                    .Where(so => so.CompanyId == companyId && !so.IsDeleted)
                    .Include(so => so.Customer)
                    .Include(so => so.SalesOrderDetails)
                        .ThenInclude(sod => sod.Item)
                    .AsQueryable();

                // Filter by status
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(so => so.Status == status);
                }

                // Search functionality
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(so => so.SONumber.Contains(search) ||
                                           (so.Customer != null && so.Customer.Name.Contains(search)));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var salesOrders = await query
                    .OrderByDescending(so => so.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(so => new
                    {
                        id = so.Id,
                        soNumber = so.SONumber,
                        customerId = so.CustomerId,
                        customerName = so.Customer != null ? so.Customer.Name : null,
                        orderDate = so.OrderDate,
                        requiredDate = so.RequiredDate,
                        status = so.Status,
                        totalAmount = so.TotalAmount,
                        holdingLocationId = so.HoldingLocationId,
                        holdingLocationName = so.HoldingLocation != null ? so.HoldingLocation.Name : null,
                        totalItems = so.SalesOrderDetails.Count,
                        totalQuantity = so.SalesOrderDetails.Sum(sod => sod.Quantity),
                        createdDate = so.CreatedDate,
                        createdBy = so.CreatedBy
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = salesOrders,
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
                _logger.LogError(ex, "Error getting Sales Order list");
                return Json(new { success = false, message = "Error loading Sales Order list" });
            }
        }

        /// <summary>
        /// GET: api/salesorder/{id}
        /// Get Sales Order details by ID
        /// </summary>
        [HttpGet("api/salesorder/{id}")]
        [RequirePermission(Constants.SO_VIEW)]
        public async Task<IActionResult> GetSalesOrder(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var salesOrder = await _context.SalesOrders
                    .Where(so => so.Id == id && so.CompanyId == companyId && !so.IsDeleted)
                    .Include(so => so.Customer)
                    .Include(so => so.HoldingLocation)
                    .Include(so => so.SalesOrderDetails)
                        .ThenInclude(sod => sod.Item)
                    .FirstOrDefaultAsync();

                if (salesOrder == null)
                {
                    return Json(new { success = false, message = "Sales Order not found" });
                }

                var result = new
                {
                    id = salesOrder.Id,
                    soNumber = salesOrder.SONumber,
                    customerId = salesOrder.CustomerId,
                    customerName = salesOrder.Customer?.Name ?? "N/A",
                    customerEmail = salesOrder.Customer?.Email ?? "N/A",
                    orderDate = salesOrder.OrderDate,
                    requiredDate = salesOrder.RequiredDate,
                    status = salesOrder.Status,
                    totalAmount = salesOrder.TotalAmount,
                    holdingLocationId = salesOrder.HoldingLocationId,
                    holdingLocationName = salesOrder.HoldingLocation?.Name ?? "N/A",
                    notes = salesOrder.Notes,
                    createdDate = salesOrder.CreatedDate,
                    createdBy = salesOrder.CreatedBy,
                    details = salesOrder.SalesOrderDetails.Select(sod => new
                    {
                        id = sod.Id,
                        itemId = sod.ItemId,
                        itemCode = sod.Item.ItemCode,
                        itemName = sod.Item.Name,
                        itemUnit = sod.Item.Unit,
                        quantity = sod.Quantity,
                        unitPrice = sod.UnitPrice,
                        totalPrice = sod.TotalPrice,
                        notes = sod.Notes
                    })
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Sales Order details for ID: {SalesOrderId}", id);
                return Json(new { success = false, message = "Error loading Sales Order details" });
            }
        }

        /// <summary>
        /// POST: api/salesorder
        /// Create new Sales Order
        /// </summary>
        [HttpPost("api/salesorder")]
        [RequirePermission(Constants.SO_MANAGE)]
        public async Task<IActionResult> CreateSalesOrder([FromBody] CreateSalesOrderRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => new { errorMessage = e.ErrorMessage }).ToArray()
                        );
                    
                    _logger.LogWarning("Sales Order creation failed validation: {@Errors}", errors);
                    return Json(new { success = false, message = "Validation failed", errors = errors });
                }

                var companyId = _currentUserService.CompanyId.Value;
                var userId = _currentUserService.UserId;

                _logger.LogInformation("Creating Sales Order for Customer {CustomerId}, Company {CompanyId}", request.CustomerId, companyId);

                // Validate Customer exists
                var customer = await _context.Customers
                    .Where(c => c.Id == request.CustomerId && c.CompanyId == companyId && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                if (customer == null)
                {
                    return Json(new { success = false, message = "Customer not found" });
                }

                // Validate items
                if (request.Items == null || !request.Items.Any())
                {
                    return Json(new { success = false, message = "At least one item must be provided" });
                }

                // Validate no duplicate items
                var duplicateItems = request.Items
                    .GroupBy(i => i.ItemId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateItems.Any())
                {
                    return Json(new { success = false, message = "Duplicate items are not allowed" });
                }

                // Validate holding location
                // Must be Other category (not Storage location)
                var holdingLocation = await _context.Locations
                    .FirstOrDefaultAsync(l => l.Id == request.HoldingLocationId && l.CompanyId == companyId && !l.IsDeleted && l.IsActive);

                if (holdingLocation == null)
                {
                    return Json(new { success = false, message = "Holding location not found or inactive" });
                }

                // Validate location is Other category (holding location)
                if (holdingLocation.Category != Constants.LOCATION_CATEGORY_OTHER)
                {
                    return Json(new { success = false, message = "Holding location must be of category 'Other', not 'Storage'" });
                }

                // Validate stock availability and item existence for each item
                foreach (var itemRequest in request.Items)
                {
                    // Validate item exists and get it (single query - more efficient)
                    var item = await _context.Items
                        .Where(i => i.Id == itemRequest.ItemId && 
                                   i.CompanyId == companyId && 
                                   !i.IsDeleted)
                        .FirstOrDefaultAsync();

                    if (item == null)
                    {
                        return Json(new { success = false, message = $"Item with ID {itemRequest.ItemId} not found" });
                    }

                    // Check stock availability (Storage locations only)
                    // Sales Order items must be picked from Storage locations
                    var totalStock = await _context.Inventories
                        .Where(i => i.ItemId == itemRequest.ItemId &&
                                   i.CompanyId == companyId &&
                                   !i.IsDeleted &&
                                   i.Status == Constants.INVENTORY_STATUS_AVAILABLE &&
                                   i.Quantity > 0 &&
                                   i.Location.Category == Constants.LOCATION_CATEGORY_STORAGE)
                        .SumAsync(i => i.Quantity);

                    if (totalStock < itemRequest.Quantity)
                    {
                        return Json(new { success = false, message = $"Insufficient stock for item {item.ItemCode}. Available: {totalStock}, Required: {itemRequest.Quantity}" });
                    }

                    // Use StandardPrice from item if not provided
                    if (itemRequest.UnitPrice <= 0)
                    {
                        itemRequest.UnitPrice = item.StandardPrice;
                    }
                }

                // Generate SO Number
                var soNumber = await GenerateSONumber(companyId);

                // Create Sales Order
                var salesOrder = new SalesOrder
                {
                    SONumber = soNumber,
                    CustomerId = request.CustomerId,
                    OrderDate = request.OrderDate ?? DateTime.Now,
                    RequiredDate = request.ExpectedArrivalDate,
                    Status = "Pending",
                    HoldingLocationId = request.HoldingLocationId,
                    TotalAmount = 0, // Will be calculated
                    Notes = request.Notes,
                    CompanyId = companyId,
                    CreatedBy = userId?.ToString() ?? "0",
                    CreatedDate = DateTime.Now
                };

                _context.SalesOrders.Add(salesOrder);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Sales Order saved successfully with ID: {SOId}", salesOrder.Id);

                // Create Sales Order Details
                decimal totalAmount = 0;
                foreach (var itemRequest in request.Items)
                {
                    var item = await _context.Items.FindAsync(itemRequest.ItemId);
                    var unitPrice = itemRequest.UnitPrice > 0 ? itemRequest.UnitPrice : item.StandardPrice;
                    var totalPrice = itemRequest.Quantity * unitPrice;

                    var detail = new SalesOrderDetail
                    {
                        SalesOrderId = salesOrder.Id,
                        ItemId = itemRequest.ItemId,
                        Quantity = itemRequest.Quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = totalPrice,
                        Notes = itemRequest.Notes,
                        CompanyId = companyId,
                        CreatedBy = userId?.ToString() ?? "0",
                        CreatedDate = DateTime.Now
                    };

                    detail.CalculateTotalPrice();
                    _context.SalesOrderDetails.Add(detail);
                    totalAmount += detail.TotalPrice;
                }

                // Update total amount
                salesOrder.TotalAmount = totalAmount;
                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync("CREATE", "SalesOrder", salesOrder.Id, 
                    $"Created Sales Order {salesOrder.SONumber}");

                transaction.Commit();
                _logger.LogInformation("Transaction committed successfully");

                return Json(new { success = true, message = "Sales Order created successfully", data = new { id = salesOrder.Id } });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating Sales Order");
                return Json(new { success = false, message = "Error creating Sales Order" });
            }
        }

        /// <summary>
        /// PUT: api/salesorder/{id}
        /// Update Sales Order (only if status is Pending)
        /// </summary>
        [HttpPut("api/salesorder/{id}")]
        [RequirePermission(Constants.SO_MANAGE)]
        public async Task<IActionResult> UpdateSalesOrder(int id, [FromBody] UpdateSalesOrderRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var companyId = _currentUserService.CompanyId.Value;
                var userId = _currentUserService.UserId;

                var salesOrder = await _context.SalesOrders
                    .Include(so => so.SalesOrderDetails)
                    .Where(so => so.Id == id && so.CompanyId == companyId && !so.IsDeleted)
                    .FirstOrDefaultAsync();

                if (salesOrder == null)
                {
                    return Json(new { success = false, message = "Sales Order not found" });
                }

                // Check if can be edited
                if (salesOrder.Status != "Pending")
                {
                    return Json(new { success = false, message = "Sales Order cannot be edited in current status" });
                }

                // Update basic fields
                if (request.ExpectedArrivalDate.HasValue)
                    salesOrder.RequiredDate = request.ExpectedArrivalDate.Value;
                
                if (request.HoldingLocationId.HasValue)
                    salesOrder.HoldingLocationId = request.HoldingLocationId.Value;
                
                if (request.Notes != null)
                    salesOrder.Notes = request.Notes;

                salesOrder.ModifiedBy = userId?.ToString() ?? "0";
                salesOrder.ModifiedDate = DateTime.Now;

                // Update details if provided
                if (request.Items != null && request.Items.Any())
                {
                    // Remove existing details
                    var existingDetails = salesOrder.SalesOrderDetails.ToList();
                    _context.SalesOrderDetails.RemoveRange(existingDetails);

                    // Validate items
                    var duplicateItems = request.Items
                        .GroupBy(i => i.ItemId)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicateItems.Any())
                    {
                        return Json(new { success = false, message = "Duplicate items are not allowed" });
                    }

                    // Add new details
                    decimal totalAmount = 0;
                    foreach (var itemRequest in request.Items)
                    {
                        var item = await _context.Items.FindAsync(itemRequest.ItemId);
                        var unitPrice = itemRequest.UnitPrice > 0 ? itemRequest.UnitPrice : item.StandardPrice;
                        var totalPrice = itemRequest.Quantity * unitPrice;

                        var detail = new SalesOrderDetail
                        {
                            SalesOrderId = salesOrder.Id,
                            ItemId = itemRequest.ItemId,
                            Quantity = itemRequest.Quantity,
                            UnitPrice = unitPrice,
                            TotalPrice = totalPrice,
                            Notes = itemRequest.Notes,
                            CompanyId = companyId,
                            CreatedBy = userId?.ToString() ?? "0",
                            CreatedDate = DateTime.Now
                        };

                        detail.CalculateTotalPrice();
                        _context.SalesOrderDetails.Add(detail);
                        totalAmount += detail.TotalPrice;
                    }

                    salesOrder.TotalAmount = totalAmount;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditService.LogActionAsync("UPDATE", "SalesOrder", salesOrder.Id, 
                    $"Updated Sales Order {salesOrder.SONumber}");

                return Json(new { success = true, message = "Sales Order updated successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating Sales Order with ID: {SalesOrderId}", id);
                return Json(new { success = false, message = "Error updating Sales Order" });
            }
        }

        /// <summary>
        /// PATCH: api/salesorder/{id}/cancel
        /// Cancel Sales Order
        /// Case 1: Status Pending (no Picking) → Cancel SO only
        /// Case 2: Status In Progress with Picking Pending → Cancel SO and Picking
        /// </summary>
        [HttpPatch("api/salesorder/{id}/cancel")]
        [RequirePermission(Constants.SO_MANAGE)]
        public async Task<IActionResult> CancelSalesOrder(int id, [FromBody] CancelRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var companyId = _currentUserService.CompanyId.Value;
                var userId = _currentUserService.UserId;

                // Validate request
                if (string.IsNullOrWhiteSpace(request?.Reason))
                {
                    return Json(new { success = false, message = "Reason is required" });
                }

                var salesOrder = await _context.SalesOrders
                    .Where(so => so.Id == id && so.CompanyId == companyId && !so.IsDeleted)
                    .Include(so => so.SalesOrderDetails)
                    .FirstOrDefaultAsync();

                if (salesOrder == null)
                {
                    return Json(new { success = false, message = "Sales Order not found" });
                }

                // Validate: Only can cancel if status is Pending or In Progress
                if (salesOrder.Status != "Pending" && salesOrder.Status != "In Progress")
                {
                    return Json(new { 
                        success = false, 
                        message = $"Sales Order cannot be cancelled. Current status: {salesOrder.Status}. Only Pending or In Progress status can be cancelled." 
                    });
                }

                // Check for existing Picking
                var existingPicking = await _context.Pickings
                    .Where(p => p.SalesOrderId == id && 
                               p.CompanyId == companyId && 
                               !p.IsDeleted &&
                               p.Status != Constants.PICKING_STATUS_CANCELLED)
                    .Include(p => p.PickingDetails)
                    .FirstOrDefaultAsync();

                // Store old Notes for audit
                var oldNotes = salesOrder.Notes;

                // Case 1: No Picking exists (Status = Pending)
                if (existingPicking == null)
                {
                    if (salesOrder.Status != "Pending")
                    {
                        return Json(new { 
                            success = false, 
                            message = "Sales Order status is not Pending but no Picking found. Cannot determine cancellation rules." 
                        });
                    }

                    // Cancel the Sales Order and add cancellation reason to Notes
                    salesOrder.Status = Constants.SO_STATUS_CANCELLED;
                    salesOrder.Notes = FormatCancellationNotes(salesOrder.Notes, request.Reason);
                    salesOrder.ModifiedBy = userId?.ToString() ?? "0";
                    salesOrder.ModifiedDate = DateTime.Now;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await _auditService.LogActionAsync("CANCEL", "SalesOrder", salesOrder.Id, 
                        $"Cancelled Sales Order {salesOrder.SONumber} (no Picking created). Reason: {request.Reason}",
                        new { Status = "Pending", Notes = oldNotes },
                        new { Status = Constants.SO_STATUS_CANCELLED, Notes = salesOrder.Notes });

                    _logger.LogInformation("Sales Order cancelled (no Picking). ID: {SOId}, SO Number: {SONumber}, Reason: {Reason}", 
                        id, salesOrder.SONumber, request.Reason);

                    return Json(new { 
                        success = true, 
                        message = "Sales Order cancelled successfully",
                        data = new { 
                            soId = salesOrder.Id, 
                            soNumber = salesOrder.SONumber, 
                            status = salesOrder.Status,
                            pickingCancelled = false
                        }
                    });
                }

                // Case 2: Picking exists
                if (existingPicking.Status != Constants.PICKING_STATUS_PENDING)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Sales Order cannot be cancelled because Picking status is {existingPicking.Status}. Only Pending Picking can be cancelled along with Sales Order." 
                    });
                }

                if (salesOrder.Status != "In Progress")
                {
                    return Json(new { 
                        success = false, 
                        message = $"Sales Order status must be 'In Progress' when Picking exists. Current status: {salesOrder.Status}" 
                    });
                }

                // Cancel the Picking
                existingPicking.Status = Constants.PICKING_STATUS_CANCELLED;
                existingPicking.ModifiedBy = userId?.ToString() ?? "0";
                existingPicking.ModifiedDate = DateTime.Now;

                foreach (var pickingDetail in existingPicking.PickingDetails)
                {
                    pickingDetail.ModifiedBy = userId?.ToString() ?? "0";
                    pickingDetail.ModifiedDate = DateTime.Now;
                }

                // Cancel the Sales Order and add cancellation reason to Notes
                salesOrder.Status = Constants.SO_STATUS_CANCELLED;
                salesOrder.Notes = FormatCancellationNotes(salesOrder.Notes, request.Reason);
                salesOrder.ModifiedBy = userId?.ToString() ?? "0";
                salesOrder.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Log audit trail
                await _auditService.LogActionAsync("CANCEL", "Picking", existingPicking.Id, 
                    $"Cancelled Picking {existingPicking.PickingNumber} due to Sales Order cancellation");

                await _auditService.LogActionAsync("CANCEL", "SalesOrder", salesOrder.Id, 
                    $"Cancelled Sales Order {salesOrder.SONumber} (Picking {existingPicking.PickingNumber} also cancelled). Reason: {request.Reason}",
                    new { Status = "In Progress", Notes = oldNotes },
                    new { Status = Constants.SO_STATUS_CANCELLED, Notes = salesOrder.Notes });

                _logger.LogInformation("Sales Order and Picking cancelled. SO ID: {SOId}, SO Number: {SONumber}, Picking ID: {PickingId}, Reason: {Reason}", 
                    id, salesOrder.SONumber, existingPicking.Id, request.Reason);

                return Json(new { 
                    success = true, 
                    message = "Sales Order and Picking cancelled successfully",
                    data = new { 
                        soId = salesOrder.Id, 
                        soNumber = salesOrder.SONumber, 
                        soStatus = salesOrder.Status,
                        pickingId = existingPicking.Id,
                        pickingNumber = existingPicking.PickingNumber,
                        pickingStatus = existingPicking.Status,
                        pickingCancelled = true
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling Sales Order with ID: {SOId}", id);
                return Json(new { success = false, message = "Error cancelling Sales Order" });
            }
        }

        /// <summary>
        /// PATCH: api/salesorder/{id}/status
        /// Update Sales Order status
        /// Special handling for Shipped status (only from Picked)
        /// When shipping, reduce inventory from holding location
        /// </summary>
        [HttpPatch("api/salesorder/{id}/status")]
        [RequirePermission(Constants.SO_MANAGE)]
        public async Task<IActionResult> UpdateSalesOrderStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var companyId = _currentUserService.CompanyId.Value;
                var userId = _currentUserService.UserId;

                var salesOrder = await _context.SalesOrders
                    .Include(so => so.HoldingLocation)
                    .Include(so => so.SalesOrderDetails)
                        .ThenInclude(sod => sod.Item)
                    .Where(so => so.Id == id && so.CompanyId == companyId && !so.IsDeleted)
                    .FirstOrDefaultAsync();

                if (salesOrder == null)
                {
                    return Json(new { success = false, message = "Sales Order not found" });
                }

                // Validate status transition: Only allow Picked -> Shipped
                if (request.Status == Constants.SO_STATUS_SHIPPED || request.Status == "Shipped")
                {
                    if (salesOrder.Status != "Picked")
                    {
                        return Json(new { success = false, message = "Sales Order must be in 'Picked' status before it can be shipped" });
                    }

                    // Validate holding location exists
                    if (!salesOrder.HoldingLocationId.HasValue || salesOrder.HoldingLocation == null)
                    {
                        return Json(new { success = false, message = "Holding location not set for this Sales Order" });
                    }

                    var holdingLocation = salesOrder.HoldingLocation;

                    // Reduce inventory from holding location for each Sales Order Detail
                    foreach (var soDetail in salesOrder.SalesOrderDetails)
                    {
                        // Find inventory in holding location for this item
                        var holdingInventory = await _context.Inventories
                            .Where(i => i.ItemId == soDetail.ItemId &&
                                       i.LocationId == holdingLocation.Id &&
                                       i.CompanyId == companyId &&
                                       !i.IsDeleted &&
                                       i.Status == Constants.INVENTORY_STATUS_AVAILABLE)
                            .FirstOrDefaultAsync();

                        if (holdingInventory == null)
                        {
                            return Json(new { success = false, message = $"No available inventory found for item {soDetail.Item.ItemCode} at holding location {holdingLocation.Code}" });
                        }

                        // Validate sufficient stock
                        if (holdingInventory.Quantity < soDetail.Quantity)
                        {
                            return Json(new { success = false, message = $"Insufficient stock for item {soDetail.Item.ItemCode} at holding location. Available: {holdingInventory.Quantity}, Required: {soDetail.Quantity}" });
                        }

                        // Reduce inventory quantity
                        holdingInventory.Quantity -= soDetail.Quantity;
                        
                        // Auto-update status berdasarkan quantity
                        if (holdingInventory.Quantity > 0)
                        {
                            holdingInventory.Status = Constants.INVENTORY_STATUS_AVAILABLE;
                        }
                        else
                        {
                            holdingInventory.Status = Constants.INVENTORY_STATUS_EMPTY;
                        }
                        
                        holdingInventory.ModifiedBy = userId?.ToString() ?? "0";
                        holdingInventory.ModifiedDate = DateTime.Now;

                        // Reduce holding location capacity
                        holdingLocation.CurrentCapacity -= soDetail.Quantity;
                        holdingLocation.ModifiedBy = userId?.ToString() ?? "0";
                        holdingLocation.ModifiedDate = DateTime.Now;
                    }
                }

                var oldStatus = salesOrder.Status;
                salesOrder.Status = request.Status;
                salesOrder.ModifiedBy = userId?.ToString() ?? "0";
                salesOrder.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditService.LogActionAsync("UPDATE_STATUS", "SalesOrder", salesOrder.Id, 
                    $"Changed Sales Order {salesOrder.SONumber} status from {oldStatus} to {request.Status}");

                return Json(new { success = true, message = "Sales Order status updated successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating Sales Order status for ID: {SalesOrderId}", id);
                return Json(new { success = false, message = "Error updating Sales Order status" });
            }
        }

        #endregion

        #region Helper API Endpoints

        /// <summary>
        /// GET: api/salesorder/customers
        /// Get list of customers for dropdown
        /// </summary>
        [HttpGet("api/salesorder/customers")]
        [RequirePermission(Constants.SO_VIEW)]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var customers = await _context.Customers
                    .Where(c => c.CompanyId == companyId && !c.IsDeleted && c.IsActive)
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        email = c.Email,
                        phone = c.Phone,
                        code = c.Code
                    })
                    .OrderBy(c => c.name)
                    .ToListAsync();

                return Json(new { success = true, data = customers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                return Json(new { success = false, message = "Error loading customers" });
            }
        }

        /// <summary>
        /// POST: api/salesorder/customers/search
        /// Advanced search for customers
        /// </summary>
        [HttpPost("api/salesorder/customers/search")]
        [RequirePermission(Constants.SO_VIEW)]
        public async Task<IActionResult> SearchCustomersAdvanced([FromBody] CustomerAdvancedSearchRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var query = _context.Customers
                    .Where(c => c.CompanyId == companyId && !c.IsDeleted)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(request.Name))
                {
                    query = query.Where(c => EF.Functions.Like(c.Name, $"%{request.Name}%"));
                }

                if (!string.IsNullOrEmpty(request.Email))
                {
                    query = query.Where(c => EF.Functions.Like(c.Email, $"%{request.Email}%"));
                }

                if (!string.IsNullOrEmpty(request.Phone))
                {
                    query = query.Where(c => EF.Functions.Like(c.Phone, $"%{request.Phone}%"));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                var results = await query
                    .OrderBy(c => c.Name)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(c => new CustomerSearchResult
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Email = c.Email,
                        Phone = c.Phone,
                        Code = c.Code,
                        IsActive = c.IsActive
                    })
                    .ToListAsync();

                return Json(new CustomerAdvancedSearchResponse
                {
                    Success = true,
                    Data = results,
                    TotalCount = totalCount,
                    CurrentPage = request.Page,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced customer search");
                return Json(new CustomerAdvancedSearchResponse
                {
                    Success = false,
                    Message = "Error performing search"
                });
            }
        }

        /// <summary>
        /// POST: api/salesorder/items/advanced-search
        /// Advanced item search endpoint for Sales Order (Inventory-based)
        /// </summary>
        [HttpPost("api/salesorder/items/advanced-search")]
        [RequirePermission(Constants.SO_VIEW)]
        public async Task<IActionResult> SearchItemsAdvanced([FromBody] SOItemAdvancedSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Advanced item search started for Sales Order. Request: {@Request}", request);
                
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    _logger.LogWarning("No company context found for user");
                    return Json(new SOItemAdvancedSearchResponse
                    {
                        Success = false,
                        Message = "No company context found"
                    });
                }

                // Get items that have available stock in inventory (Storage locations only)
                // Sales Order items must be picked from Storage locations, not holding locations
                var itemsWithStock = await _context.Inventories
                    .Where(i => i.CompanyId == companyId.Value &&
                               !i.IsDeleted &&
                               i.Status == Constants.INVENTORY_STATUS_AVAILABLE &&
                               i.Quantity > 0 &&
                               i.Location.Category == Constants.LOCATION_CATEGORY_STORAGE)
                    .Select(i => i.ItemId)
                    .Distinct()
                    .ToListAsync();

                var query = _context.Items
                    .Where(i => i.CompanyId == companyId.Value && 
                               !i.IsDeleted && 
                               i.IsActive &&
                               itemsWithStock.Contains(i.Id))
                    .AsQueryable();

                // Apply search filters
                if (!string.IsNullOrEmpty(request.ItemCode))
                {
                    query = query.Where(i => EF.Functions.Like(i.ItemCode, $"%{request.ItemCode}%"));
                }

                if (!string.IsNullOrEmpty(request.Name))
                {
                    query = query.Where(i => EF.Functions.Like(i.Name, $"%{request.Name}%"));
                }

                if (!string.IsNullOrEmpty(request.Unit))
                {
                    query = query.Where(i => EF.Functions.Like(i.Unit, $"%{request.Unit}%"));
                }

                if (request.CreatedDateFrom.HasValue)
                {
                    query = query.Where(i => i.CreatedDate >= request.CreatedDateFrom.Value);
                }

                if (request.CreatedDateTo.HasValue)
                {
                    // Add one day to include the entire end date
                    var endDate = request.CreatedDateTo.Value.AddDays(1);
                    query = query.Where(i => i.CreatedDate < endDate);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var items = await query
                    .OrderBy(i => i.Name)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                // Calculate total stock for each item (Storage locations only)
                // Available Stock untuk Sales Order hanya dari Storage locations
                var itemResults = new List<SOItemSearchResult>();
                foreach (var item in items)
                {
                    var totalStock = await _context.Inventories
                        .Where(i => i.ItemId == item.Id &&
                                   i.CompanyId == companyId.Value &&
                                   !i.IsDeleted &&
                                   i.Status == Constants.INVENTORY_STATUS_AVAILABLE &&
                                   i.Quantity > 0 &&
                                   i.Location.Category == Constants.LOCATION_CATEGORY_STORAGE)
                        .SumAsync(i => i.Quantity);

                    itemResults.Add(new SOItemSearchResult
                    {
                        Id = item.Id,
                        ItemCode = item.ItemCode,
                        Name = item.Name,
                        Unit = item.Unit,
                        StandardPrice = item.StandardPrice,
                        TotalStock = totalStock,
                        CreatedDate = item.CreatedDate,
                        IsActive = item.IsActive
                    });
                }

                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                _logger.LogInformation("Found {Count} items matching criteria with available stock", itemResults.Count);

                return Json(new SOItemAdvancedSearchResponse
                {
                    Success = true,
                    Data = itemResults,
                    TotalCount = totalCount,
                    CurrentPage = request.Page,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced item search for Sales Order");
                return Json(new SOItemAdvancedSearchResponse
                {
                    Success = false,
                    Message = "Error performing search"
                });
            }
        }

        /// <summary>
        /// GET: api/salesorder/items/search
        /// Search items from inventory (only items with available stock)
        /// </summary>
        [HttpGet("api/salesorder/items/search")]
        [RequirePermission(Constants.SO_VIEW)]
        public async Task<IActionResult> SearchItems(string q)
        {
            try
            {
                if (string.IsNullOrEmpty(q) || q.Length < 2)
                {
                    return Json(new List<object>());
                }

                var companyId = _currentUserService.CompanyId.Value;

                // Get items that have available inventory (Storage locations only)
                // Sales Order items must be available in Storage locations
                var itemsWithStock = await _context.Inventories
                    .Where(i => i.CompanyId == companyId &&
                               !i.IsDeleted &&
                               i.Status == Constants.INVENTORY_STATUS_AVAILABLE &&
                               i.Quantity > 0 &&
                               i.Location.Category == Constants.LOCATION_CATEGORY_STORAGE)
                    .Select(i => i.ItemId)
                    .Distinct()
                    .ToListAsync();

                var items = await _context.Items
                    .Where(i => i.CompanyId == companyId &&
                               !i.IsDeleted &&
                               i.IsActive &&
                               itemsWithStock.Contains(i.Id) &&
                               (EF.Functions.Like(i.ItemCode, $"%{q}%") ||
                                EF.Functions.Like(i.Name, $"%{q}%")))
                    .Select(i => new
                    {
                        id = i.Id,
                        itemCode = i.ItemCode,
                        name = i.Name,
                        unit = i.Unit,
                        standardPrice = i.StandardPrice,
                        purchasePrice = i.PurchasePrice,
                        totalStock = _context.Inventories
                            .Where(inv => inv.ItemId == i.Id &&
                                         inv.CompanyId == companyId &&
                                         !inv.IsDeleted &&
                                         inv.Status == Constants.INVENTORY_STATUS_AVAILABLE &&
                                         inv.Quantity > 0 &&
                                         inv.Location.Category == Constants.LOCATION_CATEGORY_STORAGE)
                            .Sum(inv => inv.Quantity)
                    })
                    .Take(10)
                    .ToListAsync();

                return Json(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching items for Sales Order");
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// GET: api/salesorder/items/{itemId}/stock
        /// Get total available stock for an item
        /// </summary>
        [HttpGet("api/salesorder/items/{itemId}/stock")]
        [RequirePermission(Constants.SO_VIEW)]
        public async Task<IActionResult> GetItemStock(int itemId)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                // Get total stock from Storage locations only
                // Sales Order items must be picked from Storage locations
                var totalStock = await _context.Inventories
                    .Where(i => i.ItemId == itemId &&
                               i.CompanyId == companyId &&
                               !i.IsDeleted &&
                               i.Status == Constants.INVENTORY_STATUS_AVAILABLE &&
                               i.Quantity > 0 &&
                               i.Location.Category == Constants.LOCATION_CATEGORY_STORAGE)
                    .SumAsync(i => i.Quantity);

                // Get stock breakdown by Storage locations only
                var stockByLocation = await _context.Inventories
                    .Where(i => i.ItemId == itemId &&
                               i.CompanyId == companyId &&
                               !i.IsDeleted &&
                               i.Status == Constants.INVENTORY_STATUS_AVAILABLE &&
                               i.Quantity > 0 &&
                               i.Location.Category == Constants.LOCATION_CATEGORY_STORAGE)
                    .Include(i => i.Location)
                    .Select(i => new
                    {
                        locationId = i.LocationId,
                        locationCode = i.Location.Code,
                        locationName = i.Location.Name,
                        quantity = i.Quantity
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        itemId,
                        totalStock,
                        stockByLocation
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item stock for ItemId: {ItemId}", itemId);
                return Json(new { success = false, message = "Error loading item stock" });
            }
        }

        /// <summary>
        /// GET: api/salesorder/locations
        /// Get holding locations (Other category only)
        /// </summary>
        [HttpGet("api/salesorder/locations")]
        [RequirePermission(Constants.SO_VIEW)]
        public async Task<IActionResult> GetLocations()
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                // Only show Other category locations for holding (not Storage locations)
                var locations = await _context.Locations
                    .Where(l => l.CompanyId == companyId && 
                               !l.IsDeleted && 
                               l.IsActive &&
                               l.Category == Constants.LOCATION_CATEGORY_OTHER)
                    .Select(l => new
                    {
                        id = l.Id,
                        name = l.Name,
                        code = l.Code,
                        currentCapacity = l.CurrentCapacity,
                        maxCapacity = l.MaxCapacity,
                        availableCapacity = l.MaxCapacity - l.CurrentCapacity
                    })
                    .OrderBy(l => l.name)
                    .ToListAsync();

                return Json(new { success = true, data = locations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting locations");
                return Json(new { success = false, message = "Error loading locations" });
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Format Notes dengan cancellation reason
        /// Format: "Cancelation reason : [Reason]"
        /// Jika Notes sudah ada, append dengan "\n\nCancelation reason : [Reason]"
        /// </summary>
        private string FormatCancellationNotes(string? existingNotes, string cancellationReason)
        {
            const string prefix = "Cancelation reason : ";
            string formattedReason = $"{prefix}{cancellationReason.Trim()}";
            
            const int maxLength = 500;
            int availableSpace = maxLength;
            
            if (!string.IsNullOrWhiteSpace(existingNotes))
            {
                string separator = "\n\n";
                availableSpace = maxLength - existingNotes.Length - separator.Length;
                
                if (availableSpace < formattedReason.Length)
                {
                    int maxReasonLength = Math.Max(0, availableSpace - prefix.Length);
                    if (maxReasonLength > 0)
                    {
                        formattedReason = $"{prefix}{cancellationReason.Trim().Substring(0, maxReasonLength)}";
                    }
                    else
                    {
                        formattedReason = prefix;
                    }
                }
                
                return $"{existingNotes}{separator}{formattedReason}";
            }
            else
            {
                if (formattedReason.Length > maxLength)
                {
                    int maxReasonLength = maxLength - prefix.Length;
                    if (maxReasonLength > 0)
                    {
                        formattedReason = $"{prefix}{cancellationReason.Trim().Substring(0, maxReasonLength)}";
                    }
                    else
                    {
                        formattedReason = prefix;
                    }
                }
                
                return formattedReason;
            }
        }

        private async Task<string> GenerateSONumber(int companyId)
        {
            try
            {
                var today = DateTime.Now;
                var prefix = $"SO{today:yyyyMMdd}";
                
                var lastSO = await _context.SalesOrders
                    .Where(so => so.CompanyId == companyId &&
                               so.SONumber.StartsWith(prefix) &&
                               !so.IsDeleted)
                    .OrderByDescending(so => so.SONumber)
                    .FirstOrDefaultAsync();

                if (lastSO == null)
                {
                    return $"{prefix}001";
                }

                var lastNumber = lastSO.SONumber.Substring(prefix.Length);
                if (int.TryParse(lastNumber, out int number))
                {
                    return $"{prefix}{(number + 1):D3}";
                }

                return $"{prefix}001";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SO number for company {CompanyId}", companyId);
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                return $"SO{timestamp}";
            }
        }

        #endregion

        #region Request Models

        public class CreateSalesOrderRequest
        {
            [Required]
            public int CustomerId { get; set; }
            
            public DateTime? OrderDate { get; set; }
            
            public DateTime? ExpectedArrivalDate { get; set; }
            
            [Required(ErrorMessage = "Holding location is required")]
            public int HoldingLocationId { get; set; }
            
            [StringLength(500)]
            public string? Notes { get; set; }

            public List<SalesOrderItemRequest> Items { get; set; } = new List<SalesOrderItemRequest>();
        }

        public class SalesOrderItemRequest
        {
            [Required]
            public int ItemId { get; set; }

            [Required]
            [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
            public int Quantity { get; set; }

            [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
            public decimal UnitPrice { get; set; }

            [StringLength(200)]
            public string? Notes { get; set; }
        }

        public class UpdateSalesOrderRequest
        {
            public DateTime? ExpectedArrivalDate { get; set; }
            
            public int? HoldingLocationId { get; set; }
            
            [StringLength(500)]
            public string? Notes { get; set; }
            
            public List<SalesOrderItemRequest>? Items { get; set; }
        }

        public class UpdateStatusRequest
        {
            [Required]
            [StringLength(50)]
            public string Status { get; set; } = string.Empty;
        }

        public class CustomerAdvancedSearchRequest
        {
            public string? Name { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 10;
        }

        public class CustomerSearchResult
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string? Code { get; set; }
            public bool IsActive { get; set; }
        }

        public class CustomerAdvancedSearchResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public List<CustomerSearchResult> Data { get; set; } = new();
            public int TotalCount { get; set; }
            public int CurrentPage { get; set; }
            public int TotalPages { get; set; }
        }

        // Item Advanced Search Models
        public class SOItemAdvancedSearchRequest
        {
            public string? ItemCode { get; set; }
            public string? Name { get; set; }
            public string? Unit { get; set; }
            public DateTime? CreatedDateFrom { get; set; }
            public DateTime? CreatedDateTo { get; set; }
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 10;
        }

        public class SOItemAdvancedSearchResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public List<SOItemSearchResult> Data { get; set; } = new List<SOItemSearchResult>();
            public int TotalCount { get; set; }
            public int TotalPages { get; set; }
            public int CurrentPage { get; set; }
        }

        public class SOItemSearchResult
        {
            public int Id { get; set; }
            public string ItemCode { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public decimal StandardPrice { get; set; }
            public int TotalStock { get; set; }
            public DateTime CreatedDate { get; set; }
            public bool IsActive { get; set; }
        }

        #endregion
    }
}
