//using Microsoft.AspNetCore.Mvc;
//using WMS.Models;
//using WMS.Data.Repositories;

//namespace WMS.Controllers
//{
//    public class LocationController : Controller
//    {
//        private readonly ILocationRepository _locationRepository;
//        private readonly ILogger<LocationController> _logger;

//        public LocationController(
//            ILocationRepository locationRepository,
//            ILogger<LocationController> logger)
//        {
//            _locationRepository = locationRepository;
//            _logger = logger;
//        }

//        // GET: Location
//        public async Task<IActionResult> Index(string? searchTerm = null, bool? isActive = null, bool? isFull = null)
//        {
//            try
//            {
//                IEnumerable<Location> locations;

//                if (!string.IsNullOrEmpty(searchTerm))
//                {
//                    locations = await _locationRepository.SearchLocationsAsync(searchTerm);
//                }
//                else if (isActive.HasValue)
//                {
//                    if (isActive.Value)
//                        locations = await _locationRepository.GetActiveLocationsAsync();
//                    else
//                        locations = (await _locationRepository.GetAllAsync()).Where(l => !l.IsActive);
//                }
//                else
//                {
//                    locations = await _locationRepository.GetAllWithInventoryAsync();
//                }

//                // Apply full status filter if specified
//                if (isFull.HasValue)
//                {
//                    locations = locations.Where(l => l.IsFull == isFull.Value);
//                }

//                ViewBag.SearchTerm = searchTerm;
//                ViewBag.IsActive = isActive;
//                ViewBag.IsFull = isFull;

//                // Get statistics
//                ViewBag.Statistics = await _locationRepository.GetLocationStatisticsAsync();

//                return View(locations.OrderBy(l => l.Code));
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error loading locations");
//                TempData["ErrorMessage"] = "Error loading locations. Please try again.";
//                return View(new List<Location>());
//            }
//        }

//        // GET: Location/Details/5
//        public async Task<IActionResult> Details(int id)
//        {
//            try
//            {
//                var location = await _locationRepository.GetByIdWithInventoryAsync(id);
//                if (location == null)
//                {
//                    TempData["ErrorMessage"] = "Location not found.";
//                    return RedirectToAction(nameof(Index));
//                }

//                // Calculate additional details
//                var totalItems = location.Inventories.Count();
//                var totalQuantity = location.Inventories.Sum(i => i.Quantity);
//                var totalValue = location.Inventories.Sum(i => i.Quantity * i.LastCostPrice);
//                var itemTypes = location.Inventories.Select(i => i.Item.Name).Distinct().Count();

//                ViewBag.TotalItems = totalItems;
//                ViewBag.TotalQuantity = totalQuantity;
//                ViewBag.TotalValue = totalValue;
//                ViewBag.ItemTypes = itemTypes;
//                ViewBag.RecentInventory = location.Inventories.OrderByDescending(i => i.LastUpdated).Take(10);

//                return View(location);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error loading location details for ID: {Id}", id);
//                TempData["ErrorMessage"] = "Error loading location details.";
//                return RedirectToAction(nameof(Index));
//            }
//        }

//        // GET: Location/Create
//        public IActionResult Create()
//        {
//            return View(new Location());
//        }

//        // POST: Location/Create
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(Location location)
//        {
//            try
//            {
//                if (!ModelState.IsValid)
//                {
//                    return View(location);
//                }

//                // Validate location code uniqueness
//                if (await _locationRepository.ExistsByCodeAsync(location.Code))
//                {
//                    ModelState.AddModelError("Code", "A location with this code already exists. Please use a different code.");
//                    return View(location);
//                }

//                // Validate business rules
//                if (location.MaxCapacity <= 0)
//                {
//                    ModelState.AddModelError("MaxCapacity", "Maximum capacity must be greater than 0.");
//                    return View(location);
//                }

//                // Set created date and user
//                location.CreatedDate = DateTime.Now;
//                location.CreatedBy = User.Identity?.Name ?? "System";

