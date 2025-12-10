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
    /// Controller untuk Purchase Order management - Hybrid MVC + API
    /// </summary>
    [RequirePermission(Constants.PO_MANAGE)]
    public class PurchaseOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly IAuditTrailService _auditService;
        private readonly ILogger<PurchaseOrderController> _logger;
        private readonly IConfiguration _configuration;

        public PurchaseOrderController(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            IAuditTrailService auditService,
            ILogger<PurchaseOrderController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _currentUserService = currentUserService;
            _emailService = emailService;
            _auditService = auditService;
            _logger = logger;
            _configuration = configuration;
        }

        #region MVC Actions

        /// <summary>
        /// GET: /PurchaseOrder
        /// Purchase Order management index page
        /// </summary>
        public IActionResult Index()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading purchase order index page");
                return View("Error");
            }
        }

        #endregion

        #region Dashboard & Statistics

        /// <summary>
        /// GET: api/purchaseorder/dashboard
        /// Get purchase order statistics for dashboard
        /// </summary>
        [HttpGet("api/purchaseorder/dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var purchaseOrders = await _context.PurchaseOrders
                    .Where(po => po.CompanyId == companyId.Value && !po.IsDeleted)
                    .ToListAsync();

                var statistics = new
                {
                    totalPurchaseOrders = purchaseOrders.Count,
                    draftOrders = purchaseOrders.Count(po => po.Status == "Draft"),
                    sentOrders = purchaseOrders.Count(po => po.Status == "Sent"),
                    receivedOrders = purchaseOrders.Count(po => po.Status == "Received"),
                    cancelledOrders = purchaseOrders.Count(po => po.Status == "Cancelled"),
                    totalValue = purchaseOrders.Sum(po => po.TotalAmount),
                    averageOrderValue = purchaseOrders.Any() ? purchaseOrders.Average(po => po.TotalAmount) : 0
                };

                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting purchase order dashboard statistics");
                return StatusCode(500, new { success = false, message = "Error loading dashboard statistics" });
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// GET: api/purchaseorder
        /// Get paginated list of purchase orders with filters
        /// </summary>
        [HttpGet("api/purchaseorder")]
        public async Task<IActionResult> GetPurchaseOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? supplier = null)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var query = _context.PurchaseOrders
                    .Where(po => po.CompanyId == companyId.Value && !po.IsDeleted)
                    .Include(po => po.Supplier)
                    .Include(po => po.PurchaseOrderDetails)
                        .ThenInclude(pod => pod.Item)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(po => 
                        po.PONumber.Contains(search) || 
                        po.Supplier.Name.Contains(search) ||
                        (po.Notes != null && po.Notes.Contains(search)));
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(po => po.Status == status);
                }

                // Apply supplier filter
                if (!string.IsNullOrEmpty(supplier))
                {
                    query = query.Where(po => po.Supplier.Name.Contains(supplier));
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var purchaseOrders = await query
                    .OrderByDescending(po => po.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(po => new
                    {
                        id = po.Id,
                        poNumber = po.PONumber,
                        supplierName = po.Supplier.Name,
                        supplierEmail = po.Supplier.Email,
                        orderDate = po.OrderDate,
                        expectedDeliveryDate = po.ExpectedDeliveryDate,
                        status = po.Status,
                        totalAmount = po.TotalAmount,
                        itemCount = po.PurchaseOrderDetails.Count,
                        notes = po.Notes,
                        createdDate = po.CreatedDate,
                        modifiedDate = po.ModifiedDate,
                        createdBy = po.CreatedBy,
                        modifiedBy = po.ModifiedBy
                    })
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return Ok(new
                {
                    success = true,
                    data = purchaseOrders,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting purchase orders");
                return StatusCode(500, new { success = false, message = "Error loading purchase orders" });
            }
        }

        /// <summary>
        /// GET: api/purchaseorder/{id}
        /// Get specific purchase order by ID
        /// </summary>
        [HttpGet("api/purchaseorder/{id}")]
        public async Task<IActionResult> GetPurchaseOrder(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var purchaseOrder = await _context.PurchaseOrders
                    .Where(po => po.Id == id && po.CompanyId == companyId.Value && !po.IsDeleted)
                    .Include(po => po.Supplier)
                    .Include(po => po.PurchaseOrderDetails)
                        .ThenInclude(pod => pod.Item)
                    .FirstOrDefaultAsync();

                if (purchaseOrder == null)
                {
                    return NotFound(new { success = false, message = "Purchase Order not found" });
                }

                var result = new
                {
                    id = purchaseOrder.Id,
                    poNumber = purchaseOrder.PONumber,
                    supplierId = purchaseOrder.SupplierId,
                    supplierName = purchaseOrder.Supplier.Name,
                    supplierEmail = purchaseOrder.Supplier.Email,
                    orderDate = purchaseOrder.OrderDate,
                    expectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
                    status = purchaseOrder.Status,
                    totalAmount = purchaseOrder.TotalAmount,
                    notes = purchaseOrder.Notes,
                    createdDate = purchaseOrder.CreatedDate,
                    modifiedDate = purchaseOrder.ModifiedDate,
                    createdBy = purchaseOrder.CreatedBy,
                    modifiedBy = purchaseOrder.ModifiedBy,
                    details = purchaseOrder.PurchaseOrderDetails.Select(pod => new
                    {
                        id = pod.Id,
                        itemId = pod.ItemId,
                        itemName = pod.Item.Name,
                        itemCode = pod.Item.ItemCode,
                        itemUnit = pod.Item.Unit,
                        quantity = pod.Quantity,
                        unitPrice = pod.UnitPrice,
                        totalPrice = pod.TotalPrice,
                        notes = pod.Notes
                    }).ToList()
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting purchase order with ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error loading purchase order" });
            }
        }

        /// <summary>
        /// POST: api/purchaseorder
        /// Create new purchase order
        /// </summary>
        [HttpPost("api/purchaseorder")]
        public async Task<IActionResult> CreatePurchaseOrder([FromBody] PurchaseOrderCreateRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                var userId = _currentUserService.UserId;

                if (!companyId.HasValue || !userId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company or user context found" });
                }

                // Validate request
                if (request == null)
                {
                    return BadRequest(new { success = false, message = "Invalid request data" });
                }

                if (request.SupplierId <= 0)
                {
                    return BadRequest(new { success = false, message = "Supplier is required" });
                }

                if (request.Details == null || !request.Details.Any())
                {
                    return BadRequest(new { success = false, message = "At least one item is required" });
                }

                // Validate supplier exists
                var supplier = await _context.Suppliers
                    .Where(s => s.Id == request.SupplierId && s.CompanyId == companyId.Value && !s.IsDeleted)
                    .FirstOrDefaultAsync();

                if (supplier == null)
                {
                    return BadRequest(new { success = false, message = "Supplier not found" });
                }

                // Generate PO Number
                var poNumber = await GeneratePONumberAsync(companyId.Value);

                // Create purchase order
                var purchaseOrder = new PurchaseOrder
                {
                    CompanyId = companyId.Value,
                    PONumber = poNumber,
                    SupplierId = request.SupplierId,
                    OrderDate = request.OrderDate ?? DateTime.Today,
                    ExpectedDeliveryDate = request.ExpectedDeliveryDate,
                    Status = "Draft",
                    Notes = request.Notes,
                    TotalAmount = 0, // Will be calculated
                    CreatedBy = userId.Value.ToString(),
                    CreatedDate = DateTime.UtcNow
                };

                _context.PurchaseOrders.Add(purchaseOrder);
                await _context.SaveChangesAsync();

                // Create purchase order details
                decimal totalAmount = 0;
                foreach (var detailRequest in request.Details)
                {
                    // Validate item exists
                    var item = await _context.Items
                        .Where(i => i.Id == detailRequest.ItemId && i.CompanyId == companyId.Value && !i.IsDeleted)
                        .FirstOrDefaultAsync();

                    if (item == null)
                    {
                        return BadRequest(new { success = false, message = $"Item with ID {detailRequest.ItemId} not found" });
                    }

                    var detail = new PurchaseOrderDetail
                    {
                        PurchaseOrderId = purchaseOrder.Id,
                        ItemId = detailRequest.ItemId,
                        Quantity = detailRequest.Quantity,
                        UnitPrice = detailRequest.UnitPrice,
                        TotalPrice = detailRequest.Quantity * detailRequest.UnitPrice,
                        Notes = detailRequest.Notes,
                        CreatedBy = userId.Value.ToString(),
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.PurchaseOrderDetails.Add(detail);
                    totalAmount += detail.TotalPrice;
                }

                // Update total amount
                purchaseOrder.TotalAmount = totalAmount;
                purchaseOrder.ModifiedBy = userId.Value.ToString();
                purchaseOrder.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("CREATE", "PurchaseOrder", purchaseOrder.Id, 
                        $"{purchaseOrder.PONumber} - {purchaseOrder.Supplier?.Name}", null, new { 
                            PONumber = purchaseOrder.PONumber,
                            SupplierId = purchaseOrder.SupplierId,
                            OrderDate = purchaseOrder.OrderDate,
                            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
                            TotalAmount = purchaseOrder.TotalAmount,
                            Status = purchaseOrder.Status,
                            ItemCount = request.Details?.Count ?? 0
                        });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for purchase order creation");
                }

                _logger.LogInformation("Purchase Order created successfully. ID: {Id}, PO Number: {PONumber}", 
                    purchaseOrder.Id, purchaseOrder.PONumber);

                return Ok(new
                {
                    success = true,
                    message = "Purchase Order created successfully",
                    data = new
                    {
                        id = purchaseOrder.Id,
                        poNumber = purchaseOrder.PONumber,
                        totalAmount = purchaseOrder.TotalAmount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating purchase order");
                return StatusCode(500, new { success = false, message = "Error creating purchase order" });
            }
        }

        /// <summary>
        /// PUT: api/purchaseorder/{id}
        /// Update existing purchase order
        /// </summary>
        [HttpPut("api/purchaseorder/{id}")]
        public async Task<IActionResult> UpdatePurchaseOrder(int id, [FromBody] PurchaseOrderUpdateRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                var userId = _currentUserService.UserId;

                if (!companyId.HasValue || !userId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company or user context found" });
                }

                var purchaseOrder = await _context.PurchaseOrders
                    .Where(po => po.Id == id && po.CompanyId == companyId.Value && !po.IsDeleted)
                    .FirstOrDefaultAsync();

                if (purchaseOrder == null)
                {
                    return NotFound(new { success = false, message = "Purchase Order not found" });
                }

                // Store old values for audit trail
                var oldValues = new {
                    PONumber = purchaseOrder.PONumber,
                    SupplierId = purchaseOrder.SupplierId,
                    OrderDate = purchaseOrder.OrderDate,
                    ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
                    TotalAmount = purchaseOrder.TotalAmount,
                    Status = purchaseOrder.Status,
                    Notes = purchaseOrder.Notes
                };

                // Check if can be edited
                if (purchaseOrder.Status != "Draft")
                {
                    return BadRequest(new { success = false, message = "Purchase Order cannot be edited in current status" });
                }

                // Validate supplier if changed
                if (request.SupplierId.HasValue && request.SupplierId.Value != purchaseOrder.SupplierId)
                {
                    var supplier = await _context.Suppliers
                        .Where(s => s.Id == request.SupplierId.Value && s.CompanyId == companyId.Value && !s.IsDeleted)
                        .FirstOrDefaultAsync();

                    if (supplier == null)
                    {
                        return BadRequest(new { success = false, message = "Supplier not found" });
                    }

                    purchaseOrder.SupplierId = request.SupplierId.Value;
                }

                // Update basic fields
                if (request.OrderDate.HasValue)
                    purchaseOrder.OrderDate = request.OrderDate.Value;
                
                if (request.ExpectedDeliveryDate.HasValue)
                    purchaseOrder.ExpectedDeliveryDate = request.ExpectedDeliveryDate.Value;
                
                if (request.Notes != null)
                    purchaseOrder.Notes = request.Notes;

                // Update details if provided
                if (request.Details != null && request.Details.Any())
                {
                    // Remove existing details
                    var existingDetails = await _context.PurchaseOrderDetails
                        .Where(pod => pod.PurchaseOrderId == id)
                        .ToListAsync();

                    _context.PurchaseOrderDetails.RemoveRange(existingDetails);

                    // Add new details
                    decimal totalAmount = 0;
                    foreach (var detailRequest in request.Details)
                    {
                        var item = await _context.Items
                            .Where(i => i.Id == detailRequest.ItemId && i.CompanyId == companyId.Value && !i.IsDeleted)
                            .FirstOrDefaultAsync();

                        if (item == null)
                        {
                            return BadRequest(new { success = false, message = $"Item with ID {detailRequest.ItemId} not found" });
                        }

                        var detail = new PurchaseOrderDetail
                        {
                            PurchaseOrderId = id,
                            ItemId = detailRequest.ItemId,
                            Quantity = detailRequest.Quantity,
                            UnitPrice = detailRequest.UnitPrice,
                            TotalPrice = detailRequest.Quantity * detailRequest.UnitPrice,
                            Notes = detailRequest.Notes,
                            CreatedBy = userId.Value.ToString(),
                            CreatedDate = DateTime.UtcNow
                        };

                        _context.PurchaseOrderDetails.Add(detail);
                        totalAmount += detail.TotalPrice;
                    }

                    purchaseOrder.TotalAmount = totalAmount;
                }

                purchaseOrder.ModifiedBy = userId.Value.ToString();
                purchaseOrder.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("UPDATE", "PurchaseOrder", purchaseOrder.Id, 
                        $"{purchaseOrder.PONumber} - {purchaseOrder.Supplier?.Name}", oldValues, new { 
                            PONumber = purchaseOrder.PONumber,
                            SupplierId = purchaseOrder.SupplierId,
                            OrderDate = purchaseOrder.OrderDate,
                            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
                            TotalAmount = purchaseOrder.TotalAmount,
                            Status = purchaseOrder.Status,
                            Notes = purchaseOrder.Notes
                        });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for purchase order update");
                }

                _logger.LogInformation("Purchase Order updated successfully. ID: {Id}", id);

                return Ok(new
                {
                    success = true,
                    message = "Purchase Order updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating purchase order with ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error updating purchase order" });
            }
        }

        /// <summary>
        /// PATCH: api/purchaseorder/{id}/cancel
        /// Cancel purchase order (only if status is Draft)
        /// </summary>
        [HttpPatch("api/purchaseorder/{id}/cancel")]
        [RequirePermission(Constants.PO_MANAGE)]
        public async Task<IActionResult> CancelPurchaseOrder(int id, [FromBody] CancelRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                var userId = _currentUserService.UserId;

                if (!companyId.HasValue || !userId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company or user context found" });
                }

                // Validate request
                if (string.IsNullOrWhiteSpace(request?.Reason))
                {
                    return BadRequest(new { success = false, message = "Reason is required" });
                }

                var purchaseOrder = await _context.PurchaseOrders
                    .Where(po => po.Id == id && po.CompanyId == companyId.Value && !po.IsDeleted)
                    .Include(po => po.AdvancedShippingNotices)
                    .FirstOrDefaultAsync();

                if (purchaseOrder == null)
                {
                    return NotFound(new { success = false, message = "Purchase Order not found" });
                }

                // Validate: Only can cancel if status is Draft
                if (purchaseOrder.Status != Constants.PO_STATUS_DRAFT)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Purchase Order cannot be cancelled. Current status: {purchaseOrder.Status}. Only Draft status can be cancelled." 
                    });
                }

                // Check if PO has active ASNs
                var activeASNs = purchaseOrder.AdvancedShippingNotices
                    .Where(asn => !asn.IsDeleted && asn.Status != Constants.ASN_STATUS_CANCELLED)
                    .ToList();

                if (activeASNs.Any())
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Purchase Order cannot be cancelled because it has {activeASNs.Count} active ASN(s). Please cancel the ASN(s) first." 
                    });
                }

                // Store old Notes for audit
                var oldNotes = purchaseOrder.Notes;

                // Update status to Cancelled and add cancellation reason to Notes
                purchaseOrder.Status = Constants.PO_STATUS_CANCELLED;
                purchaseOrder.Notes = FormatCancellationNotes(purchaseOrder.Notes, request.Reason);
                purchaseOrder.ModifiedBy = userId.Value.ToString();
                purchaseOrder.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("CANCEL", "PurchaseOrder", purchaseOrder.Id, 
                        $"Cancelled Purchase Order {purchaseOrder.PONumber}. Reason: {request.Reason}", 
                        new { Status = Constants.PO_STATUS_DRAFT, Notes = oldNotes }, 
                        new { Status = Constants.PO_STATUS_CANCELLED, Notes = purchaseOrder.Notes });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for purchase order cancellation");
                }

                _logger.LogInformation("Purchase Order cancelled successfully. ID: {Id}, PO Number: {PONumber}, Reason: {Reason}", 
                    id, purchaseOrder.PONumber, request.Reason);

                return Ok(new
                {
                    success = true,
                    message = "Purchase Order cancelled successfully",
                    data = new { id = purchaseOrder.Id, poNumber = purchaseOrder.PONumber, status = purchaseOrder.Status }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling purchase order with ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error cancelling purchase order" });
            }
        }

        #endregion

        #region Email Operations

        /// <summary>
        /// POST: api/purchaseorder/{id}/send
        /// Send purchase order via email to supplier
        /// </summary>
        [HttpPost("api/purchaseorder/{id}/send")]
        public async Task<IActionResult> SendPurchaseOrder(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                var userId = _currentUserService.UserId;

                if (!companyId.HasValue || !userId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company or user context found" });
                }

                var purchaseOrder = await _context.PurchaseOrders
                    .Where(po => po.Id == id && po.CompanyId == companyId.Value && !po.IsDeleted)
                    .Include(po => po.Supplier)
                    .Include(po => po.PurchaseOrderDetails)
                        .ThenInclude(pod => pod.Item)
                    .FirstOrDefaultAsync();

                if (purchaseOrder == null)
                {
                    return NotFound(new { success = false, message = "Purchase Order not found" });
                }

                // Validate can be sent
                if (purchaseOrder.Status != "Draft")
                {
                    return BadRequest(new { success = false, message = "Purchase Order cannot be sent in current status" });
                }

                // Validate supplier email
                if (string.IsNullOrEmpty(purchaseOrder.Supplier.Email))
                {
                    return BadRequest(new { success = false, message = "Supplier email address is missing" });
                }

                // Generate email content
                var emailContent = await GeneratePurchaseOrderEmailContentAsync(purchaseOrder);
                var subject = $"Purchase Order {purchaseOrder.PONumber} from {_configuration["WMSSettings:CompanyName"] ?? "PT. Vera Co."}";

                // Send email
                var emailSent = await _emailService.SendEmailAsync(
                    purchaseOrder.Supplier.Email,
                    subject,
                    emailContent
                );

                if (emailSent)
                {
                    // Update status to Sent
                    purchaseOrder.Status = "Sent";
                    purchaseOrder.ModifiedBy = userId.Value.ToString();
                    purchaseOrder.ModifiedDate = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Purchase Order sent successfully. ID: {Id}, Email: {Email}", 
                        id, purchaseOrder.Supplier.Email);

                    return Ok(new
                    {
                        success = true,
                        message = $"Purchase Order sent successfully to {purchaseOrder.Supplier.Email}"
                    });
                }
                else
                {
                    _logger.LogError("Failed to send email for Purchase Order ID: {Id}", id);
                    return StatusCode(500, new { success = false, message = "Failed to send email" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending purchase order with ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error sending purchase order" });
            }
        }

        #endregion

        #region Helper API Endpoints

        /// <summary>
        /// GET: api/purchaseorder/suppliers
        /// Get list of suppliers for dropdown
        /// </summary>
        [HttpGet("api/purchaseorder/suppliers")]
        public async Task<IActionResult> GetSuppliers()
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var suppliers = await _context.Suppliers
                    .Where(s => s.CompanyId == companyId.Value && !s.IsDeleted)
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        email = s.Email,
                        phone = s.Phone,
                        address = s.Address
                    })
                    .OrderBy(s => s.name)
                    .ToListAsync();

                return Ok(new { success = true, data = suppliers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting suppliers");
                return StatusCode(500, new { success = false, message = "Error loading suppliers" });
            }
        }

        /// <summary>
        /// GET: api/purchaseorder/items
        /// Get list of items for dropdown
        /// </summary>
        [HttpGet("api/purchaseorder/items")]
        public async Task<IActionResult> GetItems([FromQuery] int? supplierId = null)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var query = _context.Items
                    .Where(i => i.CompanyId == companyId.Value && !i.IsDeleted)
                    .AsQueryable();

                // Filter by supplier if provided
                if (supplierId.HasValue)
                {
                    query = query.Where(i => i.SupplierId == supplierId.Value);
                }

                var items = await query
                    .Select(i => new
                    {
                        id = i.Id,
                        name = i.Name,
                        code = i.ItemCode,
                        unit = i.Unit,
                        purchasePrice = i.PurchasePrice,
                        description = i.Description,
                        supplierId = i.SupplierId
                    })
                    .OrderBy(i => i.name)
                    .ToListAsync();

                return Ok(new { success = true, data = items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items");
                return StatusCode(500, new { success = false, message = "Error loading items" });
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

        /// <summary>
        /// Generate unique PO number for company
        /// </summary>
        private async Task<string> GeneratePONumberAsync(int companyId)
        {
            var today = DateTime.Today;
            var year = today.Year;
            var month = today.Month;

            // Get the latest PO number for this month
            var latestPO = await _context.PurchaseOrders
                .Where(po => po.CompanyId == companyId && 
                           po.PONumber.StartsWith($"PO{year}{month:D2}"))
                .OrderByDescending(po => po.PONumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (latestPO != null)
            {
                var numberPart = latestPO.PONumber.Substring($"PO{year}{month:D2}".Length);
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"PO{year}{month:D2}{nextNumber:D3}";
        }

        /// <summary>
        /// Generate email content for purchase order
        /// </summary>
        private Task<string> GeneratePurchaseOrderEmailContentAsync(PurchaseOrder purchaseOrder)
        {
            var companyName = _configuration["WMSSettings:CompanyName"] ?? "PT. Vera Co.";
            
            var emailContent = $@"
                <h2>Purchase Order</h2>
                <p>Dear {purchaseOrder.Supplier.Name},</p>
                
                <p>Please find below our purchase order details:</p>
                
                <table border='1' style='border-collapse: collapse; width: 100%;'>
                    <tr>
                        <td><strong>PO Number:</strong></td>
                        <td>{purchaseOrder.PONumber}</td>
                    </tr>
                    <tr>
                        <td><strong>Order Date:</strong></td>
                        <td>{purchaseOrder.OrderDate:dd MMMM yyyy}</td>
                    </tr>
                    <tr>
                        <td><strong>Expected Delivery:</strong></td>
                        <td>{purchaseOrder.ExpectedDeliveryDate:dd MMMM yyyy}</td>
                    </tr>
                    <tr>
                        <td><strong>Company:</strong></td>
                        <td>{companyName}</td>
                    </tr>
                </table>
                
                <h3>Items:</h3>
                <table border='1' style='border-collapse: collapse; width: 100%;'>
                    <tr>
                        <th>Item Code</th>
                        <th>Item Name</th>
                        <th>Quantity</th>
                        <th>Unit Price</th>
                        <th>Total Price</th>
                    </tr>";

            foreach (var detail in purchaseOrder.PurchaseOrderDetails)
            {
                emailContent += $@"
                    <tr>
                        <td>{detail.Item.ItemCode}</td>
                        <td>{detail.Item.Name}</td>
                        <td>{detail.Quantity}</td>
                        <td>{detail.UnitPrice:C}</td>
                        <td>{detail.TotalPrice:C}</td>
                    </tr>";
            }

            emailContent += $@"
                </table>
                
                <p><strong>Total Amount: {purchaseOrder.TotalAmount:C}</strong></p>
                
                {(!string.IsNullOrEmpty(purchaseOrder.Notes) ? $"<p><strong>Notes:</strong> {purchaseOrder.Notes}</p>" : "")}
                
                <p>Please confirm receipt of this purchase order.</p>
                
                <p>Best regards,<br>{companyName}</p>";

            return Task.FromResult(emailContent);
        }

        #endregion

        #region Search Operations

        /// <summary>
        /// POST: api/purchaseorder/search
        /// Advanced search for purchase orders
        /// </summary>
        [HttpPost("api/purchaseorder/search")]
        public async Task<IActionResult> AdvancedSearchPurchaseOrders([FromBody] PurchaseOrderSearchRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var query = _context.PurchaseOrders
                    .Where(po => po.CompanyId == companyId.Value && !po.IsDeleted)
                    .Include(po => po.Supplier)
                    .Include(po => po.PurchaseOrderDetails)
                        .ThenInclude(pod => pod.Item)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.SearchText))
                {
                    query = query.Where(po =>
                        po.PONumber.Contains(request.SearchText) ||
                        po.Supplier.Name.Contains(request.SearchText));
                }

                if (!string.IsNullOrEmpty(request.SupplierNameFilter))
                {
                    query = query.Where(po => po.Supplier.Name.Contains(request.SupplierNameFilter));
                }

                if (!string.IsNullOrEmpty(request.PONumberFilter))
                {
                    query = query.Where(po => po.PONumber.Contains(request.PONumberFilter));
                }

                if (!string.IsNullOrEmpty(request.POStatusFilter))
                {
                    query = query.Where(po => po.Status == request.POStatusFilter);
                }

                if (request.DateFrom.HasValue)
                {
                    query = query.Where(po => po.OrderDate >= request.DateFrom.Value);
                }

                if (request.DateTo.HasValue)
                {
                    query = query.Where(po => po.OrderDate <= request.DateTo.Value);
                }

                var results = await query
                    .OrderByDescending(po => po.CreatedDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(po => new
                    {
                        id = po.Id,
                        poNumber = po.PONumber,
                        supplierName = po.Supplier.Name,
                        supplierEmail = po.Supplier.Email,
                        orderDate = po.OrderDate,
                        expectedDeliveryDate = po.ExpectedDeliveryDate,
                        status = po.Status,
                        totalAmount = po.TotalAmount,
                        itemCount = po.PurchaseOrderDetails.Count,
                        notes = po.Notes,
                        createdDate = po.CreatedDate
                    })
                    .ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced search purchase orders");
                return StatusCode(500, new { success = false, message = "Error searching purchase orders" });
            }
        }

        /// <summary>
        /// GET: api/purchaseorder/quick-search
        /// Quick search for purchase orders (for autocomplete/dropdown)
        /// </summary>
        [HttpGet("api/purchaseorder/quick-search")]
        public async Task<IActionResult> QuickSearchPurchaseOrders([FromQuery] string q = "")
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var query = _context.PurchaseOrders
                    .Where(po => po.CompanyId == companyId.Value && !po.IsDeleted)
                    .Include(po => po.Supplier)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(q))
                {
                    query = query.Where(po =>
                        po.PONumber.Contains(q) ||
                        po.Supplier.Name.Contains(q));
                }

                var results = await query
                    .OrderByDescending(po => po.CreatedDate)
                    .Take(10)
                    .Select(po => new
                    {
                        id = po.Id,
                        poNumber = po.PONumber,
                        supplierName = po.Supplier.Name,
                        orderDate = po.OrderDate,
                        status = po.Status,
                        totalAmount = po.TotalAmount
                    })
                    .ToListAsync();

                return Ok(results);
                }
                catch (Exception ex)
                {
                _logger.LogError(ex, "Error in quick search purchase orders");
                return StatusCode(500, new { success = false, message = "Error searching purchase orders" });
            }
        }

        /// <summary>
        /// GET: api/purchaseorder/suppliers/quick-search
        /// Quick search suppliers for dropdown/autocomplete
        /// </summary>
        [HttpGet("api/purchaseorder/suppliers/quick-search")]
        public async Task<IActionResult> QuickSearchSuppliers([FromQuery] string q = "")
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var query = _context.Suppliers
                    .Where(s => s.CompanyId == companyId.Value && !s.IsDeleted && s.IsActive)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(q))
                {
                    query = query.Where(s =>
                        s.Name.Contains(q) ||
                        s.Code.Contains(q) ||
                        s.Email.Contains(q));
                }

                var results = await query
                    .OrderBy(s => s.Name)
                    .Take(10)
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        code = s.Code,
                        email = s.Email,
                        phone = s.Phone,
                        contactPerson = s.ContactPerson
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick search suppliers");
                return StatusCode(500, new { success = false, message = "Error searching suppliers" });
            }
        }

        /// <summary>
        /// POST: api/purchaseorder/suppliers/search
        /// Advanced search for suppliers
        /// </summary>
        [HttpPost("api/purchaseorder/suppliers/search")]
        public async Task<IActionResult> AdvancedSearchSuppliers([FromBody] SupplierSearchRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var query = _context.Suppliers
                    .Where(s => s.CompanyId == companyId.Value && !s.IsDeleted)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.SearchText))
                {
                    query = query.Where(s => s.Name.Contains(request.SearchText) ||
                                           s.Email.Contains(request.SearchText) ||
                                           s.Phone.Contains(request.SearchText) ||
                                           s.Code.Contains(request.SearchText));
                }

                if (!string.IsNullOrEmpty(request.StatusFilter))
                {
                    if (request.StatusFilter == "active")
                        query = query.Where(s => s.IsActive);
                    else if (request.StatusFilter == "inactive")
                        query = query.Where(s => !s.IsActive);
                }

                if (!string.IsNullOrEmpty(request.CityFilter))
                {
                    query = query.Where(s => s.City.Contains(request.CityFilter));
                }

                if (!string.IsNullOrEmpty(request.SupplierNameFilter))
                {
                    query = query.Where(s => s.Name.Contains(request.SupplierNameFilter));
                }

                if (!string.IsNullOrEmpty(request.PhoneFilter))
                {
                    query = query.Where(s => s.Phone.Contains(request.PhoneFilter));
                }

                if (!string.IsNullOrEmpty(request.SupplierCodeFilter))
                {
                    query = query.Where(s => s.Code.Contains(request.SupplierCodeFilter));
                }

                if (request.DateFrom.HasValue)
                {
                    query = query.Where(s => s.CreatedDate >= request.DateFrom.Value);
                }

                if (request.DateTo.HasValue)
                {
                    query = query.Where(s => s.CreatedDate <= request.DateTo.Value);
                }

                var results = await query
                    .OrderBy(s => s.Name)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        code = s.Code,
                        email = s.Email,
                        phone = s.Phone,
                        address = s.Address,
                        city = s.City,
                        contactPerson = s.ContactPerson,
                        isActive = s.IsActive
                    })
                    .ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced search suppliers");
                return StatusCode(500, new { success = false, message = "Error searching suppliers" });
            }
        }

        /// <summary>
        /// GET: api/purchaseorder/items/quick-search
        /// Quick search items for dropdown/autocomplete
        /// </summary>
        [HttpGet("api/purchaseorder/items/quick-search")]
        public async Task<IActionResult> QuickSearchItems([FromQuery] string q = "", [FromQuery] int? supplierId = null)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var query = _context.Items
                    .Where(i => i.CompanyId == companyId.Value && !i.IsDeleted && i.IsActive)
                    .Include(i => i.Supplier)
                    .AsQueryable();

                if (supplierId.HasValue)
                {
                    query = query.Where(i => i.SupplierId == supplierId.Value);
                }

                if (!string.IsNullOrEmpty(q))
                {
                    query = query.Where(i =>
                        i.Name.Contains(q) ||
                        i.ItemCode.Contains(q) ||
                        (i.Description != null && i.Description.Contains(q)));
                }

                var results = await query
                    .OrderBy(i => i.Name)
                    .Take(10)
                    .Select(i => new
                    {
                        id = i.Id,
                        name = i.Name,
                        itemCode = i.ItemCode,
                        unit = i.Unit,
                        purchasePrice = i.PurchasePrice,
                        supplierName = i.Supplier != null ? i.Supplier.Name : null,
                        description = i.Description
                    })
                    .ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick search items");
                return StatusCode(500, new { success = false, message = "Error searching items" });
            }
        }

        /// <summary>
        /// POST: api/purchaseorder/items/search
        /// Advanced search for items
        /// </summary>
        [HttpPost("api/purchaseorder/items/search")]
        public async Task<IActionResult> AdvancedSearchItems([FromBody] ItemSearchRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var query = _context.Items
                    .Where(i => i.CompanyId == companyId.Value && !i.IsDeleted)
                    .Include(i => i.Supplier)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.SearchText))
                {
                    query = query.Where(i => i.Name.Contains(request.SearchText) ||
                                           i.ItemCode.Contains(request.SearchText) ||
                                           (i.Description != null && i.Description.Contains(request.SearchText)));
                }

                if (!string.IsNullOrEmpty(request.StatusFilter))
                {
                    if (request.StatusFilter == "active")
                        query = query.Where(i => i.IsActive);
                    else if (request.StatusFilter == "inactive")
                        query = query.Where(i => !i.IsActive);
                }

                if (request.SupplierFilter.HasValue)
                {
                    query = query.Where(i => i.SupplierId == request.SupplierFilter.Value);
                }

                if (request.PriceFrom.HasValue)
                {
                    query = query.Where(i => i.StandardPrice >= request.PriceFrom.Value);
                }

                if (request.PriceTo.HasValue)
                {
                    query = query.Where(i => i.StandardPrice <= request.PriceTo.Value);
                }

                if (request.DateFrom.HasValue)
                {
                    query = query.Where(i => i.CreatedDate >= request.DateFrom.Value);
                }

                if (request.DateTo.HasValue)
                {
                    query = query.Where(i => i.CreatedDate <= request.DateTo.Value);
                }

                var results = await query
                    .OrderBy(i => i.Name)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(i => new
                    {
                        id = i.Id,
                        name = i.Name,
                        itemCode = i.ItemCode,
                        unit = i.Unit,
                        purchasePrice = i.PurchasePrice,
                        supplierName = i.Supplier != null ? i.Supplier.Name : null,
                        supplierId = i.SupplierId,
                        description = i.Description,
                        isActive = i.IsActive
                    })
                    .ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced search items");
                return StatusCode(500, new { success = false, message = "Error searching items" });
            }
        }

        #endregion

        #region Legacy MVC Actions (for backward compatibility)

        // GET: PurchaseOrder/Details/5
        [RequirePermission(Constants.PO_VIEW)]
        public IActionResult Details(int id)
        {
            try
            {
                return View(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading purchase order details page for ID: {POId}", id);
                return View("Error");
            }
        }


        /// <summary>
        /// POST: api/purchaseorder/suppliers/advanced-search
        /// Advanced supplier search endpoint for Purchase Order
        /// </summary>
        [HttpPost("api/purchaseorder/suppliers/advanced-search")]
        [RequirePermission(Constants.SUPPLIER_VIEW)]
        public async Task<IActionResult> SearchSuppliersAdvanced([FromBody] SupplierAdvancedSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Advanced supplier search started for Purchase Order. Request: {@Request}", request);
                
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    _logger.LogWarning("No company context found for user");
                    return Json(new SupplierAdvancedSearchResponse
                    {
                        Success = false,
                        Message = "No company context found"
                    });
                }

                _logger.LogInformation("Searching suppliers for company ID: {CompanyId}", companyId.Value);

                var query = _context.Suppliers
                    .Where(s => s.CompanyId == companyId.Value && !s.IsDeleted)
                    .AsQueryable();

                // Apply search filters
                if (!string.IsNullOrEmpty(request.Name))
                {
                    query = query.Where(s => EF.Functions.Like(s.Name, $"%{request.Name}%"));
                }

                if (!string.IsNullOrEmpty(request.Email))
                {
                    query = query.Where(s => EF.Functions.Like(s.Email, $"%{request.Email}%"));
                }

                if (!string.IsNullOrEmpty(request.Phone))
                {
                    query = query.Where(s => EF.Functions.Like(s.Phone, $"%{request.Phone}%"));
                }

                if (!string.IsNullOrEmpty(request.City))
                {
                    query = query.Where(s => EF.Functions.Like(s.City, $"%{request.City}%"));
                }

                if (!string.IsNullOrEmpty(request.ContactPerson))
                {
                    query = query.Where(s => EF.Functions.Like(s.ContactPerson, $"%{request.ContactPerson}%"));
                }

                if (request.CreatedDateFrom.HasValue)
                {
                    query = query.Where(s => s.CreatedDate >= request.CreatedDateFrom.Value);
                }

                if (request.CreatedDateTo.HasValue)
                {
                    // Add one day to include the entire end date
                    var endDate = request.CreatedDateTo.Value.AddDays(1);
                    query = query.Where(s => s.CreatedDate < endDate);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var suppliers = await query
                    .OrderBy(s => s.Name)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(s => new SupplierSearchResult
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Email = s.Email,
                        Phone = s.Phone,
                        City = s.City,
                        ContactPerson = s.ContactPerson,
                        CreatedDate = s.CreatedDate,
                        IsActive = s.IsActive
                    })
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                _logger.LogInformation("Found {Count} suppliers matching criteria", suppliers.Count);

                return Json(new SupplierAdvancedSearchResponse
                {
                    Success = true,
                    Data = suppliers,
                    TotalCount = totalCount,
                    CurrentPage = request.Page,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced supplier search for Purchase Order");
                return Json(new SupplierAdvancedSearchResponse
                {
                    Success = false,
                    Message = "Error performing search"
                });
            }
        }

        /// <summary>
        /// POST: api/purchaseorder/items/advanced-search
        /// Advanced item search endpoint for Purchase Order
        /// </summary>
        [HttpPost("api/purchaseorder/items/advanced-search")]
        [RequirePermission(Constants.ITEM_VIEW)]
        public async Task<IActionResult> SearchItemsAdvanced([FromBody] ItemAdvancedSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Advanced item search started for Purchase Order. Request: {@Request}", request);
                
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    _logger.LogWarning("No company context found for user");
                    return Json(new ItemAdvancedSearchResponse
                    {
                        Success = false,
                        Message = "No company context found"
                    });
                }

                _logger.LogInformation("Searching items for company ID: {CompanyId}, Supplier ID: {SupplierId}", companyId.Value, request.SupplierId);

                var query = _context.Items
                    .Where(i => i.CompanyId == companyId.Value && !i.IsDeleted && i.IsActive)
                    .Include(i => i.Supplier)
                    .AsQueryable();

                // Filter by supplier (wajib)
                query = query.Where(i => i.SupplierId == request.SupplierId);

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
                    .Select(i => new ItemSearchResult
                    {
                        Id = i.Id,
                        ItemCode = i.ItemCode,
                        Name = i.Name,
                        Unit = i.Unit,
                        StandardPrice = i.StandardPrice,
                        SupplierId = i.SupplierId ?? 0,
                        SupplierName = i.Supplier != null ? i.Supplier.Name : "",
                        CreatedDate = i.CreatedDate,
                        IsActive = i.IsActive
                    })
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                _logger.LogInformation("Found {Count} items matching criteria for supplier {SupplierId}", items.Count, request.SupplierId);

                return Json(new ItemAdvancedSearchResponse
                {
                    Success = true,
                    Data = items,
                    TotalCount = totalCount,
                    CurrentPage = request.Page,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced item search for Purchase Order");
                return Json(new ItemAdvancedSearchResponse
                {
                    Success = false,
                    Message = "Error performing search"
                });
            }
        }

        #endregion
    }
}
