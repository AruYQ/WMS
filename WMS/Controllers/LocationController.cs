using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Attributes;
using WMS.Data;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace WMS.Controllers
{
    /// <summary>
    /// API Controller untuk Location management - AJAX-based
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [RequirePermission("LOCATION_MANAGE")]
    public class LocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<LocationController> _logger;

        public LocationController(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<LocationController> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        #region Dashboard & Statistics

        /// <summary>
        /// GET: api/location/dashboard
        /// Get location statistics for dashboard
        /// </summary>
        [HttpGet("dashboard")]
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
        [HttpGet]
        public async Task<IActionResult> GetLocations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? capacity = null)
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
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLocation(int id)
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
                    .Select(l => new
                    {
                        id = l.Id,
                        code = l.Code,
                        name = l.Name,
                        description = l.Description,
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
                        createdBy = l.CreatedBy,
                        modifiedBy = l.ModifiedBy
                    })
                    .FirstOrDefaultAsync();

                if (location == null)
                {
                    return NotFound(new { success = false, message = "Location not found" });
                }

                return Ok(new { success = true, data = location });
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
        [HttpPost]
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

                // Create location entity
                var location = new Location
                {
                    Code = request.Code,
                    Name = request.Name,
                    Description = request.Description,
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

                return Ok(new
                {
                    success = true,
                    message = $"Location '{location.Code} - {location.Name}' created successfully",
                    data = new { id = location.Id }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating location");
                return StatusCode(500, new { success = false, message = "Error creating location" });
            }
        }

        /// <summary>
        /// PUT: api/location/{id}
        /// Update existing location
        /// </summary>
        [HttpPut("{id}")]
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

                // Update location
                location.Code = request.Code;
                location.Name = request.Name;
                location.Description = request.Description;
                location.MaxCapacity = request.MaxCapacity;
                location.IsActive = request.IsActive;
                location.ModifiedDate = DateTime.Now;
                location.ModifiedBy = _currentUserService.Username ?? "System";

                // Recalculate capacity status
                location.IsFull = location.CurrentCapacity >= location.MaxCapacity;

                await _context.SaveChangesAsync();

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
        [HttpDelete("{id}")]
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
        [HttpPost("{id}/clear-inventory")]
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
        [HttpPatch("{id}/toggle-status")]
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

                location.IsActive = !location.IsActive;
                location.ModifiedDate = DateTime.Now;
                location.ModifiedBy = _currentUserService.Username ?? "System";

                await _context.SaveChangesAsync();

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
        [HttpPatch("{id}/update-capacity")]
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
        [HttpPost("refresh-all-capacities")]
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
        [HttpGet("check-code")]
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
        [HttpGet("export")]
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

        /// <summary>
        /// POST: api/location/export-excel
        /// Export locations to Excel with advanced filtering and formatting
        /// </summary>
        [HttpPost("export-excel")]
        public async Task<IActionResult> ExportLocationsExcel([FromBody] LocationExportRequest request)
        {
            try
            {
                _logger.LogInformation("Starting Excel export with request: {@Request}", request);
                
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    _logger.LogWarning("No company context found for Excel export");
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                // Get company info
                _logger.LogInformation("Fetching company info for company ID: {CompanyId}", companyId.Value);
                var company = await _context.Companies.FindAsync(companyId.Value);
                if (company == null)
                {
                    _logger.LogError("Company not found for ID: {CompanyId}", companyId.Value);
                    return NotFound(new { success = false, message = "Company not found" });
                }
                
                _logger.LogInformation("Company found: {CompanyName} ({CompanyCode})", company.Name, company.Code);

                // Build query with filters
                _logger.LogInformation("Building query with filters...");
                var query = _context.Locations
                    .Where(l => l.CompanyId == companyId.Value && !l.IsDeleted);

                // Apply filters
                if (request.DateFrom.HasValue)
                {
                    _logger.LogInformation("Applying date from filter: {DateFrom}", request.DateFrom.Value);
                    query = query.Where(l => l.CreatedDate >= request.DateFrom.Value);
                }

                if (request.DateTo.HasValue)
                {
                    _logger.LogInformation("Applying date to filter: {DateTo}", request.DateTo.Value);
                    query = query.Where(l => l.CreatedDate <= request.DateTo.Value);
                }

                if (!string.IsNullOrEmpty(request.StatusFilter))
                {
                    _logger.LogInformation("Applying status filter: {StatusFilter}", request.StatusFilter);
                    switch (request.StatusFilter.ToLower())
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
                        case "empty":
                            query = query.Where(l => l.CurrentCapacity == 0);
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(request.LocationTypeFilter))
                {
                    _logger.LogInformation("Applying location type filter: {LocationTypeFilter}", request.LocationTypeFilter);
                    if (request.LocationTypeFilter == "storage")
                    {
                        query = query.Where(l => !l.Code.Contains("RECEIVING") && 
                                                !l.Code.Contains("SHIPPING") && 
                                                !l.Code.Contains("QUARANTINE") && 
                                                !l.Code.Contains("RETURNS"));
                    }
                    else if (request.LocationTypeFilter == "special")
                    {
                        query = query.Where(l => l.Code.Contains("RECEIVING") || 
                                                l.Code.Contains("SHIPPING") || 
                                                l.Code.Contains("QUARANTINE") || 
                                                l.Code.Contains("RETURNS"));
                    }
                }

                if (request.CapacityFrom.HasValue)
                {
                    _logger.LogInformation("Applying capacity from filter: {CapacityFrom}", request.CapacityFrom.Value);
                    query = query.Where(l => l.MaxCapacity >= request.CapacityFrom.Value);
                }

                if (request.CapacityTo.HasValue)
                {
                    _logger.LogInformation("Applying capacity to filter: {CapacityTo}", request.CapacityTo.Value);
                    query = query.Where(l => l.MaxCapacity <= request.CapacityTo.Value);
                }

                if (!string.IsNullOrEmpty(request.SearchText))
                {
                    _logger.LogInformation("Applying search text filter: {SearchText}", request.SearchText);
                    query = query.Where(l => l.Code.Contains(request.SearchText) || 
                                           l.Name.Contains(request.SearchText) ||
                                           (l.Description != null && l.Description.Contains(request.SearchText)));
                }

                _logger.LogInformation("Executing database query to fetch locations...");
                var locations = await query
                    .OrderBy(l => l.Code)
                    .Select(l => new
                    {
                        Id = l.Id,
                        Code = l.Code,
                        Name = l.Name,
                        Description = l.Description ?? "",
                        MaxCapacity = l.MaxCapacity,
                        CurrentCapacity = l.CurrentCapacity,
                        AvailableCapacity = l.MaxCapacity - l.CurrentCapacity,
                        UtilizationPercentage = l.MaxCapacity > 0 ? (double)l.CurrentCapacity / l.MaxCapacity * 100 : 0,
                        IsActive = l.IsActive,
                        IsFull = l.IsFull,
                        CapacityStatus = l.IsFull ? "FULL" : 
                                       l.CurrentCapacity >= l.MaxCapacity * 0.8 ? "NEAR FULL" : 
                                       l.CurrentCapacity > 0 ? "IN USE" : "AVAILABLE",
                        CreatedDate = l.CreatedDate,
                        ModifiedDate = l.ModifiedDate,
                        CreatedBy = l.CreatedBy ?? "System"
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {LocationCount} locations from database", locations.Count);

                // Generate Excel file
                _logger.LogInformation("Starting Excel file generation...");
                var excelBytes = GenerateLocationExcel(locations, company, request);
                _logger.LogInformation("Excel file generated successfully, size: {FileSize} bytes", excelBytes.Length);
                
                var fileName = $"Locations_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                _logger.LogInformation("Returning Excel file: {FileName}", fileName);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting locations to Excel");
                return StatusCode(500, new { success = false, message = "Error exporting locations to Excel" });
            }
        }

        private byte[] GenerateLocationExcel(IEnumerable<dynamic> locations, Company company, LocationExportRequest request)
        {
            try
            {
                _logger.LogInformation("Creating Excel package...");
                using var package = new ExcelPackage();
                package.Workbook.Properties.Title = "Location Management Report";
                package.Workbook.Properties.Author = company?.Name ?? "WMS System";
                package.Workbook.Properties.Created = DateTime.Now;

                _logger.LogInformation("Creating Summary sheet...");
                // Create Summary Sheet
                CreateSummarySheet(package, locations, company, request);
                
                _logger.LogInformation("Creating Details sheet...");
                // Create Details Sheet
                CreateDetailsSheet(package, locations, company);
                
                _logger.LogInformation("Creating Statistics sheet...");
                // Create Statistics Sheet
                CreateStatisticsSheet(package, locations, company);

                _logger.LogInformation("Generating Excel byte array...");
                var result = package.GetAsByteArray();
                _logger.LogInformation("Excel byte array generated successfully, size: {Size} bytes", result.Length);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateLocationExcel method");
                throw new InvalidOperationException("Failed to generate Excel file", ex);
            }
        }

        private void CreateSummarySheet(ExcelPackage package, IEnumerable<dynamic> locations, Company company, LocationExportRequest request)
        {
            try
            {
                _logger.LogInformation("Creating Summary sheet...");
                var sheet = package.Workbook.Worksheets.Add("Summary");
                
                // Header styling
                using (var range = sheet.Cells[1, 1, 1, 4])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 14;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // Company Info
                sheet.Cells[1, 1].Value = $"LOCATION MANAGEMENT REPORT";
                sheet.Cells[2, 1].Value = $"Company: {company?.Name ?? "Unknown"} ({company?.Code ?? "N/A"})";
                sheet.Cells[3, 1].Value = $"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                sheet.Cells[4, 1].Value = $"Exported By: {_currentUserService.Username ?? "System"}";

                // Filters Applied
                if (request?.DateFrom.HasValue == true || request?.DateTo.HasValue == true || 
                    !string.IsNullOrEmpty(request?.StatusFilter) || !string.IsNullOrEmpty(request?.SearchText))
                {
                    sheet.Cells[5, 1].Value = "Filters Applied:";
                    int filterRow = 6;
                    
                    if (request?.DateFrom.HasValue == true || request?.DateTo.HasValue == true)
                    {
                        var dateRange = $"{request.DateFrom?.ToString("yyyy-MM-dd") ?? "Start"} to {request.DateTo?.ToString("yyyy-MM-dd") ?? "End"}";
                        sheet.Cells[filterRow++, 1].Value = $"Date Range: {dateRange}";
                    }
                    
                    if (!string.IsNullOrEmpty(request?.StatusFilter))
                        sheet.Cells[filterRow++, 1].Value = $"Status: {request.StatusFilter}";
                    
                    if (!string.IsNullOrEmpty(request?.SearchText))
                        sheet.Cells[filterRow++, 1].Value = $"Search: {request.SearchText}";
                }

                // Statistics
                var locationList = locations?.ToList() ?? new List<dynamic>();
                var statsRow = 8;
                
                sheet.Cells[statsRow, 1].Value = "SUMMARY STATISTICS";
                sheet.Cells[statsRow, 1].Style.Font.Bold = true;
                sheet.Cells[statsRow, 1].Style.Font.Size = 12;
                
                statsRow++;
                sheet.Cells[statsRow, 1].Value = "Total Locations:";
                sheet.Cells[statsRow, 2].Value = locationList.Count;
                
                statsRow++;
                sheet.Cells[statsRow, 1].Value = "Active Locations:";
                sheet.Cells[statsRow, 2].Value = locationList.Count(l => l.IsActive);
                
                statsRow++;
                sheet.Cells[statsRow, 1].Value = "Full Locations:";
                sheet.Cells[statsRow, 2].Value = locationList.Count(l => l.IsFull);
                
                statsRow++;
                sheet.Cells[statsRow, 1].Value = "Empty Locations:";
                sheet.Cells[statsRow, 2].Value = locationList.Count(l => l.CurrentCapacity == 0);
                
                statsRow++;
                sheet.Cells[statsRow, 1].Value = "Average Utilization:";
                sheet.Cells[statsRow, 2].Value = locationList.Any() ? locationList.Average(l => l.UtilizationPercentage) : 0;
                sheet.Cells[statsRow, 2].Style.Numberformat.Format = "0.00%";

                // Auto-fit columns
                sheet.Cells.AutoFitColumns();
                _logger.LogInformation("Summary sheet created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Summary sheet");
                throw new InvalidOperationException("Failed to create Summary sheet", ex);
            }
        }

        private void CreateDetailsSheet(ExcelPackage package, IEnumerable<dynamic> locations, Company company)
        {
            try
            {
                _logger.LogInformation("Creating Details sheet...");
                var sheet = package.Workbook.Worksheets.Add("Location Details");
                var locationList = locations?.ToList() ?? new List<dynamic>();

            // Headers
            var headers = new[]
            {
                "Code", "Name", "Description", "Max Capacity", "Current Capacity", 
                "Available Capacity", "Utilization %", "Status", "Active", "Created Date", "Created By"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[1, i + 1].Value = headers[i];
                sheet.Cells[1, i + 1].Style.Font.Bold = true;
                sheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Data
            for (int i = 0; i < locationList.Count; i++)
            {
                var location = locationList[i];
                var row = i + 2;

                sheet.Cells[row, 1].Value = location.Code;
                sheet.Cells[row, 2].Value = location.Name;
                sheet.Cells[row, 3].Value = location.Description;
                sheet.Cells[row, 4].Value = location.MaxCapacity;
                sheet.Cells[row, 5].Value = location.CurrentCapacity;
                sheet.Cells[row, 6].Value = location.AvailableCapacity;
                sheet.Cells[row, 7].Value = location.UtilizationPercentage / 100;
                sheet.Cells[row, 7].Style.Numberformat.Format = "0.00%";
                sheet.Cells[row, 8].Value = location.CapacityStatus;
                sheet.Cells[row, 9].Value = location.IsActive ? "Yes" : "No";
                sheet.Cells[row, 10].Value = location.CreatedDate;
                sheet.Cells[row, 10].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                sheet.Cells[row, 11].Value = location.CreatedBy;

                // Conditional formatting for capacity status
                if (location.IsFull)
                {
                    sheet.Cells[row, 8].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                }
                else if (location.UtilizationPercentage >= 80)
                {
                    sheet.Cells[row, 8].Style.Font.Color.SetColor(System.Drawing.Color.Orange);
                }
                else if (location.UtilizationPercentage == 0)
                {
                    sheet.Cells[row, 8].Style.Font.Color.SetColor(System.Drawing.Color.Green);
                }

                // Conditional formatting for utilization
                if (location.UtilizationPercentage >= 100)
                {
                    sheet.Cells[row, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    sheet.Cells[row, 7].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                }
                else if (location.UtilizationPercentage >= 80)
                {
                    sheet.Cells[row, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    sheet.Cells[row, 7].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Orange);
                }
            }

                // Auto-fit columns and freeze panes
                sheet.Cells.AutoFitColumns();
                sheet.View.FreezePanes(2, 1);
                _logger.LogInformation("Details sheet created successfully with {LocationCount} locations", locationList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Details sheet");
                throw new InvalidOperationException("Failed to create Details sheet", ex);
            }
        }

        private void CreateStatisticsSheet(ExcelPackage package, IEnumerable<dynamic> locations, Company company)
        {
            try
            {
                _logger.LogInformation("Creating Statistics sheet...");
                var sheet = package.Workbook.Worksheets.Add("Statistics");
                var locationList = locations?.ToList() ?? new List<dynamic>();

            // Capacity Analysis
            sheet.Cells[1, 1].Value = "CAPACITY ANALYSIS";
            sheet.Cells[1, 1].Style.Font.Bold = true;
            sheet.Cells[1, 1].Style.Font.Size = 12;

            var capacityStats = new[]
            {
                new { Category = "Empty (0%)", Count = locationList.Count(l => l.CurrentCapacity == 0) },
                new { Category = "Low (1-25%)", Count = locationList.Count(l => l.CurrentCapacity > 0 && l.UtilizationPercentage <= 25) },
                new { Category = "Medium (26-75%)", Count = locationList.Count(l => l.UtilizationPercentage > 25 && l.UtilizationPercentage <= 75) },
                new { Category = "High (76-99%)", Count = locationList.Count(l => l.UtilizationPercentage > 75 && l.UtilizationPercentage < 100) },
                new { Category = "Full (100%)", Count = locationList.Count(l => l.UtilizationPercentage >= 100) }
            };

            sheet.Cells[3, 1].Value = "Category";
            sheet.Cells[3, 2].Value = "Count";
            sheet.Cells[3, 3].Value = "Percentage";
            
            for (int i = 0; i < capacityStats.Length; i++)
            {
                var stat = capacityStats[i];
                var row = i + 4;
                var percentage = locationList.Count > 0 ? (double)stat.Count / locationList.Count * 100 : 0;

                sheet.Cells[row, 1].Value = stat.Category;
                sheet.Cells[row, 2].Value = stat.Count;
                sheet.Cells[row, 3].Value = percentage / 100;
                sheet.Cells[row, 3].Style.Numberformat.Format = "0.00%";
            }

            // Location Type Analysis
            var typeStatsRow = capacityStats.Length + 6;
            sheet.Cells[typeStatsRow, 1].Value = "LOCATION TYPE ANALYSIS";
            sheet.Cells[typeStatsRow, 1].Style.Font.Bold = true;
            sheet.Cells[typeStatsRow, 1].Style.Font.Size = 12;

            var storageLocations = locationList.Count(l => !l.Code.Contains("RECEIVING") && 
                                                         !l.Code.Contains("SHIPPING") && 
                                                         !l.Code.Contains("QUARANTINE") && 
                                                         !l.Code.Contains("RETURNS"));
            var specialLocations = locationList.Count - storageLocations;

            sheet.Cells[typeStatsRow + 2, 1].Value = "Storage Locations";
            sheet.Cells[typeStatsRow + 2, 2].Value = storageLocations;
            
            sheet.Cells[typeStatsRow + 3, 1].Value = "Special Areas";
            sheet.Cells[typeStatsRow + 3, 2].Value = specialLocations;

                // Auto-fit columns
                sheet.Cells.AutoFitColumns();
                _logger.LogInformation("Statistics sheet created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Statistics sheet");
                throw new InvalidOperationException("Failed to create Statistics sheet", ex);
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
        public int MaxCapacity { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class LocationUpdateRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxCapacity { get; set; }
        public bool IsActive { get; set; }
    }

    public class MoveInventoryRequest
    {
        public int TargetLocationId { get; set; }
    }

    public class LocationExportRequest
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? StatusFilter { get; set; } // active, inactive, full, empty
        public string? LocationTypeFilter { get; set; } // storage, special
        public int? CapacityFrom { get; set; }
        public int? CapacityTo { get; set; }
        public string? SearchText { get; set; }
    }

    #endregion
}
//}