//                var createdLocation = await _locationRepository.AddAsync(location);

//                TempData["SuccessMessage"] = $"Location '{createdLocation.Code} - {createdLocation.Name}' created successfully.";
//                return RedirectToAction(nameof(Details), new { id = createdLocation.Id });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error creating location");
//                TempData["ErrorMessage"] = "Error creating location. Please try again.";
//                return View(location);
//            }
//        }

//        // GET: Location/Edit/5
//        public async Task<IActionResult> Edit(int id)
//        {
//            try
//            {
//                var location = await _locationRepository.GetByIdAsync(id);
//                if (location == null)
//                {
//                    TempData["ErrorMessage"] = "Location not found.";
//                    return RedirectToAction(nameof(Index));
//                }

//                return View(location);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error loading location for edit, ID: {Id}", id);
//                TempData["ErrorMessage"] = "Error loading location for editing.";
//                return RedirectToAction(nameof(Index));
//            }
//        }

//        // POST: Location/Edit/5
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, Location location)
//        {
//            try
//            {
//                if (id != location.Id)
//                {
//                    return BadRequest();
//                }

//                if (!ModelState.IsValid)
//                {
//                    return View(location);
//                }

//                // Get existing location to check code change
//                var existingLocation = await _locationRepository.GetByIdAsync(id);
//                if (existingLocation == null)
//                {
//                    TempData["ErrorMessage"] = "Location not found.";
//                    return RedirectToAction(nameof(Index));
//                }

//                // Validate location code uniqueness (if code changed)
//                if (existingLocation.Code != location.Code && await _locationRepository.ExistsByCodeAsync(location.Code))
//                {
//                    ModelState.AddModelError("Code", "A location with this code already exists. Please use a different code.");
//                    return View(location);
//                }

//                // Validate business rules
//                if (location.MaxCapacity <= 0)
//                {
//                    ModelState.AddModelError("MaxCapacity", "Maximum capacity must be greater than 0.");
//                    return View(location);
//                }

//                // Validate that new max capacity is not less than current capacity
//                if (location.MaxCapacity < existingLocation.CurrentCapacity)
//                {
//                    ModelState.AddModelError("MaxCapacity", $"Maximum capacity cannot be less than current capacity ({existingLocation.CurrentCapacity}).");
//                    return View(location);
//                }

//                // Update fields
//                existingLocation.Code = location.Code;
//                existingLocation.Name = location.Name;
//                existingLocation.Description = location.Description;
//                existingLocation.MaxCapacity = location.MaxCapacity;
//                existingLocation.IsActive = location.IsActive;
//                existingLocation.ModifiedDate = DateTime.Now;
//                existingLocation.ModifiedBy = User.Identity?.Name ?? "System";

//                // Recalculate full status
//                existingLocation.IsFull = existingLocation.CurrentCapacity >= existingLocation.MaxCapacity;

//                await _locationRepository.UpdateAsync(existingLocation);

//                TempData["SuccessMessage"] = $"Location '{existingLocation.Code} - {existingLocation.Name}' updated successfully.";
//                return RedirectToAction(nameof(Details), new { id });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error updating location, ID: {Id}", id);
//                TempData["ErrorMessage"] = "Error updating location. Please try again.";
//                return View(location);
//            }
//        }

//        // GET: Location/Delete/5
//        public async Task<IActionResult> Delete(int id)
//        {
//            try
//            {
//                var location = await _locationRepository.GetByIdWithInventoryAsync(id);
//                if (location == null)
//                {
//                    TempData["ErrorMessage"] = "Location not found.";
//                    return RedirectToAction(nameof(Index));
//                }

//                // Check if location can be deleted
//                if (location.Inventories.Any())
//                {
//                    TempData["ErrorMessage"] = "This location cannot be deleted because it contains inventory items.";
//                    return RedirectToAction(nameof(Details), new { id });
//                }

