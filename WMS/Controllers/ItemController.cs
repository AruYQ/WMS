using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.Data;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Attributes;
using WMS.Utilities;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller untuk Item management - Hybrid MVC + API
    /// MVC actions use default routing (/Item)
    /// API actions use explicit routing (/api/item/*)
    /// </summary>
    [RequirePermission(Constants.ITEM_VIEW)]
    public class ItemController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditTrailService _auditService;
        private readonly ILogger<ItemController> _logger;

        public ItemController(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAuditTrailService auditService,
            ILogger<ItemController> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _auditService = auditService;
            _logger = logger;
        }

        #region MVC Actions

        /// <summary>
        /// GET: /Item
        /// Item management index page
        /// </summary>
        public IActionResult Index()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading item index page");
                return View("Error");
            }
        }

        #endregion

        #region Dashboard & Statistics

        /// <summary>
        /// GET: api/item/dashboard
        /// Get item statistics for dashboard
        /// </summary>
        [HttpGet("api/item/dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var items = await _context.Items
                    .Where(i => i.CompanyId == companyId.Value && !i.IsDeleted)
                    .Include(i => i.Supplier)
                    .ToListAsync();

                var statistics = new
                {
                    totalItems = items.Count,
                    activeItems = items.Count(i => i.IsActive),
                    inactiveItems = items.Count(i => !i.IsActive),
                    totalSuppliers = items.Select(i => i.SupplierId).Distinct().Count(),
                    totalValue = items.Sum(i => i.StandardPrice),
                    lowStockItems = items.Count(i => i.Inventories != null && i.Inventories.Sum(inv => inv.Quantity) <= 10)
                };

                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item dashboard statistics");
                return StatusCode(500, new { success = false, message = "Error loading dashboard statistics" });
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// GET: api/item
        /// Get items with pagination and search
        /// </summary>
        [HttpGet("api/item")]
        public async Task<IActionResult> GetItems([FromQuery] ItemListRequest request)
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

                // Apply search filter
                if (!string.IsNullOrEmpty(request.Search))
                {
                    query = query.Where(i => 
                        i.ItemCode.Contains(request.Search) ||
                        i.Name.Contains(request.Search) ||
                        i.Description.Contains(request.Search));
                }

                // Apply supplier filter
                if (request.SupplierId.HasValue)
                {
                    query = query.Where(i => i.SupplierId == request.SupplierId.Value);
                }

                // Apply active filter
                if (request.IsActive.HasValue)
                {
                    query = query.Where(i => i.IsActive == request.IsActive.Value);
                }

                // Apply sorting
                query = request.SortBy?.ToLower() switch
                {
                    "name" => request.SortDirection == "desc" ? query.OrderByDescending(i => i.Name) : query.OrderBy(i => i.Name),
                    "itemcode" => request.SortDirection == "desc" ? query.OrderByDescending(i => i.ItemCode) : query.OrderBy(i => i.ItemCode),
                    "price" => request.SortDirection == "desc" ? query.OrderByDescending(i => i.StandardPrice) : query.OrderBy(i => i.StandardPrice),
                    "supplier" => request.SortDirection == "desc" ? query.OrderByDescending(i => i.Supplier.Name) : query.OrderBy(i => i.Supplier.Name),
                    "createddate" => request.SortDirection == "desc" ? query.OrderByDescending(i => i.CreatedDate) : query.OrderBy(i => i.CreatedDate),
                    _ => query.OrderBy(i => i.ItemCode)
                };

                var totalCount = await query.CountAsync();

                var items = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(i => new ItemResponse
                    {
                        Id = i.Id,
                        ItemCode = i.ItemCode,
                        Name = i.Name,
                        Description = i.Description,
                        Unit = i.Unit,
                        StandardPrice = i.StandardPrice,
                        SupplierId = i.SupplierId ?? 0,
                        SupplierName = i.Supplier.Name,
                        IsActive = i.IsActive,
                        CreatedDate = i.CreatedDate,
                        ModifiedDate = i.ModifiedDate,
                        CreatedBy = i.CreatedBy,
                        ModifiedBy = i.ModifiedBy,
                        TotalStock = i.Inventories != null ? i.Inventories.Sum(inv => inv.Quantity) : 0,
                        TotalValue = i.StandardPrice * (i.Inventories != null ? i.Inventories.Sum(inv => inv.Quantity) : 0)
                    })
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                return Ok(new ItemListResponse
                {
                    Success = true,
                    Data = items,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = request.Page
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items");
                return StatusCode(500, new ItemListResponse
                {
                    Success = false,
                    Message = "Error loading items"
                });
            }
        }

        /// <summary>
        /// GET: api/item/{id}
        /// Get single item by ID
        /// </summary>
        [HttpGet("api/item/{id}")]
        public async Task<IActionResult> GetItem(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var item = await _context.Items
                    .Where(i => i.CompanyId == companyId.Value && i.Id == id && !i.IsDeleted)
                    .Include(i => i.Supplier)
                    .Select(i => new ItemResponse
                    {
                        Id = i.Id,
                        ItemCode = i.ItemCode,
                        Name = i.Name,
                        Description = i.Description,
                        Unit = i.Unit,
                        StandardPrice = i.StandardPrice,
                        SupplierId = i.SupplierId ?? 0,
                        SupplierName = i.Supplier.Name,
                        IsActive = i.IsActive,
                        CreatedDate = i.CreatedDate,
                        ModifiedDate = i.ModifiedDate,
                        CreatedBy = i.CreatedBy,
                        ModifiedBy = i.ModifiedBy,
                        TotalStock = i.Inventories != null ? i.Inventories.Sum(inv => inv.Quantity) : 0,
                        TotalValue = i.StandardPrice * (i.Inventories != null ? i.Inventories.Sum(inv => inv.Quantity) : 0)
                    })
                    .FirstOrDefaultAsync();

                if (item == null)
                {
                    return NotFound(new { success = false, message = "Item not found" });
                }

                return Ok(new { success = true, data = item });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item {ItemId}", id);
                return StatusCode(500, new { success = false, message = "Error loading item" });
            }
        }

        /// <summary>
        /// POST: api/item
        /// Create new item
        /// </summary>
        [HttpPost("api/item")]
        [RequirePermission(Constants.ITEM_MANAGE)]
        public async Task<IActionResult> CreateItem([FromBody] ItemCreateRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                // Check if item code is unique
                var existingItem = await _context.Items
                    .Where(i => i.CompanyId == companyId.Value && i.ItemCode == request.ItemCode && !i.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingItem != null)
                {
                    return BadRequest(new { success = false, message = "Item code already exists" });
                }

                // Verify supplier exists
                var supplier = await _context.Suppliers
                    .Where(s => s.CompanyId == companyId.Value && s.Id == request.SupplierId && !s.IsDeleted)
                    .FirstOrDefaultAsync();

                if (supplier == null)
                {
                    return BadRequest(new { success = false, message = "Supplier not found" });
                }

                var item = new Item
                {
                    CompanyId = companyId.Value,
                    ItemCode = request.ItemCode,
                    Name = request.Name,
                    Description = request.Description,
                    Unit = request.Unit,
                    StandardPrice = request.StandardPrice,
                    SupplierId = request.SupplierId,
                    IsActive = request.IsActive,
                    CreatedBy = _currentUserService.Username
                };

                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("CREATE", "Item", item.Id, 
                        $"{item.ItemCode} - {item.Name}", null, new { 
                            ItemCode = item.ItemCode, 
                            Name = item.Name, 
                            Unit = item.Unit,
                            StandardPrice = item.StandardPrice,
                            SupplierId = item.SupplierId,
                            IsActive = item.IsActive 
                        });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for item creation");
                }

                return Ok(new { success = true, message = "Item created successfully", data = new { id = item.Id } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item: {ItemCode} - Exception: {ExceptionMessage}", 
                    request.ItemCode, ex.Message);
                
                // Check if it's a unique constraint violation
                if (ex.InnerException?.Message.Contains("duplicate key") == true || 
                    ex.InnerException?.Message.Contains("unique constraint") == true)
                {
                    return BadRequest(new { success = false, message = "Item code already exists" });
                }
                
                return StatusCode(500, new { 
                    success = false, 
                    message = "Error creating item", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// PUT: api/item/{id}
        /// Update existing item
        /// </summary>
        [HttpPut("api/item/{id}")]
        [RequirePermission(Constants.ITEM_MANAGE)]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] ItemUpdateRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var item = await _context.Items
                    .Where(i => i.CompanyId == companyId.Value && i.Id == id && !i.IsDeleted)
                    .FirstOrDefaultAsync();

                if (item == null)
                {
                    return NotFound(new { success = false, message = "Item not found" });
                }

                // Check if item code is unique (excluding current item)
                var existingItem = await _context.Items
                    .Where(i => i.CompanyId == companyId.Value && i.ItemCode == request.ItemCode && i.Id != id && !i.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingItem != null)
                {
                    return BadRequest(new { success = false, message = "Item code already exists" });
                }

                // Verify supplier exists
                var supplier = await _context.Suppliers
                    .Where(s => s.CompanyId == companyId.Value && s.Id == request.SupplierId && !s.IsDeleted)
                    .FirstOrDefaultAsync();

                if (supplier == null)
                {
                    return BadRequest(new { success = false, message = "Supplier not found" });
                }

                // Get old values for audit trail
                var oldValues = new {
                    ItemCode = item.ItemCode,
                    Name = item.Name,
                    Description = item.Description,
                    Unit = item.Unit,
                    StandardPrice = item.StandardPrice,
                    SupplierId = item.SupplierId,
                    IsActive = item.IsActive
                };

                // Update item
                item.ItemCode = request.ItemCode;
                item.Name = request.Name;
                item.Description = request.Description;
                item.Unit = request.Unit;
                item.StandardPrice = request.StandardPrice;
                item.SupplierId = request.SupplierId;
                item.IsActive = request.IsActive;
                item.ModifiedDate = DateTime.Now;
                item.ModifiedBy = _currentUserService.Username;

                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("UPDATE", "Item", item.Id, 
                        $"{item.ItemCode} - {item.Name}", oldValues, new { 
                            ItemCode = item.ItemCode, 
                            Name = item.Name, 
                            Description = item.Description,
                            Unit = item.Unit,
                            StandardPrice = item.StandardPrice,
                            SupplierId = item.SupplierId,
                            IsActive = item.IsActive 
                        });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for item update");
                }

                return Ok(new { success = true, message = "Item updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item {ItemId}", id);
                return StatusCode(500, new { success = false, message = "Error updating item" });
            }
        }

        /// <summary>
        /// DELETE: api/item/{id}
        /// Delete item (soft delete)
        /// </summary>
        [HttpDelete("api/item/{id}")]
        [RequirePermission(Constants.ITEM_MANAGE)]
        public async Task<IActionResult> DeleteItem(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var item = await _context.Items
                    .Where(i => i.CompanyId == companyId.Value && i.Id == id && !i.IsDeleted)
                    .FirstOrDefaultAsync();

                if (item == null)
                {
                    return NotFound(new { success = false, message = "Item not found" });
                }

                // Check if item can be deleted (no inventory or transactions)
                var hasInventory = await _context.Inventories
                    .AnyAsync(inv => inv.ItemId == id && inv.Quantity > 0);

                if (hasInventory)
                {
                    return BadRequest(new { success = false, message = "Cannot delete item with existing inventory" });
                }

                // Get item data before deletion for audit trail
                var itemData = new {
                    ItemCode = item.ItemCode,
                    Name = item.Name,
                    Description = item.Description,
                    Unit = item.Unit,
                    StandardPrice = item.StandardPrice,
                    SupplierId = item.SupplierId,
                    IsActive = item.IsActive
                };

                // Soft delete
                item.IsDeleted = true;
                item.DeletedDate = DateTime.Now;
                item.DeletedBy = _currentUserService.Username;

                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("DELETE", "Item", id, 
                        $"{item.ItemCode} - {item.Name}", itemData, null);
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for item deletion");
                }

                return Ok(new { success = true, message = "Item deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item {ItemId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting item" });
            }
        }

        #endregion

        #region Utility Operations

        /// <summary>
        /// PATCH: api/item/{id}/toggle-status
        /// Toggle item status (active/inactive)
        /// </summary>
        [HttpPatch("api/item/{id}/toggle-status")]
        [RequirePermission(Constants.ITEM_MANAGE)]
        public async Task<IActionResult> ToggleItemStatus(int id, [FromBody] bool isActive)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var item = await _context.Items
                    .Where(i => i.CompanyId == companyId.Value && i.Id == id && !i.IsDeleted)
                    .FirstOrDefaultAsync();

                if (item == null)
                {
                    return NotFound(new { success = false, message = "Item not found" });
                }

                var oldStatus = item.IsActive;
                item.IsActive = isActive;
                item.ModifiedDate = DateTime.Now;
                item.ModifiedBy = _currentUserService.Username;

                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    var action = isActive ? "ACTIVATE" : "DEACTIVATE";
                    await _auditService.LogActionAsync(action, "Item", id, 
                        $"{item.ItemCode} - {item.Name}", 
                        new { IsActive = oldStatus }, 
                        new { IsActive = isActive });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for item status update");
                }

                return Ok(new { success = true, message = "Item status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item status for ID {ItemId}", id);
                return StatusCode(500, new { success = false, message = "Error updating item status" });
            }
        }

        #endregion

        #region Legacy API Endpoints (for backward compatibility)

        /// <summary>
        /// POST: Item/UpdateStatus
        /// Legacy endpoint for updating item status
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, bool isActive)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var item = await _context.Items
                    .Where(i => i.CompanyId == companyId.Value && i.Id == id && !i.IsDeleted)
                    .FirstOrDefaultAsync();

                if (item == null)
                {
                    return Json(new { success = false, message = "Item not found" });
                }

                var oldStatus = item.IsActive;
                item.IsActive = isActive;
                item.ModifiedDate = DateTime.Now;
                item.ModifiedBy = _currentUserService.Username;

                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    var action = isActive ? "ACTIVATE" : "DEACTIVATE";
                    await _auditService.LogActionAsync(action, "Item", id, 
                        $"{item.ItemCode} - {item.Name}", 
                        new { IsActive = oldStatus }, 
                        new { IsActive = isActive });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for item status update");
                }

                return Json(new { success = true, message = "Item status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item status for ID {ItemId}", id);
                return Json(new { success = false, message = "Error updating item status" });
            }
        }

        /// <summary>
        /// GET: Item/GetSuppliers
        /// Legacy endpoint for getting suppliers
        /// </summary>
        [HttpGet]
        [RequirePermission(Constants.SUPPLIER_VIEW)]
        public async Task<IActionResult> GetSuppliers([FromQuery] string? search = null, [FromQuery] int limit = 20)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Json(new { success = false, message = "No company context found" });
                }

                var query = _context.Suppliers
                    .Where(s => s.CompanyId == companyId.Value && !s.IsDeleted)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(s => 
                        s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        s.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                var suppliers = await query
                    .Take(limit)
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        email = s.Email,
                        phone = s.Phone,
                        address = s.Address
                    })
                    .ToListAsync();

                return Json(new { success = true, data = suppliers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting suppliers for dropdown");
                return Json(new { success = false, message = "Error loading suppliers" });
            }
        }

        /// <summary>
        /// POST: api/item/suppliers/advanced-search
        /// Advanced supplier search endpoint
        /// </summary>
        [HttpPost("api/item/suppliers/advanced-search")]
        [RequirePermission(Constants.SUPPLIER_VIEW)]
        public async Task<IActionResult> SearchSuppliersAdvanced([FromBody] SupplierAdvancedSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Advanced supplier search started. Request: {@Request}", request);
                
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

                // Check if suppliers exist for this company
                var supplierCount = await _context.Suppliers
                    .Where(s => s.CompanyId == companyId.Value && !s.IsDeleted)
                    .CountAsync();
                
                _logger.LogInformation("Found {SupplierCount} suppliers for company {CompanyId}", supplierCount, companyId.Value);

                var query = _context.Suppliers
                    .Where(s => s.CompanyId == companyId.Value && !s.IsDeleted)
                    .AsQueryable();

                // Apply search filters
                _logger.LogInformation("Applying search filters...");
                
                if (!string.IsNullOrEmpty(request.Name))
                {
                    _logger.LogInformation("Filtering by name: {Name}", request.Name);
                    query = query.Where(s => EF.Functions.Like(s.Name, $"%{request.Name}%"));
                }

                if (!string.IsNullOrEmpty(request.Email))
                {
                    _logger.LogInformation("Filtering by email: {Email}", request.Email);
                    query = query.Where(s => EF.Functions.Like(s.Email, $"%{request.Email}%"));
                }

                if (!string.IsNullOrEmpty(request.Phone))
                {
                    _logger.LogInformation("Filtering by phone: {Phone}", request.Phone);
                    query = query.Where(s => s.Phone != null && EF.Functions.Like(s.Phone, $"%{request.Phone}%"));
                }

                if (!string.IsNullOrEmpty(request.City))
                {
                    _logger.LogInformation("Filtering by city: {City}", request.City);
                    query = query.Where(s => s.City != null && EF.Functions.Like(s.City, $"%{request.City}%"));
                }

                if (!string.IsNullOrEmpty(request.ContactPerson))
                {
                    _logger.LogInformation("Filtering by contact person: {ContactPerson}", request.ContactPerson);
                    query = query.Where(s => s.ContactPerson != null && EF.Functions.Like(s.ContactPerson, $"%{request.ContactPerson}%"));
                }

                if (request.CreatedDateFrom.HasValue)
                {
                    _logger.LogInformation("Filtering by created date from: {CreatedDateFrom}", request.CreatedDateFrom.Value);
                    query = query.Where(s => s.CreatedDate >= request.CreatedDateFrom.Value);
                }

                if (request.CreatedDateTo.HasValue)
                {
                    _logger.LogInformation("Filtering by created date to: {CreatedDateTo}", request.CreatedDateTo.Value);
                    query = query.Where(s => s.CreatedDate <= request.CreatedDateTo.Value.AddDays(1).AddTicks(-1));
                }

                _logger.LogInformation("Executing count query...");
                var totalCount = await query.CountAsync();
                _logger.LogInformation("Total suppliers found: {TotalCount}", totalCount);

                _logger.LogInformation("Executing paginated query... Page: {Page}, PageSize: {PageSize}", request.Page, request.PageSize);
                var pagedSuppliers = await query
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
                _logger.LogInformation("Query completed successfully. Found {ResultCount} suppliers, TotalPages: {TotalPages}", pagedSuppliers.Count, totalPages);

                return Json(new SupplierAdvancedSearchResponse
                {
                    Success = true,
                    Data = pagedSuppliers,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = request.Page
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced supplier search: {Message}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                
                // Return more specific error message
                var errorMessage = ex switch
                {
                    ArgumentNullException => "Invalid search parameters provided",
                    InvalidOperationException => "Database operation failed",
                    TimeoutException => "Request timed out",
                    _ => $"Error performing advanced search: {ex.Message}"
                };
                
                return Json(new SupplierAdvancedSearchResponse
                {
                    Success = false,
                    Message = errorMessage
                });
            }
        }

        #endregion
    }
}
