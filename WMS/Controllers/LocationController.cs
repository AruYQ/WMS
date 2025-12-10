using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Attributes;
using WMS.Data;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller untuk Location management - Hybrid MVC + API
    /// MVC actions use default routing (/Location)
    /// API actions use explicit routing (/api/location/*)
    /// </summary>
    [RequirePermission(Constants.LOCATION_VIEW)]
    public class LocationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditTrailService _auditService;
        private readonly ILogger<LocationController> _logger;

        public LocationController(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAuditTrailService auditService,
            ILogger<LocationController> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _auditService = auditService;
            _logger = logger;
        }

        #region MVC Actions

        /// <summary>
        /// GET: /Location
        /// Location management index page
        /// </summary>
        public IActionResult Index()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading location index page");
                return View("Error");
            }
        }

        #endregion

        #region Dashboard & Statistics

        /// <summary>
        /// GET: api/location/dashboard
        /// Get location statistics for dashboard
        /// </summary>
        [HttpGet("api/location/dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var locations = await _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && !l.IsDeleted)
                    .ToListAsync();

                var statistics = new
                {
                    totalLocations = locations.Count,
                    activeLocations = locations.Count(l => l.IsActive),
                    inactiveLocations = locations.Count(l => !l.IsActive),
                    nearFullLocations = locations.Count(l => l.IsActive && l.CurrentCapacity >= l.MaxCapacity * 0.8 && l.CurrentCapacity < l.MaxCapacity),
                    fullLocations = locations.Count(l => l.IsFull),
                    emptyLocations = locations.Count(l => l.CurrentCapacity == 0)
                };

                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location dashboard statistics");
                return StatusCode(500, new { success = false, message = "Error loading dashboard statistics" });
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// GET: api/location
        /// Get paginated list of locations with filters
        /// </summary>
        [HttpGet("api/location")]
        public async Task<IActionResult> GetLocations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? capacity = null,
            [FromQuery] string? category = null)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var query = _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && !l.IsDeleted)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(l => 
                        l.Code.Contains(search) || 
                        l.Name.Contains(search) ||
                        (l.Description != null && l.Description.Contains(search)));
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    switch (status.ToLower())
                    {
                        case "active":
                            query = query.Where(l => l.IsActive);
                            break;
                        case "inactive":
                            query = query.Where(l => !l.IsActive);
                            break;
                        case "full":
                            query = query.Where(l => l.IsFull);
                            break;
                        case "near-full":
                            query = query.Where(l => l.IsActive && l.CurrentCapacity >= l.MaxCapacity * 0.8 && l.CurrentCapacity < l.MaxCapacity);
                            break;
                    }
                }

                // Apply category filter
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(l => l.Category == category);
                }

                // Apply capacity filter
                if (!string.IsNullOrEmpty(capacity))
                {
                    switch (capacity.ToLower())
                    {
                        case "empty":
                            query = query.Where(l => l.CurrentCapacity == 0);
                            break;
                        case "low":
                            query = query.Where(l => l.CurrentCapacity > 0 && l.CurrentCapacity <= l.MaxCapacity * 0.5);
                            break;
                        case "medium":
                            query = query.Where(l => l.CurrentCapacity > l.MaxCapacity * 0.5 && l.CurrentCapacity < l.MaxCapacity * 0.8);
                            break;
                        case "high":
                            query = query.Where(l => l.CurrentCapacity >= l.MaxCapacity * 0.8 && l.CurrentCapacity < l.MaxCapacity);
                            break;
                        case "full":
                            query = query.Where(l => l.CurrentCapacity >= l.MaxCapacity);
                            break;
                    }
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var locations = await query
                    .OrderBy(l => l.Code)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new
                    {
                        id = l.Id,
                        code = l.Code,
                        name = l.Name,
                        description = l.Description,
                        category = l.Category,
                        maxCapacity = l.MaxCapacity,
                        currentCapacity = l.CurrentCapacity,
                        availableCapacity = l.MaxCapacity - l.CurrentCapacity,
                        isFull = l.IsFull,
                        isActive = l.IsActive,
                        capacityPercentage = l.MaxCapacity > 0 ? (double)l.CurrentCapacity / l.MaxCapacity * 100 : 0,
                        capacityStatus = l.IsFull ? "FULL" :
                                        l.CurrentCapacity >= l.MaxCapacity * 0.8 ? "NEAR FULL" :
                                        l.CurrentCapacity > 0 ? "IN USE" : "AVAILABLE",
                        createdDate = l.CreatedDate,
                        modifiedDate = l.ModifiedDate,
                        createdBy = l.CreatedBy
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items = locations,
                        totalCount = totalCount,
                        totalPages = totalPages,
                        currentPage = page,
                        pageSize = pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting locations list");
                return StatusCode(500, new { success = false, message = "Error loading locations" });
            }
        }

        /// <summary>
        /// GET: api/location/{id}
        /// Get single location by ID
        /// </summary>
        [HttpGet("api/location/{id}")]
        public async Task<IActionResult> GetLocation(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var locationEntity = await _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && l.Id == id && !l.IsDeleted)
                    .FirstOrDefaultAsync();

                if (locationEntity == null)
                {
                    return NotFound(new { success = false, message = "Location not found" });
                }

                var inventoryItems = await _context.Inventories
                    .Where(inv =>
                        inv.CompanyId == companyId.Value &&
                        inv.LocationId == id &&
                        !inv.IsDeleted)
                    .Include(inv => inv.Item)
                    .OrderByDescending(inv => inv.Quantity)
                    .Take(20)
                    .Select(inv => new
                    {
                        itemId = inv.ItemId,
                        itemCode = inv.Item != null ? inv.Item.ItemCode : "N/A",
                        itemName = inv.Item != null ? inv.Item.Name : "Unknown Item",
                        unit = inv.Item != null ? inv.Item.Unit : "-",
                        quantity = inv.Quantity,
                        lastUpdated = inv.ModifiedDate ?? inv.CreatedDate
                    })
                    .ToListAsync();

                var capacityPercentage = locationEntity.MaxCapacity > 0
                    ? (double)locationEntity.CurrentCapacity / locationEntity.MaxCapacity * 100
                    : 0;

                var capacityStatus = locationEntity.IsFull
                    ? "FULL"
                    : locationEntity.CurrentCapacity >= locationEntity.MaxCapacity * 0.8
                        ? "NEAR FULL"
                        : locationEntity.CurrentCapacity > 0
                            ? "IN USE"
                            : "AVAILABLE";

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = locationEntity.Id,
                        code = locationEntity.Code,
                        name = locationEntity.Name,
                        description = locationEntity.Description,
                        category = locationEntity.Category,
                        isActive = locationEntity.IsActive,
                        capacity = new
                        {
                            max = locationEntity.MaxCapacity,
                            current = locationEntity.CurrentCapacity,
                            available = locationEntity.MaxCapacity - locationEntity.CurrentCapacity,
                            percentage = capacityPercentage,
                            status = capacityStatus,
                            isFull = locationEntity.IsFull
                        },
                        audit = new
                        {
                            createdDate = locationEntity.CreatedDate,
                            modifiedDate = locationEntity.ModifiedDate,
                            createdBy = locationEntity.CreatedBy,
                            modifiedBy = locationEntity.ModifiedBy
                        },
                        inventoryItems
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location {LocationId}", id);
                return StatusCode(500, new { success = false, message = "Error loading location" });
            }
        }

        /// <summary>
        /// POST: api/location
        /// Create new location
        /// </summary>
        [HttpPost("api/location")]
        [RequirePermission(Constants.LOCATION_MANAGE)]
        public async Task<IActionResult> CreateLocation([FromBody] LocationCreateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
                }

                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                // Validate location code uniqueness
                var existingLocation = await _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && l.Code == request.Code && !l.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingLocation != null)
                {
                    return BadRequest(new { success = false, message = "A location with this code already exists" });
                }

                // Validate business rules
                if (request.MaxCapacity <= 0)
                {
                    return BadRequest(new { success = false, message = "Maximum capacity must be greater than 0" });
                }

                // Validate category
                if (string.IsNullOrEmpty(request.Category) || 
                    (request.Category != Constants.LOCATION_CATEGORY_STORAGE && 
                     request.Category != Constants.LOCATION_CATEGORY_OTHER))
                {
                    return BadRequest(new { success = false, message = "Invalid category. Must be 'Storage' or 'Other'" });
                }

                // Create location entity
                var location = new Location
                {
                    Code = request.Code,
                    Name = request.Name,
                    Description = request.Description,
                    Category = request.Category,
                    MaxCapacity = request.MaxCapacity,
                    CurrentCapacity = 0,
                    IsFull = false,
                    IsActive = request.IsActive,
                    CompanyId = companyId.Value,
                    CreatedDate = DateTime.Now,
                    CreatedBy = _currentUserService.Username ?? "System"
                };

                _context.Locations.Add(location);
                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("CREATE", "Location", location.Id, 
                        $"{location.Code} - {location.Name}", null, new { 
                            Code = location.Code, 
                            Name = location.Name,
                            Category = location.Category,
                            MaxCapacity = location.MaxCapacity,
                            IsActive = location.IsActive 
                        });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for location creation");
                }

                return Ok(new
                {
                    success = true,
                    message = $"Location '{location.Code} - {location.Name}' created successfully",
                    data = new { id = location.Id }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating location: {Code} - Exception: {ExceptionMessage}", 
                    request.Code, ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                
                // Check if it's a unique constraint violation
                if (ex.InnerException?.Message.Contains("duplicate key") == true || 
                    ex.InnerException?.Message.Contains("unique constraint") == true)
                {
                    return BadRequest(new { success = false, message = "A location with this code already exists" });
                }
                
                return StatusCode(500, new { 
                    success = false, 
                    message = "Error creating location", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// PUT: api/location/{id}
        /// Update existing location
        /// </summary>
        [HttpPut("api/location/{id}")]
        [RequirePermission(Constants.LOCATION_MANAGE)]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] LocationUpdateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
                }

                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var location = await _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && l.Id == id && !l.IsDeleted)
                    .FirstOrDefaultAsync();

                if (location == null)
                {
                    return NotFound(new { success = false, message = "Location not found" });
                }

                // Validate location code uniqueness (excluding current location)
                var existingLocation = await _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && l.Code == request.Code && l.Id != id && !l.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingLocation != null)
                {
                    return BadRequest(new { success = false, message = "A location with this code already exists" });
                }

                // Validate business rules
                if (request.MaxCapacity <= 0)
                {
                    return BadRequest(new { success = false, message = "Maximum capacity must be greater than 0" });
                }

                if (request.MaxCapacity < location.CurrentCapacity)
                {
                    return BadRequest(new { success = false, message = "Maximum capacity cannot be less than current usage" });
                }

                // Validate category if provided
                if (!string.IsNullOrEmpty(request.Category) &&
                    request.Category != Constants.LOCATION_CATEGORY_STORAGE &&
                    request.Category != Constants.LOCATION_CATEGORY_OTHER)
                {
                    return BadRequest(new { success = false, message = "Invalid category. Must be 'Storage' or 'Other'" });
                }

                // Store old values for audit trail
                var oldValues = new {
                    Code = location.Code,
                    Name = location.Name,
                    Description = location.Description,
                    Category = location.Category,
                    MaxCapacity = location.MaxCapacity,
                    IsActive = location.IsActive
                };

                // Update location
                location.Code = request.Code;
                location.Name = request.Name;
                location.Description = request.Description;
                if (!string.IsNullOrEmpty(request.Category))
                {
                    location.Category = request.Category;
                }
                location.MaxCapacity = request.MaxCapacity;
                location.IsActive = request.IsActive;
                location.ModifiedDate = DateTime.Now;
                location.ModifiedBy = _currentUserService.Username ?? "System";

                // Recalculate capacity status
                location.IsFull = location.CurrentCapacity >= location.MaxCapacity;

                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("UPDATE", "Location", location.Id, 
                        $"{location.Code} - {location.Name}", oldValues, new { 
                            Code = location.Code, 
                            Name = location.Name,
                            Description = location.Description,
                            Category = location.Category,
                            MaxCapacity = location.MaxCapacity,
                            IsActive = location.IsActive 
                        });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for location update");
                }

                return Ok(new
                {
                    success = true,
                    message = $"Location '{location.Code} - {location.Name}' updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location {LocationId}", id);
                return StatusCode(500, new { success = false, message = "Error updating location" });
            }
        }

        /// <summary>
        /// DELETE: api/location/{id}
        /// Delete location
        /// </summary>
        [HttpDelete("api/location/{id}")]
        [RequirePermission(Constants.LOCATION_MANAGE)]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var location = await _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && l.Id == id && !l.IsDeleted)
                    .FirstOrDefaultAsync();

                if (location == null)
                {
                    return NotFound(new { success = false, message = "Location not found" });
                }

                // Check if location has inventory - cannot delete if has inventory
                var hasInventory = await _context.Inventories
                    .AnyAsync(i => i.LocationId == id && i.Quantity > 0);

                if (hasInventory)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Cannot delete location '{location.Code} - {location.Name}' because it still contains inventory. Please move all inventory to another location first."
                    });
                }

                // Soft delete - mark as deleted instead of removing from database
                location.IsDeleted = true;
                location.DeletedDate = DateTime.Now;
                location.DeletedBy = _currentUserService.Username ?? "System";
                location.ModifiedDate = DateTime.Now;
                location.ModifiedBy = _currentUserService.Username ?? "System";
                
                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("DELETE", "Location", location.Id, 
                        $"{location.Code} - {location.Name}", new { 
                            Code = location.Code, 
                            Name = location.Name, 
                            MaxCapacity = location.MaxCapacity,
                            IsActive = location.IsActive 
                        }, null);
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for location deletion");
                }

                return Ok(new
                {
                    success = true,
                    message = $"Location '{location.Code} - {location.Name}' deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting location {LocationId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting location" });
            }
        }

        #endregion

        #region Inventory Management

        /// <summary>
        /// GET: api/location/{id}/inventory
        /// Get inventory in a specific location
        /// </summary>
        [HttpGet("{id}/inventory")]
        public async Task<IActionResult> GetLocationInventory(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var location = await _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && l.Id == id && !l.IsDeleted)
                    .FirstOrDefaultAsync();

                if (location == null)
                {
                    return NotFound(new { success = false, message = "Location not found" });
                }

                var inventories = await _context.Inventories
                    .Where(i => i.LocationId == id && i.CompanyId == companyId.Value)
                    .Include(i => i.Item)
                    .Select(i => new
                    {
                        id = i.Id,
                        itemCode = i.Item.ItemCode,
                        itemName = i.Item.Name,
                        quantity = i.Quantity,
                        unit = i.Item.Unit,
                        lastUpdated = i.ModifiedDate ?? i.CreatedDate
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        location = new
                        {
                            id = location.Id,
                            code = location.Code,
                            name = location.Name
                        },
                        inventories = inventories,
                        totalItems = inventories.Count,
                        totalQuantity = inventories.Sum(i => i.quantity)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location inventory {LocationId}", id);
                return StatusCode(500, new { success = false, message = "Error loading location inventory" });
            }
        }

        /// <summary>
        /// POST: api/location/{id}/clear-inventory
        /// Move all inventory from this location to another location
        /// </summary>
        [HttpPost("api/location/{id}/clear-inventory")]
        [RequirePermission(Constants.LOCATION_MANAGE)]
        public async Task<IActionResult> ClearLocationInventory(int id, [FromBody] MoveInventoryRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var sourceLocation = await _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && l.Id == id && !l.IsDeleted)
                    .FirstOrDefaultAsync();

                if (sourceLocation == null)
                {
                    return NotFound(new { success = false, message = "Source location not found" });
                }

                var targetLocation = await _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && l.Id == request.TargetLocationId && !l.IsDeleted && l.IsActive)
                    .FirstOrDefaultAsync();

                if (targetLocation == null)
                {
                    return NotFound(new { success = false, message = "Target location not found or inactive" });
                }

                var inventories = await _context.Inventories
                    .Where(i => i.LocationId == id && i.CompanyId == companyId.Value && i.Quantity > 0)
                    .ToListAsync();

                if (!inventories.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "No inventory found in this location",
                        data = new { movedItems = 0 }
                    });
                }

                // Check if target location has enough capacity
                var totalQuantityToMove = inventories.Sum(i => i.Quantity);
                var targetAvailableCapacity = targetLocation.MaxCapacity - targetLocation.CurrentCapacity;

                if (totalQuantityToMove > targetAvailableCapacity)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Target location '{targetLocation.Code}' does not have enough capacity. Available: {targetAvailableCapacity}, Required: {totalQuantityToMove}"
                    });
                }

                // Move inventory
                var movedItems = 0;
                foreach (var inventory in inventories)
                {
                    inventory.LocationId = targetLocation.Id;
                    inventory.ModifiedDate = DateTime.Now;
                    inventory.ModifiedBy = _currentUserService.Username ?? "System";
                    movedItems++;
                }

                // Update location capacities
                sourceLocation.CurrentCapacity = 0;
                sourceLocation.IsFull = false;
                sourceLocation.ModifiedDate = DateTime.Now;
                sourceLocation.ModifiedBy = _currentUserService.Username ?? "System";

                targetLocation.CurrentCapacity += totalQuantityToMove;
                targetLocation.IsFull = targetLocation.CurrentCapacity >= targetLocation.MaxCapacity;
                targetLocation.ModifiedDate = DateTime.Now;
                targetLocation.ModifiedBy = _currentUserService.Username ?? "System";

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Successfully moved {movedItems} inventory items from '{sourceLocation.Code}' to '{targetLocation.Code}'",
                    data = new
                    {
                        movedItems = movedItems,
                        totalQuantity = totalQuantityToMove,
                        sourceLocation = new { id = sourceLocation.Id, code = sourceLocation.Code },
                        targetLocation = new { id = targetLocation.Id, code = targetLocation.Code }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing location inventory {LocationId}", id);
                return StatusCode(500, new { success = false, message = "Error moving inventory" });
            }
        }

        #endregion

        #region Special Operations

        /// <summary>
        /// PATCH: api/location/{id}/toggle-status
        /// Toggle location active status
        /// </summary>
        [HttpPatch("api/location/{id}/toggle-status")]
        [RequirePermission(Constants.LOCATION_MANAGE)]
        public async Task<IActionResult> ToggleLocationStatus(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var location = await _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && l.Id == id && !l.IsDeleted)
                    .FirstOrDefaultAsync();

                if (location == null)
                {
                    return NotFound(new { success = false, message = "Location not found" });
                }

                var oldStatus = location.IsActive;
                location.IsActive = !location.IsActive;
                location.ModifiedDate = DateTime.Now;
                location.ModifiedBy = _currentUserService.Username ?? "System";

                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    var action = location.IsActive ? "ACTIVATE" : "DEACTIVATE";
                    var statusText = location.IsActive ? "activated" : "deactivated";
                    await _auditService.LogActionAsync(action, "Location", location.Id, 
                        $"{location.Code} - {location.Name}", new { IsActive = oldStatus }, new { IsActive = location.IsActive });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for location status toggle");
                }

                var status = location.IsActive ? "activated" : "deactivated";
                return Ok(new
                {
                    success = true,
                    message = $"Location '{location.Code}' has been {status} successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling location status {LocationId}", id);
                return StatusCode(500, new { success = false, message = "Error updating location status" });
            }
        }

        /// <summary>
        /// PATCH: api/location/{id}/update-capacity
        /// Update location capacity based on current inventory
        /// </summary>
        [HttpPatch("api/location/{id}/update-capacity")]
        [RequirePermission(Constants.LOCATION_MANAGE)]
        public async Task<IActionResult> UpdateLocationCapacity(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var location = await _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && l.Id == id && !l.IsDeleted)
                    .FirstOrDefaultAsync();

                if (location == null)
                {
                    return NotFound(new { success = false, message = "Location not found" });
                }

                // Calculate current capacity from inventory
                var currentCapacity = await _context.Inventories
                    .Where(i => i.LocationId == id)
                    .SumAsync(i => i.Quantity);

                location.CurrentCapacity = currentCapacity;
                location.IsFull = currentCapacity >= location.MaxCapacity;
                location.ModifiedDate = DateTime.Now;
                location.ModifiedBy = _currentUserService.Username ?? "System";

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Capacity updated for location '{location.Code}'. Current: {currentCapacity}/{location.MaxCapacity}",
                    data = new
                    {
                        currentCapacity = currentCapacity,
                        maxCapacity = location.MaxCapacity,
                        isFull = location.IsFull,
                        capacityPercentage = location.MaxCapacity > 0 ? (double)currentCapacity / location.MaxCapacity * 100 : 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location capacity {LocationId}", id);
                return StatusCode(500, new { success = false, message = "Error updating capacity" });
            }
        }

        /// <summary>
        /// POST: api/location/refresh-all-capacities
        /// Refresh capacities for all locations
        /// </summary>
        [HttpPost("api/location/refresh-all-capacities")]
        [RequirePermission(Constants.LOCATION_MANAGE)]
        public async Task<IActionResult> RefreshAllCapacities()
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var locations = await _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && !l.IsDeleted)
                    .ToListAsync();

                var updatedCount = 0;
                foreach (var location in locations)
                {
                    var currentCapacity = await _context.Inventories
                        .Where(i => i.LocationId == location.Id)
                        .SumAsync(i => i.Quantity);

                    if (location.CurrentCapacity != currentCapacity)
                    {
                        location.CurrentCapacity = currentCapacity;
                        location.IsFull = currentCapacity >= location.MaxCapacity;
                        location.ModifiedDate = DateTime.Now;
                        location.ModifiedBy = _currentUserService.Username ?? "System";
                        updatedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("BULK_UPDATE", "Location", null, 
                        $"Refreshed capacities for {updatedCount} locations", null, new { 
                            UpdatedCount = updatedCount,
                            TotalLocations = locations.Count 
                        });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for capacity refresh");
                }

                return Ok(new
                {
                    success = true,
                    message = $"Capacity refresh completed. Updated {updatedCount} locations."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing all location capacities");
                return StatusCode(500, new { success = false, message = "Error refreshing capacities" });
            }
        }

        #endregion

        #region Validation & Utilities

        /// <summary>
        /// GET: api/location/check-code
        /// Check if location code is unique
        /// </summary>
        [HttpGet("api/location/check-code")]
        public async Task<IActionResult> CheckLocationCode([FromQuery] string code, [FromQuery] int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return Ok(new { isUnique = false, message = "Code is required" });
                }

                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var query = _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && l.Code == code && !l.IsDeleted);

                if (excludeId.HasValue)
                {
                    query = query.Where(l => l.Id != excludeId.Value);
                }

                var existingLocation = await query.FirstOrDefaultAsync();

                if (existingLocation != null)
                {
                    return Ok(new { isUnique = false, message = "Location code already exists" });
                }

                return Ok(new { isUnique = true, message = "Location code is available" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking location code uniqueness: {Code}", code);
                return StatusCode(500, new { isUnique = false, message = "Error checking location code" });
            }
        }

        /// <summary>
        /// GET: api/location/export
        /// Export locations to CSV (legacy method)
        /// </summary>
        [HttpGet("api/location/export")]
        public async Task<IActionResult> ExportLocations()
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var locations = await _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && !l.IsDeleted)
                    .Select(l => new
                    {
                        Code = l.Code,
                        Name = l.Name,
                        Description = l.Description,
                        MaxCapacity = l.MaxCapacity,
                        CurrentCapacity = l.CurrentCapacity,
                        IsActive = l.IsActive ? "Yes" : "No",
                        IsFull = l.IsFull ? "Yes" : "No",
                        CreatedDate = l.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        CreatedBy = l.CreatedBy
                    })
                    .ToListAsync();

                var csv = "Code,Name,Description,Max Capacity,Current Capacity,Active,Full,Created Date,Created By\n";
                csv += string.Join("\n", locations.Select(l => 
                    $"\"{l.Code}\",\"{l.Name}\",\"{l.Description}\",{l.MaxCapacity},{l.CurrentCapacity},{l.IsActive},{l.IsFull},{l.CreatedDate},{l.CreatedBy}"));

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                return File(bytes, "text/csv", $"locations_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting locations");
                return StatusCode(500, new { success = false, message = "Error exporting locations" });
            }
        }




        #endregion
    }

    #region Request Models

    public class LocationCreateRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = Constants.LOCATION_CATEGORY_STORAGE;
        public int MaxCapacity { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class LocationUpdateRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public int MaxCapacity { get; set; }
        public bool IsActive { get; set; }
    }

    public class MoveInventoryRequest
    {
        public int TargetLocationId { get; set; }
    }


    #endregion
}
//}