//                return View(location);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error loading location for delete, ID: {Id}", id);
//                TempData["ErrorMessage"] = "Error loading location.";
//                return RedirectToAction(nameof(Index));
//            }
//        }

//        // POST: Location/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(int id)
//        {
//            try
//            {
//                var location = await _locationRepository.GetByIdWithInventoryAsync(id);
//                if (location == null)
//                {
//                    TempData["ErrorMessage"] = "Location not found.";
//                    return RedirectToAction(nameof(Index));
//                }

//                // Double-check if location can be deleted
//                if (location.Inventories.Any())
//                {
//                    TempData["ErrorMessage"] = "This location cannot be deleted because it contains inventory items.";
//                    return RedirectToAction(nameof(Details), new { id });
//                }

//                await _locationRepository.DeleteAsync(id);
//                TempData["SuccessMessage"] = "Location deleted successfully.";

//                return RedirectToAction(nameof(Index));
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error deleting location, ID: {Id}", id);
//                TempData["ErrorMessage"] = "Error deleting location. Please try again.";
//                return RedirectToAction(nameof(Index));
//            }
//        }

//        // POST: Location/ToggleStatus/5
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> ToggleStatus(int id)
//        {
//            try
//            {
//                var location = await _locationRepository.GetByIdAsync(id);
//                if (location == null)
//                {
//                    TempData["ErrorMessage"] = "Location not found.";
//                    return RedirectToAction(nameof(Index));
//                }

//                location.IsActive = !location.IsActive;
//                location.ModifiedDate = DateTime.Now;
//                location.ModifiedBy = User.Identity?.Name ?? "System";

//                await _locationRepository.UpdateAsync(location);

//                var status = location.IsActive ? "activated" : "deactivated";
//                TempData["SuccessMessage"] = $"Location '{location.Code}' {status} successfully.";

//                return RedirectToAction(nameof(Details), new { id });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error toggling location status, ID: {Id}", id);
//                TempData["ErrorMessage"] = "Error updating location status.";
//                return RedirectToAction(nameof(Details), new { id });
//            }
//        }

//        // POST: Location/UpdateCapacity/5
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> UpdateCapacity(int id)
//        {
//            try
//            {
//                await _locationRepository.UpdateCapacityAsync(id);

//                TempData["SuccessMessage"] = "Location capacity updated successfully.";
//                return RedirectToAction(nameof(Details), new { id });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error updating location capacity, ID: {Id}", id);
//                TempData["ErrorMessage"] = "Error updating location capacity.";
//                return RedirectToAction(nameof(Details), new { id });
//            }
//        }

//        // GET: Location/CheckCode
//        [HttpGet]
//        public async Task<JsonResult> CheckCode(string code, int? excludeId = null)
//        {
//            try
//            {
//                var exists = await _locationRepository.ExistsByCodeAsync(code);

//                // If we're editing an existing location, check if the code belongs to a different location
//                if (excludeId.HasValue && exists)
//                {
//                    var existingLocation = await _locationRepository.GetByIdAsync(excludeId.Value);
//                    exists = existingLocation?.Code != code;
//                }

//                return Json(new
//                {
//                    isUnique = !exists,
//                    message = exists ? "Location code already exists" : "Location code is available"
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error checking location code uniqueness: {Code}", code);
//                return Json(new { isUnique = false, message = "Error checking location code" });
//            }
//        }

//        // GET: Location/GetLocationsBySearch
//        [HttpGet]
//        public async Task<JsonResult> GetLocationsBySearch(string searchTerm)
//        {
//            try
//            {
//                var locations = await _locationRepository.SearchLocationsAsync(searchTerm);

//                return Json(new
//                {
//                    success = true,
//                    locations = locations.Select(l => new
//                    {
//                        id = l.Id,
//                        code = l.Code,
//                        name = l.Name,
//                        maxCapacity = l.MaxCapacity,
//                        currentCapacity = l.CurrentCapacity,
//                        availableCapacity = l.AvailableCapacity,
//                        isFull = l.IsFull,
//                        isActive = l.IsActive,
//                        displayName = l.DisplayName
//                    })
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error searching locations: {SearchTerm}", searchTerm);
//                return Json(new { success = false, message = "Error searching locations" });
//            }
//        }

