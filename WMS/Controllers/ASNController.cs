using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;
using WMS.Data;
using WMS.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller untuk ASN management - Hybrid MVC + API
    /// </summary>
    [RequirePermission(Constants.ASN_VIEW)]
    public class ASNController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditTrailService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ASNController> _logger;

        public ASNController(
            ApplicationDbContext context,
            IAuditTrailService auditService,
            ICurrentUserService currentUserService,
            ILogger<ASNController> logger)
        {
            _context = context;
            _auditService = auditService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        #region MVC Actions

        /// <summary>
        /// GET: /ASN
        /// ASN management index page
        /// </summary>
        public IActionResult Index()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ASN index page");
                return View("Error");
            }
        }

        /// <summary>
        /// GET: /ASN/Details/{id}
        /// ASN details page
        /// </summary>
        [RequirePermission(Constants.ASN_VIEW)]
        public IActionResult Details(int id)
        {
            try
            {
                return View(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ASN details page for ID: {ASNId}", id);
                return View("Error");
            }
        }

        #endregion

        #region Dashboard & Statistics

        /// <summary>
        /// GET: api/asn/dashboard
        /// Get ASN dashboard statistics
        /// </summary>
        [HttpGet("api/asn/dashboard")]
        [RequirePermission(Constants.ASN_VIEW)]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var totalASNs = await _context.AdvancedShippingNotices
                    .Where(a => a.CompanyId == companyId && !a.IsDeleted)
                    .CountAsync();

                var pendingASNs = await _context.AdvancedShippingNotices
                    .Where(a => a.CompanyId == companyId && !a.IsDeleted && a.Status == "Pending")
                    .CountAsync();

                var onDeliveryASNs = await _context.AdvancedShippingNotices
                    .Where(a => a.CompanyId == companyId && !a.IsDeleted && a.Status == "On Delivery")
                    .CountAsync();

                var arrivedASNs = await _context.AdvancedShippingNotices
                    .Where(a => a.CompanyId == companyId && !a.IsDeleted && a.Status == "Arrived")
                    .CountAsync();

                var processedASNs = await _context.AdvancedShippingNotices
                    .Where(a => a.CompanyId == companyId && !a.IsDeleted && a.Status == "Processed")
                    .CountAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        totalASNs,
                        pendingASNs,
                        onDeliveryASNs,
                        arrivedASNs,
                        processedASNs
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ASN dashboard statistics");
                return Json(new { success = false, message = "Error loading dashboard statistics" });
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// GET: api/asn
        /// Get paginated ASN list
        /// </summary>
        [HttpGet("api/asn")]
        [RequirePermission(Constants.ASN_VIEW)]
        public async Task<IActionResult> GetASNs([FromQuery] int page = 1, [FromQuery] int pageSize = 10, 
            [FromQuery] string? status = null, [FromQuery] string? search = null)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var query = _context.AdvancedShippingNotices
                    .Where(a => a.CompanyId == companyId && !a.IsDeleted)
                    .Include(a => a.PurchaseOrder)
                    .Include(a => a.ASNDetails)
                        .ThenInclude(ad => ad.Item)
                    .AsQueryable();

                // Filter by status
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(a => a.Status == status);
                }

                // Search functionality
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(a => a.ASNNumber.Contains(search) ||
                                           (a.PurchaseOrder != null && a.PurchaseOrder.PONumber.Contains(search)) ||
                                           (a.PurchaseOrder != null && a.PurchaseOrder.Supplier != null && a.PurchaseOrder.Supplier.Name.Contains(search)));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var asns = await query
                    .OrderByDescending(a => a.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new
                    {
                        id = a.Id,
                        asnNumber = a.ASNNumber,
                        purchaseOrderId = a.PurchaseOrderId,
                        purchaseOrderNumber = a.PurchaseOrder != null ? a.PurchaseOrder.PONumber : null,
                        supplierName = a.PurchaseOrder != null && a.PurchaseOrder.Supplier != null ? a.PurchaseOrder.Supplier.Name : null,
                        status = a.Status,
                        shipmentDate = a.ShipmentDate,
                        expectedArrivalDate = a.ExpectedArrivalDate,
                        actualArrivalDate = a.ActualArrivalDate,
                        totalItems = a.ASNDetails.Count,
                        totalQuantity = a.ASNDetails.Sum(ad => ad.ShippedQuantity),
                        createdDate = a.CreatedDate,
                        createdBy = a.CreatedBy
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
                _logger.LogError(ex, "Error getting ASN list");
                return Json(new { success = false, message = "Error loading ASN list" });
            }
        }

        /// <summary>
        /// GET: api/asn/purchaseorders
        /// Get available purchase orders for ASN creation
        /// </summary>
        [HttpGet("api/asn/purchaseorders")]
        [RequirePermission(Constants.ASN_MANAGE)]
        public async Task<IActionResult> GetAvailablePurchaseOrders()
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                // Get Purchase Orders that are not yet "Received" and not "Cancelled" (status-based filtering)
                var purchaseOrders = await _context.PurchaseOrders
                    .Where(po => po.CompanyId == companyId && 
                                !po.IsDeleted && 
                                po.Status != "Received" &&
                                po.Status != Constants.PO_STATUS_CANCELLED)
                    .Include(po => po.Supplier)
                    .Include(po => po.PurchaseOrderDetails)
                        .ThenInclude(pod => pod.Item)
                    .Select(po => new
                    {
                        id = po.Id,
                        poNumber = po.PONumber,
                        supplierId = po.SupplierId,
                        supplierName = po.Supplier != null ? po.Supplier.Name : "",
                        orderDate = po.OrderDate,
                        expectedDeliveryDate = po.ExpectedDeliveryDate,
                        status = po.Status,
                        totalAmount = po.TotalAmount,
                        items = po.PurchaseOrderDetails.Select(pod => new
                        {
                            id = pod.Id,
                            itemId = pod.ItemId,
                            itemCode = pod.Item != null ? pod.Item.ItemCode : "",
                            itemName = pod.Item != null ? pod.Item.Name : "",
                            itemUnit = pod.Item != null ? pod.Item.Unit : "",
                            orderedQuantity = pod.Quantity,
                            unitPrice = pod.UnitPrice,
                            totalPrice = pod.TotalPrice
                        }).ToList()
                    })
                    .OrderByDescending(po => po.orderDate)
                    .ToListAsync();

                return Json(new { success = true, data = purchaseOrders });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available purchase orders for ASN");
                return Json(new { success = false, message = "Error loading purchase orders" });
            }
        }

        /// <summary>
        /// GET: api/asn/items/{purchaseOrderId}
        /// Get items for a specific purchase order
        /// </summary>
        [HttpGet("api/asn/items/{purchaseOrderId}")]
        [RequirePermission(Constants.ASN_MANAGE)]
        public async Task<IActionResult> GetItemsForPurchaseOrder(int purchaseOrderId)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var purchaseOrder = await _context.PurchaseOrders
                    .Where(po => po.Id == purchaseOrderId && 
                                po.CompanyId == companyId && 
                                !po.IsDeleted)
                    .Include(po => po.PurchaseOrderDetails)
                        .ThenInclude(pod => pod.Item)
                    .FirstOrDefaultAsync();

                if (purchaseOrder == null)
                {
                    return Json(new { success = false, message = "Purchase Order not found" });
                }

                var items = purchaseOrder.PurchaseOrderDetails.Select(pod => new
                {
                    id = pod.Id,
                    itemId = pod.ItemId,
                    itemCode = pod.Item != null ? pod.Item.ItemCode : "",
                    itemName = pod.Item != null ? pod.Item.Name : "",
                    itemUnit = pod.Item != null ? pod.Item.Unit : "",
                    purchasePrice = pod.Item != null ? pod.Item.PurchasePrice : 0,
                    orderedQuantity = pod.Quantity,
                    unitPrice = pod.UnitPrice,
                    totalPrice = pod.TotalPrice
                }).ToList();

                return Json(new { success = true, data = items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items for purchase order {PurchaseOrderId}", purchaseOrderId);
                return Json(new { success = false, message = "Error loading items" });
            }
        }

        /// <summary>
        /// GET: api/asn/{id}
        /// Get ASN details by ID
        /// </summary>
        [HttpGet("api/asn/{id}")]
        [RequirePermission(Constants.ASN_VIEW)]
        public async Task<IActionResult> GetASN(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;

                var asn = await _context.AdvancedShippingNotices
                    .Where(a => a.Id == id && a.CompanyId == companyId && !a.IsDeleted)
                    .Include(a => a.PurchaseOrder)
                        .ThenInclude(po => po.Supplier)
                    .Include(a => a.ASNDetails)
                        .ThenInclude(ad => ad.Item)
                    .FirstOrDefaultAsync();

                if (asn == null)
                {
                    return Json(new { success = false, message = "ASN not found" });
                }

                var result = new
                {
                    id = asn.Id,
                    asnNumber = asn.ASNNumber,
                    purchaseOrderId = asn.PurchaseOrderId,
                    purchaseOrderNumber = asn.PurchaseOrder?.PONumber ?? "N/A",
                    supplierName = asn.PurchaseOrder?.Supplier?.Name ?? "N/A",
                    supplierContact = asn.PurchaseOrder?.Supplier?.ContactPerson ?? "N/A",
                    status = asn.Status,
                    expectedArrivalDate = asn.ExpectedArrivalDate,
                    actualArrivalDate = asn.ActualArrivalDate,
                    shipmentDate = asn.ShipmentDate,
                    carrierName = asn.CarrierName,
                    trackingNumber = asn.TrackingNumber,
                    notes = asn.Notes,
                    holdingLocationId = asn.HoldingLocationId,
                    createdDate = asn.CreatedDate,
                    createdBy = asn.CreatedBy,
                    details = asn.ASNDetails.Select(ad => new
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
                        notes = ad.Notes
                    })
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ASN details for ID: {ASNId}", id);
                return Json(new { success = false, message = "Error loading ASN details" });
            }
        }

        /// <summary>
        /// POST: api/asn
        /// Create new ASN
        /// </summary>
        [HttpPost("api/asn")]
        [RequirePermission(Constants.ASN_MANAGE)]
        public async Task<IActionResult> CreateASN([FromBody] CreateASNRequest request)
        {
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
                    
                    _logger.LogWarning("ASN creation failed validation: {@Errors}", errors);
                    return Json(new { success = false, message = "Validation failed", errors = errors });
                }

                var companyId = _currentUserService.CompanyId.Value;
                var userId = _currentUserService.UserId;

                _logger.LogInformation("Creating ASN for Purchase Order {PurchaseOrderId}, Company {CompanyId}", request.PurchaseOrderId, companyId);
                _logger.LogInformation("Request data: ExpectedArrivalDate={ExpectedArrivalDate}, ShipmentDate={ShipmentDate}, ItemsCount={ItemsCount}", 
                    request.ExpectedArrivalDate, request.ShipmentDate, request.Items?.Count ?? 0);

                // Validate Purchase Order exists and load its details
                var purchaseOrder = await _context.PurchaseOrders
                    .Where(po => po.Id == request.PurchaseOrderId && po.CompanyId == companyId && !po.IsDeleted)
                    .Include(po => po.PurchaseOrderDetails)
                        .ThenInclude(pod => pod.Item)
                    .FirstOrDefaultAsync();

                if (purchaseOrder == null)
                {
                    return Json(new { success = false, message = "Purchase Order not found" });
                }

                // Validate Purchase Order status - cannot create ASN for Cancelled PO
                if (purchaseOrder.Status == Constants.PO_STATUS_CANCELLED)
                {
                    return Json(new { success = false, message = "Cannot create ASN for a cancelled Purchase Order" });
                }

                // Validate items
                if (request.Items == null || !request.Items.Any())
                {
                    return Json(new { success = false, message = "At least one item must be provided" });
                }

                // Validate holding location capacity
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

                // Calculate total quantity that will be stored in holding location
                var totalQuantity = request.Items.Sum(item => item.ShippedQuantity);
                var availableCapacity = holdingLocation.MaxCapacity - holdingLocation.CurrentCapacity;

                if (availableCapacity < totalQuantity)
                {
                    return Json(new { success = false, message = $"Insufficient capacity in holding location '{holdingLocation.Name}'. Available: {availableCapacity}, Required: {totalQuantity}" });
                }

                // Generate ASN and Tracking numbers
                var asnNumber = await GenerateASNNumber(companyId);
                var trackingNumber = await GenerateTrackingNumber(companyId);
                
                _logger.LogInformation("Generated ASN Number: {ASNNumber}, Tracking Number: {TrackingNumber}", asnNumber, trackingNumber);

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var asn = new AdvancedShippingNotice
                    {
                        ASNNumber = asnNumber,
                        PurchaseOrderId = request.PurchaseOrderId,
                        Status = "Pending", // Sesuai dengan default model
                        ExpectedArrivalDate = request.ExpectedArrivalDate,
                        ShipmentDate = request.ShipmentDate ?? DateTime.Now,
                        CarrierName = request.CarrierName,
                        TrackingNumber = trackingNumber,
                        Notes = request.Notes,
                        HoldingLocationId = request.HoldingLocationId,
                        CompanyId = companyId,
                        CreatedBy = userId?.ToString() ?? "0",
                        CreatedDate = DateTime.Now
                    };

                    _logger.LogInformation("Adding ASN to context");
                    _context.AdvancedShippingNotices.Add(asn);
                    
                    _logger.LogInformation("Saving ASN to database");
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("ASN saved successfully with ID: {ASNId}", asn.Id);

                    // Add ASN Details
                    _logger.LogInformation("Adding {ItemsCount} ASN details", request.Items.Count);
                    foreach (var itemRequest in request.Items)
                    {
                        _logger.LogInformation("Processing item: ItemId={ItemId}, ShippedQuantity={ShippedQuantity}, ActualPricePerItem={ActualPricePerItem}", 
                            itemRequest.ItemId, itemRequest.ShippedQuantity, itemRequest.ActualPricePerItem);

                        // Validate item exists in the purchase order
                        var poDetail = purchaseOrder.PurchaseOrderDetails
                            .FirstOrDefault(pod => pod.ItemId == itemRequest.ItemId);

                        if (poDetail == null)
                        {
                            _logger.LogWarning("Item with ID {ItemId} not found in Purchase Order {PurchaseOrderId}", itemRequest.ItemId, request.PurchaseOrderId);
                            transaction.Rollback();
                            return Json(new { success = false, message = $"Item with ID {itemRequest.ItemId} not found in Purchase Order" });
                        }

                        // Validate shipped quantity doesn't exceed ordered quantity
                        if (itemRequest.ShippedQuantity > poDetail.Quantity)
                        {
                            _logger.LogWarning("Shipped quantity {ShippedQuantity} exceeds ordered quantity {OrderedQuantity} for item {ItemName}", 
                                itemRequest.ShippedQuantity, poDetail.Quantity, poDetail.Item?.Name ?? "Unknown");
                            transaction.Rollback();
                            return Json(new { success = false, message = $"Shipped quantity ({itemRequest.ShippedQuantity}) cannot exceed ordered quantity ({poDetail.Quantity}) for item {poDetail.Item?.Name ?? "Unknown"}" });
                        }

                        var asnDetail = new ASNDetail
                        {
                            ASNId = asn.Id,
                            ItemId = itemRequest.ItemId,
                            ShippedQuantity = itemRequest.ShippedQuantity,
                            ActualPricePerItem = itemRequest.ActualPricePerItem,
                            Notes = itemRequest.Notes,
                            CompanyId = companyId,
                            CreatedBy = userId?.ToString() ?? "0",
                            CreatedDate = DateTime.Now,
                            AlreadyPutAwayQuantity = 0, // Explicitly set required field
                            RemainingQuantity = itemRequest.ShippedQuantity // Will be properly set by InitializeRemainingQuantity
                        };

                        // Initialize remaining quantity
                        asnDetail.InitializeRemainingQuantity();
                        _logger.LogInformation("Created ASN detail for ItemId={ItemId}, RemainingQuantity={RemainingQuantity}", itemRequest.ItemId, asnDetail.RemainingQuantity);

                        _context.ASNDetails.Add(asnDetail);
                    }

                    _logger.LogInformation("Saving ASN details to database");
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("ASN details saved successfully");

                    // Update Purchase Order status to "Received" when ASN is created
                    _logger.LogInformation("Updating Purchase Order {POId} status to Received", request.PurchaseOrderId);
                    purchaseOrder.Status = "Received";
                    purchaseOrder.ModifiedBy = userId?.ToString() ?? "0";
                    purchaseOrder.ModifiedDate = DateTime.Now;

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Purchase Order status updated to Received");

                    await _auditService.LogActionAsync("CREATE", "ASN", asn.Id, 
                        $"Created ASN {asn.ASNNumber}");

                    await _auditService.LogActionAsync("UPDATE", "PurchaseOrder", purchaseOrder.Id, 
                        $"Updated Purchase Order {purchaseOrder.PONumber} status to Received");

                    transaction.Commit();
                    _logger.LogInformation("Transaction committed successfully");

                    return Json(new { success = true, message = "ASN created successfully", data = new { id = asn.Id } });
                }
            catch (DbUpdateException dbEx)
        {
            try
            {
                    transaction.Rollback();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Error during transaction rollback");
                }

                _logger.LogError(dbEx, "Database error creating ASN: {Message}", dbEx.Message);
                
                string detailedMessage = "Database error occurred while saving ASN.";
                
                if (dbEx.InnerException != null)
                {
                    var innerMessage = dbEx.InnerException.Message.ToLower();
                    _logger.LogError(dbEx.InnerException, "Database inner exception: {InnerMessage}", dbEx.InnerException.Message);
                    
                    if (innerMessage.Contains("unique constraint"))
                    {
                        detailedMessage = "A record with this information already exists. Please check ASN number or tracking number.";
                    }
                    else if (innerMessage.Contains("foreign key constraint"))
                    {
                        detailedMessage = "Referenced record not found. Please ensure Purchase Order and items are valid.";
                    }
                    else if (innerMessage.Contains("not null constraint"))
                    {
                        detailedMessage = "Required field is missing. Please check all required fields are filled.";
                    }
                    else if (innerMessage.Contains("constraint"))
                    {
                        detailedMessage = $"Database constraint violation: {dbEx.InnerException.Message}";
                }
                else
                {
                        detailedMessage = $"Database error: {dbEx.InnerException.Message}";
                    }
                }

                return Json(new { success = false, message = detailedMessage });
            }
            catch (Exception ex)
        {
            try
            {
                    transaction.Rollback();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Error during transaction rollback");
                }

                _logger.LogError(ex, "Unexpected error creating ASN: {Message}", ex.Message);
                
                // Capture inner exception for more detailed error information
                string detailedMessage = "An unexpected error occurred while creating ASN.";
                
                if (ex.InnerException != null)
                {
                    detailedMessage += $" Details: {ex.InnerException.Message}";
                    _logger.LogError(ex.InnerException, "Inner exception details");
                }
                
                return Json(new { success = false, message = detailedMessage });
                }
            }
            catch (Exception ex)
            {
            _logger.LogError(ex, "Unexpected error in CreateASN method");
            return Json(new { success = false, message = "An unexpected error occurred while creating ASN." });
        }
        }

        /// <summary>
        /// PUT: api/asn/{id}
        /// Update ASN
        /// </summary>
        [HttpPut("api/asn/{id}")]
        [RequirePermission(Constants.ASN_MANAGE)]
        public async Task<IActionResult> UpdateASN(int id, [FromBody] UpdateASNRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid data provided" });
                }

                var companyId = _currentUserService.CompanyId.Value;
                var userId = _currentUserService.UserId;

                var asn = await _context.AdvancedShippingNotices
                    .Include(a => a.ASNDetails)
                    .Where(a => a.Id == id && a.CompanyId == companyId && !a.IsDeleted)
                    .FirstOrDefaultAsync();

                if (asn == null)
                {
                    return Json(new { success = false, message = "ASN not found" });
                }

                // Validate holding location if provided
                if (request.HoldingLocationId > 0)
                {
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
                }

                // Update ASN properties
                asn.ExpectedArrivalDate = request.ExpectedArrivalDate;
                asn.ShipmentDate = request.ShipmentDate ?? asn.ShipmentDate;
                asn.CarrierName = request.CarrierName;
                asn.TrackingNumber = request.TrackingNumber;
                asn.Notes = request.Notes;
                if (request.HoldingLocationId > 0)
                {
                    asn.HoldingLocationId = request.HoldingLocationId;
                }
                asn.ModifiedBy = userId?.ToString() ?? "0";
                asn.ModifiedDate = DateTime.Now;

                // Update ASN Details (Items) if provided
                if (request.Items != null && request.Items.Any())
                {
                    _logger.LogInformation("Updating {ItemCount} ASN items for ASN ID: {ASNId}", request.Items.Count, id);
                    
                    foreach (var itemRequest in request.Items)
                    {
                        // Find existing ASN detail
                        var existingDetail = asn.ASNDetails
                            .FirstOrDefault(ad => ad.ItemId == itemRequest.ItemId);

                        if (existingDetail != null)
                        {
                            _logger.LogInformation("Updating ASN detail for ItemId={ItemId}, ShippedQuantity={ShippedQuantity}", 
                                itemRequest.ItemId, itemRequest.ShippedQuantity);
                            
                            // Update existing detail
                            existingDetail.ShippedQuantity = itemRequest.ShippedQuantity;
                            existingDetail.ActualPricePerItem = itemRequest.ActualPricePerItem;
                            existingDetail.Notes = itemRequest.Notes;
                            existingDetail.ModifiedBy = userId?.ToString() ?? "0";
                            existingDetail.ModifiedDate = DateTime.Now;
                            
                            // Recalculate remaining quantity if needed
                            existingDetail.RemainingQuantity = itemRequest.ShippedQuantity - existingDetail.AlreadyPutAwayQuantity;
                            
                            _context.ASNDetails.Update(existingDetail);
                }
                else
                {
                            _logger.LogWarning("ASN detail not found for ItemId={ItemId} in ASN ID={ASNId}", itemRequest.ItemId, id);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("ASN {ASNNumber} updated successfully", asn.ASNNumber);

                await _auditService.LogActionAsync("UPDATE", "ASN", asn.Id, 
                    $"Updated ASN {asn.ASNNumber}");

                return Json(new { success = true, message = "ASN updated successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating ASN with ID: {ASNId}", id);
                return Json(new { success = false, message = "Error updating ASN" });
            }
        }

        /// <summary>
        /// PATCH: api/asn/{id}/cancel
        /// Cancel ASN (only if status is Pending)
        /// Rollback: ASN → Cancelled, Purchase Order → Sent (karena email sudah dikirim)
        /// </summary>
        [HttpPatch("api/asn/{id}/cancel")]
        [RequirePermission(Constants.ASN_MANAGE)]
        public async Task<IActionResult> CancelASN(int id, [FromBody] CancelRequest request)
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

                var asn = await _context.AdvancedShippingNotices
                    .Where(a => a.Id == id && a.CompanyId == companyId && !a.IsDeleted)
                    .Include(a => a.PurchaseOrder)
                    .Include(a => a.ASNDetails)
                    .Include(a => a.HoldingLocation)
                    .FirstOrDefaultAsync();

                if (asn == null)
                {
                    return Json(new { success = false, message = "ASN not found" });
                }

                // Validate: Only can cancel if status is Pending
                if (asn.Status != Constants.ASN_STATUS_PENDING)
                {
                    return Json(new { 
                        success = false, 
                        message = $"ASN cannot be cancelled. Current status: {asn.Status}. Only Pending status can be cancelled." 
                    });
                }

                // Store old values for audit
                var oldPOStatus = asn.PurchaseOrder.Status;
                var oldNotes = asn.Notes;

                // Update ASN status to Cancelled and add cancellation reason to Notes
                asn.Status = Constants.ASN_STATUS_CANCELLED;
                asn.Notes = FormatCancellationNotes(asn.Notes, request.Reason);
                asn.ModifiedBy = userId?.ToString() ?? "0";
                asn.ModifiedDate = DateTime.Now;

                // Rollback Purchase Order status to "Sent"
                var otherActiveASNs = await _context.AdvancedShippingNotices
                    .Where(a => a.PurchaseOrderId == asn.PurchaseOrderId && 
                               a.Id != asn.Id && 
                               !a.IsDeleted && 
                               a.Status != Constants.ASN_STATUS_CANCELLED)
                    .CountAsync();

                if (otherActiveASNs == 0 && asn.PurchaseOrder.Status == Constants.PO_STATUS_RECEIVED)
                {
                    asn.PurchaseOrder.Status = Constants.PO_STATUS_SENT;
                    asn.PurchaseOrder.ModifiedBy = userId?.ToString() ?? "0";
                    asn.PurchaseOrder.ModifiedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Log audit trail
                await _auditService.LogActionAsync("CANCEL", "ASN", asn.Id, 
                    $"Cancelled ASN {asn.ASNNumber}. Reason: {request.Reason}",
                    new { Status = Constants.ASN_STATUS_PENDING, Notes = oldNotes },
                    new { Status = Constants.ASN_STATUS_CANCELLED, Notes = asn.Notes });

                if (asn.PurchaseOrder.Status != oldPOStatus)
                {
                    await _auditService.LogActionAsync("UPDATE", "PurchaseOrder", asn.PurchaseOrder.Id, 
                        $"Rolled back Purchase Order {asn.PurchaseOrder.PONumber} status from {oldPOStatus} to {asn.PurchaseOrder.Status} due to ASN cancellation");
                }

                _logger.LogInformation("ASN cancelled successfully. ID: {ASNId}, ASN Number: {ASNNumber}, Reason: {Reason}, PO rolled back to: {POStatus}", 
                    id, asn.ASNNumber, request.Reason, asn.PurchaseOrder.Status);

                return Json(new { 
                    success = true, 
                    message = "ASN cancelled successfully and Purchase Order status has been rolled back to 'Sent'",
                    data = new { 
                        asnId = asn.Id, 
                        asnNumber = asn.ASNNumber, 
                        asnStatus = asn.Status,
                        poId = asn.PurchaseOrder.Id,
                        poNumber = asn.PurchaseOrder.PONumber,
                        poStatus = asn.PurchaseOrder.Status,
                        poPreviousStatus = oldPOStatus
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling ASN with ID: {ASNId}", id);
                return Json(new { success = false, message = "Error cancelling ASN" });
            }
        }

        /// <summary>
        /// PATCH: api/asn/{id}/status
        /// Update ASN status
        /// </summary>
        [HttpPatch("api/asn/{id}/status")]
        [RequirePermission(Constants.ASN_MANAGE)]
        public async Task<IActionResult> UpdateASNStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId.Value;
                var userId = _currentUserService.UserId;

                var asn = await _context.AdvancedShippingNotices
                    .Where(a => a.Id == id && a.CompanyId == companyId && !a.IsDeleted)
                    .FirstOrDefaultAsync();

                if (asn == null)
                {
                    return Json(new { success = false, message = "ASN not found" });
                }

                var oldStatus = asn.Status;
                asn.Status = request.Status;
                asn.ModifiedBy = userId?.ToString() ?? "0";
                asn.ModifiedDate = DateTime.Now;

                // Set actual arrival date if status is "Arrived"
                if (request.Status == "Arrived" && !asn.ActualArrivalDate.HasValue)
                {
                    asn.ActualArrivalDate = DateTime.Now;
                    
                    // Auto-create inventory di holding location
                    await CreateInventoryAtHoldingLocation(asn);
                }

                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync("UPDATE_STATUS", "ASN", asn.Id, 
                    $"Changed ASN {asn.ASNNumber} status from {oldStatus} to {request.Status}");

                // Prepare response message
                string responseMessage = "ASN status updated successfully";
                
                // Add notification for inventory creation
                if (request.Status == "Arrived" && oldStatus != "Arrived")
                {
                    var holdingLocation = await _context.Locations
                        .FirstOrDefaultAsync(l => l.Id == asn.HoldingLocationId);
                    
                    var itemCount = await _context.ASNDetails
                        .Where(ad => ad.ASNId == asn.Id && !ad.IsDeleted)
                        .CountAsync();
                    
                    responseMessage += $". Inventory has been automatically created at holding location '{holdingLocation?.Name}' for {itemCount} items.";
                }

                return Json(new { success = true, message = responseMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ASN status for ID: {ASNId}", id);
                return Json(new { success = false, message = "Error updating ASN status" });
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

        private async Task<string> GenerateASNNumber(int companyId)
        {
            try
            {
                var today = DateTime.Now;
                var prefix = $"ASN{today:yyyyMMdd}";
                
                var lastASN = await _context.AdvancedShippingNotices
                    .Where(a => a.CompanyId == companyId && 
                               a.ASNNumber.StartsWith(prefix) && 
                               !a.IsDeleted)  // Filter soft delete
                    .OrderByDescending(a => a.ASNNumber)
                    .FirstOrDefaultAsync();

                if (lastASN == null)
                {
                    return $"{prefix}001";
                }

                var lastNumber = lastASN.ASNNumber.Substring(prefix.Length);
                if (int.TryParse(lastNumber, out int number))
                {
                    return $"{prefix}{(number + 1):D3}";
                }

                return $"{prefix}001";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating ASN number for company {CompanyId}", companyId);
                // Fallback with timestamp-based number
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                return $"ASN{timestamp}";
            }
        }

        private async Task<string> GenerateTrackingNumber(int companyId)
        {
            try
            {
                var today = DateTime.Now;
                var prefix = $"TRK{today:yyyyMMdd}";
                
                var lastTracking = await _context.AdvancedShippingNotices
                    .Where(a => a.CompanyId == companyId && 
                               a.TrackingNumber != null && 
                               a.TrackingNumber.StartsWith(prefix) && 
                               !a.IsDeleted)  // Filter soft delete
                    .OrderByDescending(a => a.TrackingNumber)
                    .FirstOrDefaultAsync();

                if (lastTracking == null)
                {
                    return $"{prefix}001";
                }

                var lastNumber = lastTracking.TrackingNumber.Substring(prefix.Length);
                if (int.TryParse(lastNumber, out int number))
                {
                    return $"{prefix}{(number + 1):D3}";
                }

                return $"{prefix}001";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tracking number for company {CompanyId}", companyId);
                // Fallback with timestamp-based number
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                return $"TRK{timestamp}";
            }
        }

        #endregion

        #region Request Models

        public class CreateASNRequest
        {
            [Required]
            public int PurchaseOrderId { get; set; }
            
            [Required]
            public DateTime ExpectedArrivalDate { get; set; }
            
            public DateTime? ShipmentDate { get; set; }
            
            [StringLength(100)]
            public string? CarrierName { get; set; }
            
            [StringLength(100)]
            public string? TrackingNumber { get; set; }
            
            [StringLength(500)]
            public string? Notes { get; set; }

            [Required(ErrorMessage = "Holding location is required")]
            public int HoldingLocationId { get; set; }

            public List<ASNItemRequest> Items { get; set; } = new List<ASNItemRequest>();
        }

        public class ASNItemRequest
        {
            [Required]
            public int ItemId { get; set; }

            [Required]
            [Range(1, int.MaxValue, ErrorMessage = "Shipped quantity must be greater than 0")]
            public int ShippedQuantity { get; set; }

            [Required]
            [Range(0.01, double.MaxValue, ErrorMessage = "Actual price must be greater than 0")]
            public decimal ActualPricePerItem { get; set; }

            [StringLength(200)]
            public string? Notes { get; set; }
        }

        public class UpdateASNRequest
        {
            [Required]
            public DateTime ExpectedArrivalDate { get; set; }
            
            public DateTime? ShipmentDate { get; set; }
            
            [StringLength(100)]
            public string? CarrierName { get; set; }
            
            [StringLength(100)]
            public string? TrackingNumber { get; set; }
            
            [StringLength(500)]
            public string? Notes { get; set; }

            [Required(ErrorMessage = "Holding location is required")]
            public int HoldingLocationId { get; set; }
            
            // Handle items update for shipped quantity
            public List<ASNItemRequest>? Items { get; set; }
        }

        public class UpdateStatusRequest
        {
            [Required]
            [StringLength(50)]
            public string Status { get; set; } = string.Empty;
        }

        /// <summary>
        /// GET: api/asn/locations
        /// Get holding locations (Other category only)
        /// </summary>
        [HttpGet("api/asn/locations")]
        [RequirePermission(Constants.ASN_VIEW)]
        public async Task<IActionResult> GetLocations()
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Json(new { success = false, message = "No company context found" });
                }

                // Only show Other category locations for holding (not Storage locations)
                var locations = await _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && 
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
                _logger.LogError(ex, "Error getting locations for ASN");
                return Json(new { success = false, message = "Error loading locations" });
            }
        }

        /// <summary>
        /// Create inventory at holding location when ASN status becomes "Arrived"
        /// </summary>
        private async Task CreateInventoryAtHoldingLocation(AdvancedShippingNotice asn)
                {
                    try
                    {
                // Load ASN details with items
                var asnDetails = await _context.ASNDetails
                    .Where(ad => ad.ASNId == asn.Id && !ad.IsDeleted)
                    .Include(ad => ad.Item)
                    .ToListAsync();

                if (!asnDetails.Any())
                {
                    _logger.LogWarning("No ASN details found for ASN {ASNId}", asn.Id);
                    return;
                }

                // Validate holding location capacity
                var holdingLocation = await _context.Locations
                    .FirstOrDefaultAsync(l => l.Id == asn.HoldingLocationId);

                if (holdingLocation == null)
                {
                    _logger.LogError("Holding location {LocationId} not found for ASN {ASNId}", asn.HoldingLocationId, asn.Id);
                    return;
                }

                // Calculate total quantity
                var totalQuantity = asnDetails.Sum(ad => ad.ShippedQuantity);
                
                // Check capacity
                if (holdingLocation.CurrentCapacity + totalQuantity > holdingLocation.MaxCapacity)
                {
                    _logger.LogError("Insufficient capacity in holding location {LocationId}. Current: {Current}, Max: {Max}, Required: {Required}", 
                        asn.HoldingLocationId, holdingLocation.CurrentCapacity, holdingLocation.MaxCapacity, totalQuantity);
                    throw new InvalidOperationException($"Insufficient capacity in holding location. Available: {holdingLocation.MaxCapacity - holdingLocation.CurrentCapacity}, Required: {totalQuantity}");
                }

                // Create inventory records for each item
                foreach (var detail in asnDetails)
                {
                    // Check if inventory already exists for this item in holding location
                    var existingInventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.ItemId == detail.ItemId && 
                                            i.LocationId == asn.HoldingLocationId && 
                                            i.CompanyId == asn.CompanyId && 
                                            !i.IsDeleted);

                        if (existingInventory != null)
                        {
                        // Update existing inventory
                        existingInventory.Quantity += detail.ShippedQuantity;
                        
                        // Auto-update status berdasarkan quantity
                        if (existingInventory.Quantity > 0)
                        {
                            existingInventory.Status = Constants.INVENTORY_STATUS_AVAILABLE;
                        }
                        
                        existingInventory.ModifiedBy = _currentUserService.UserId?.ToString() ?? "0";
                            existingInventory.ModifiedDate = DateTime.Now;
                        _context.Inventories.Update(existingInventory);
                        }
                        else
                        {
                        // Create new inventory
                            var inventory = new Inventory
                            {
                            ItemId = detail.ItemId,
                            LocationId = asn.HoldingLocationId,
                            Quantity = detail.ShippedQuantity,
                            Status = Constants.INVENTORY_STATUS_AVAILABLE, // Auto-set status untuk quantity > 0
                            CompanyId = asn.CompanyId,
                            CreatedBy = _currentUserService.UserId?.ToString() ?? "0",
                                CreatedDate = DateTime.Now,
                                LastUpdated = DateTime.Now
                            };
                        _context.Inventories.Add(inventory);
                    }
                        }

                // Update holding location capacity
                holdingLocation.CurrentCapacity += totalQuantity;
                _context.Locations.Update(holdingLocation);

                _logger.LogInformation("Created inventory for ASN {ASNId} at holding location {LocationId} with {TotalQuantity} items", 
                    asn.Id, asn.HoldingLocationId, totalQuantity);
                    }
                    catch (Exception ex)
                    {
                _logger.LogError(ex, "Error creating inventory at holding location for ASN {ASNId}", asn.Id);
                throw;
            }
        }

        /// <summary>
        /// POST: api/asn/purchaseorders/advanced-search
        /// Advanced search for Purchase Orders in ASN context
        /// </summary>
        [HttpPost("api/asn/purchaseorders/advanced-search")]
        [RequirePermission(Constants.ASN_VIEW)]
        public async Task<IActionResult> SearchPurchaseOrdersAdvanced([FromBody] PurchaseOrderAdvancedSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Advanced Purchase Order search started for ASN. Request: {@Request}", request);
                
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    _logger.LogWarning("No company context found for user");
                    return Json(new PurchaseOrderAdvancedSearchResponse
                    {
                        Success = false,
                        Message = "No company context found"
                    });
                }

                _logger.LogInformation("Searching Purchase Orders for company ID: {CompanyId}", companyId.Value);

                var query = _context.PurchaseOrders
                    .Where(po => po.CompanyId == companyId.Value && !po.IsDeleted)
                    .Include(po => po.Supplier)
                    .Include(po => po.PurchaseOrderDetails)
                    .AsQueryable();

                // Apply search filters
                if (!string.IsNullOrEmpty(request.PONumber))
                {
                    query = query.Where(po => EF.Functions.Like(po.PONumber, $"%{request.PONumber}%"));
                }

                if (!string.IsNullOrEmpty(request.SupplierName))
                {
                    query = query.Where(po => EF.Functions.Like(po.Supplier.Name, $"%{request.SupplierName}%"));
                }

                if (request.OrderDateFrom.HasValue)
                {
                    query = query.Where(po => po.OrderDate >= request.OrderDateFrom.Value);
                }

                if (request.OrderDateTo.HasValue)
                {
                    query = query.Where(po => po.OrderDate <= request.OrderDateTo.Value);
                }

                // Filter out Purchase Orders that already have ASN or are Cancelled
                query = query.Where(po => po.Status != "Received" && po.Status != Constants.PO_STATUS_CANCELLED);

                // Get total count
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                // Apply pagination
                var results = await query
                    .OrderByDescending(po => po.CreatedDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(po => new PurchaseOrderSearchResult
                    {
                        Id = po.Id,
                        PONumber = po.PONumber,
                        SupplierName = po.Supplier.Name,
                        SupplierEmail = po.Supplier.Email,
                        OrderDate = po.OrderDate,
                        ExpectedDeliveryDate = po.ExpectedDeliveryDate ?? DateTime.Today,
                        Status = po.Status,
                        TotalAmount = po.TotalAmount,
                        ItemCount = po.PurchaseOrderDetails.Count,
                        Notes = po.Notes,
                        CreatedDate = po.CreatedDate
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} Purchase Orders matching search criteria", results.Count);

                return Json(new PurchaseOrderAdvancedSearchResponse
                {
                    Success = true,
                    Message = "Search completed successfully",
                    Data = results,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = request.Page
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced Purchase Order search for ASN");
                return Json(new PurchaseOrderAdvancedSearchResponse
                {
                    Success = false,
                    Message = "Error performing search"
                });
            }
        }

        /// <summary>
        /// POST: api/asn/locations/advanced-search
        /// Advanced search for Locations in ASN context
        /// </summary>
        [HttpPost("api/asn/locations/advanced-search")]
        [RequirePermission(Constants.ASN_VIEW)]
        public async Task<IActionResult> SearchLocationsAdvanced([FromBody] LocationAdvancedSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Advanced Location search started for ASN. Request: {@Request}", request);
                
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    _logger.LogWarning("No company context found for user");
                    return Json(new LocationAdvancedSearchResponse
                    {
                        Success = false,
                        Message = "No company context found"
                    });
                }

                _logger.LogInformation("Searching Locations for company ID: {CompanyId}", companyId.Value);

                var query = _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && !l.IsDeleted && l.IsActive)
                    .AsQueryable();

                // Apply search filters
                if (!string.IsNullOrEmpty(request.Name))
                {
                    query = query.Where(l => EF.Functions.Like(l.Name, $"%{request.Name}%"));
                }

                if (!string.IsNullOrEmpty(request.Code))
                {
                    query = query.Where(l => EF.Functions.Like(l.Code, $"%{request.Code}%"));
                }

                if (request.CreatedDateFrom.HasValue)
                {
                    query = query.Where(l => l.CreatedDate >= request.CreatedDateFrom.Value);
                }

                if (request.CreatedDateTo.HasValue)
                {
                    query = query.Where(l => l.CreatedDate <= request.CreatedDateTo.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                // Apply pagination
                var results = await query
                    .OrderByDescending(l => l.CreatedDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(l => new LocationSearchResult
                    {
                        Id = l.Id,
                        Name = l.Name,
                        Code = l.Code,
                        Description = l.Description,
                        MaxCapacity = l.MaxCapacity,
                        CurrentCapacity = l.CurrentCapacity,
                        AvailableCapacity = l.MaxCapacity - l.CurrentCapacity,
                        IsActive = l.IsActive,
                        CreatedDate = l.CreatedDate
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} Locations matching search criteria", results.Count);

                return Json(new LocationAdvancedSearchResponse
                {
                    Success = true,
                    Message = "Search completed successfully",
                    Data = results,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = request.Page
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced Location search for ASN");
                return Json(new LocationAdvancedSearchResponse
                {
                    Success = false,
                    Message = "Error performing search"
                });
            }
        }

        /// <summary>
        /// GET: api/asn/items/search
        /// Search items by code or name for autocomplete
        /// </summary>
        [HttpGet("api/asn/items/search")]
        [RequirePermission(Constants.ASN_VIEW)]
        public async Task<IActionResult> SearchItems(string q, int? purchaseOrderId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(q) || q.Length < 2)
                {
                    return Json(new List<object>());
                }

                _logger.LogInformation("Searching items for ASN with query: {Query}, Purchase Order ID: {PurchaseOrderId}", q, purchaseOrderId);
                
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    _logger.LogWarning("No company context found for user");
                    return Json(new List<object>());
                }

                IQueryable<Item> query = _context.Items
                    .Where(i => i.CompanyId == companyId.Value && !i.IsDeleted && i.IsActive)
                    .Where(i => EF.Functions.Like(i.ItemCode, $"%{q}%") || 
                               EF.Functions.Like(i.Name, $"%{q}%"));

                // If Purchase Order ID provided, filter by items in that PO
                if (purchaseOrderId.HasValue)
                {
                    var poItemIds = await _context.PurchaseOrderDetails
                        .Where(pod => pod.PurchaseOrderId == purchaseOrderId.Value && !pod.IsDeleted)
                        .Select(pod => pod.ItemId)
                        .ToListAsync();
                        
                    query = query.Where(i => poItemIds.Contains(i.Id));
                    _logger.LogInformation("Filtering search by Purchase Order items: {ItemIds}", string.Join(",", poItemIds));
                }

                var items = await query
                    .Select(i => new
                    {
                        id = i.Id,
                        itemCode = i.ItemCode,
                        name = i.Name,
                        unit = i.Unit,
                        purchasePrice = i.PurchasePrice,
                        description = i.Description
                    })
                    .Take(10)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} items matching query: {Query} for Purchase Order {PurchaseOrderId}", items.Count, q, purchaseOrderId);

                return Json(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching items for ASN with query: {Query}, Purchase Order ID: {PurchaseOrderId}", q, purchaseOrderId);
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// GET: api/asn/items/top
        /// Get top 3 items for autocomplete when no search query
        /// </summary>
        [HttpGet("api/asn/items/top")]
        [RequirePermission(Constants.ASN_VIEW)]
        public async Task<IActionResult> GetTopItems(int? purchaseOrderId = null)
        {
            try
            {
                _logger.LogInformation("Getting top 3 items for ASN autocomplete, Purchase Order ID: {PurchaseOrderId}", purchaseOrderId);
                
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    _logger.LogWarning("No company context found for user");
                    return Json(new List<object>());
                }

                IQueryable<Item> query = _context.Items
                    .Where(i => i.CompanyId == companyId.Value && !i.IsDeleted && i.IsActive);

                // If Purchase Order ID provided, filter by items in that PO
                if (purchaseOrderId.HasValue)
                {
                    var poItemIds = await _context.PurchaseOrderDetails
                        .Where(pod => pod.PurchaseOrderId == purchaseOrderId.Value && !pod.IsDeleted)
                        .Select(pod => pod.ItemId)
                        .ToListAsync();
                        
                    query = query.Where(i => poItemIds.Contains(i.Id));
                    _logger.LogInformation("Filtering by Purchase Order items: {ItemIds}", string.Join(",", poItemIds));
                }

                var topItems = await query
                    .OrderBy(i => i.ItemCode) // Order by ItemCode for consistency
                    .Take(3)
                    .Select(i => new
                    {
                        id = i.Id,
                        itemCode = i.ItemCode,
                        name = i.Name,
                        unit = i.Unit,
                        purchasePrice = i.PurchasePrice,
                        description = i.Description
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} top items for Purchase Order {PurchaseOrderId}", topItems.Count, purchaseOrderId);

                return Json(topItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top items for ASN autocomplete, Purchase Order ID: {PurchaseOrderId}", purchaseOrderId);
                return Json(new List<object>());
            }
        }

        #endregion
    }
}