//        // GET: Location/GetAvailableLocations
//        [HttpGet]
//        public async Task<JsonResult> GetAvailableLocations()
//        {
//            try
//            {
//                var locations = await _locationRepository.GetAvailableLocationsAsync();

//                return Json(new
//                {
//                    success = true,
//                    locations = locations.Select(l => new
//                    {
//                        id = l.Id,
//                        code = l.Code,
//                        name = l.Name,
//                        availableCapacity = l.AvailableCapacity,
//                        displayName = l.DisplayName
//                    })
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting available locations");
//                return Json(new { success = false, message = "Error getting available locations" });
//            }
//        }

//        // GET: Location/GetLocationDetails
//        [HttpGet]
//        public async Task<JsonResult> GetLocationDetails(int id)
//        {
//            try
//            {
//                var location = await _locationRepository.GetByIdWithInventoryAsync(id);
//                if (location == null)
//                {
//                    return Json(new { success = false, message = "Location not found" });
//                }

//                return Json(new
//                {
//                    success = true,
//                    id = location.Id,
//                    code = location.Code,
//                    name = location.Name,
//                    description = location.Description,
//                    maxCapacity = location.MaxCapacity,
//                    currentCapacity = location.CurrentCapacity,
//                    availableCapacity = location.AvailableCapacity,
//                    capacityPercentage = location.CapacityPercentage,
//                    isFull = location.IsFull,
//                    isActive = location.IsActive,
//                    capacityStatus = location.CapacityStatus,
//                    displayName = location.DisplayName,
//                    inventoryCount = location.Inventories.Count(),
//                    totalValue = location.Inventories.Sum(i => i.Quantity * i.LastCostPrice)
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting location details for ID: {Id}", id);
//                return Json(new { success = false, message = "Error getting location details" });
//            }
//        }

//        // GET: Location/CapacityReport
//        public async Task<IActionResult> CapacityReport()
//        {
//            try
//            {
//                var locations = await _locationRepository.GetAllWithInventoryAsync();
//                var statistics = await _locationRepository.GetLocationStatisticsAsync();

//                var reportData = locations.Select(l => new
//                {
//                    Location = l,
//                    CapacityUtilization = l.CapacityPercentage,
//                    ItemCount = l.Inventories.Count(),
//                    TotalValue = l.Inventories.Sum(i => i.Quantity * i.LastCostPrice)
//                }).OrderByDescending(r => r.CapacityUtilization);

//                ViewBag.Statistics = statistics;

//                return View(reportData);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error loading capacity report");
//                TempData["ErrorMessage"] = "Error loading capacity report.";
//                return RedirectToAction(nameof(Index));
//            }
//        }

//        // GET: Location/LocationStatus
//        public async Task<IActionResult> LocationStatus()
//        {
//            try
//            {
//                var locations = await _locationRepository.GetActiveLocationsAsync();
//                var statistics = await _locationRepository.GetLocationStatisticsAsync();

//                ViewBag.Statistics = statistics;

//                return View(locations);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error loading location status");
//                TempData["ErrorMessage"] = "Error loading location status.";
//                return RedirectToAction(nameof(Index));
//            }
//        }

//        // GET: Location/Export
//        public async Task<IActionResult> Export()
//        {
//            try
//            {
//                var locations = await _locationRepository.GetAllWithInventoryAsync();
//                var statistics = await _locationRepository.GetLocationStatisticsAsync();

//                ViewBag.Statistics = statistics;

//                // Here you would implement Excel export logic
//                // For now, returning the view for demonstration
//                return View(locations);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error exporting locations");
//                TempData["ErrorMessage"] = "Error exporting locations.";
//                return RedirectToAction(nameof(Index));
//            }
//        }
//    }
//}