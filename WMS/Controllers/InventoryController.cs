using Microsoft.AspNetCore.Mvc;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Controllers
{
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly IItemService _itemService;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(
            IInventoryService inventoryService,
            IItemService itemService,
            ILogger<InventoryController> logger)
        {
            _inventoryService = inventoryService;
            _itemService = itemService;
            _logger = logger;
        }

        // GET: Inventory
        public async Task<IActionResult> Index(string? status = null, int? itemId = null, int? locationId = null)
        {
            try
            {
                IEnumerable<WMS.Models.Inventory> inventories;

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<InventoryStatus>(status, true, out var statusEnum))
                {
                    inventories = await _inventoryService.GetInventoryByStatusAsync(statusEnum);
                }
                else if (itemId.HasValue)
                {
                    inventories = await _inventoryService.GetInventoryByItemAsync(itemId.Value);
                }
                else if (locationId.HasValue)
                {
                    inventories = await _inventoryService.GetInventoryByLocationAsync(locationId.Value);
                }
                else
                {
                    inventories = await _inventoryService.GetAllInventoryAsync();
                }

                ViewBag.CurrentStatus = status;
                ViewBag.CurrentItemId = itemId;
                ViewBag.CurrentLocationId = locationId;

                // Statistics for dashboard
                ViewBag.Statistics = await _inventoryService.GetInventoryStatisticsAsync();
                ViewBag.LowStockItems = await _inventoryService.GetLowStockInventoryAsync();

                return View(inventories.OrderByDescending(inv => inv.LastUpdated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory");
                TempData["ErrorMessage"] = "Error loading inventory. Please try again.";
                return View(new List<WMS.Models.Inventory>());
            }
        }

        // GET: Inventory/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var inventory = await _inventoryService.GetInventoryByIdAsync(id);
                if (inventory == null)
                {
                    TempData["ErrorMessage"] = "Inventory not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Get movement history
                ViewBag.ItemLocationHistory = await _inventoryService.GetItemLocationHistoryAsync(inventory.ItemId);

                return View(inventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading inventory details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Inventory/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = await _inventoryService.GetInventoryViewModelAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing create inventory form");
                TempData["ErrorMessage"] = "Error preparing form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InventoryViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    viewModel = await _inventoryService.PopulateInventoryViewModelAsync(viewModel);
                    return View(viewModel);
                }

                // Check if inventory already exists for this item-location combination
                var existingInventory = await _inventoryService.GetInventoryByItemAndLocationAsync(viewModel.ItemId, viewModel.LocationId);
                if (existingInventory != null)
                {
                    TempData["ErrorMessage"] = "Inventory already exists for this item at this location. Use Edit to update quantity.";
                    viewModel = await _inventoryService.PopulateInventoryViewModelAsync(viewModel);
                    return View(viewModel);
                }

                var inventory = await _inventoryService.CreateInventoryAsync(viewModel);

                TempData["SuccessMessage"] = $"Inventory created successfully. {viewModel.Quantity} {viewModel.ItemUnit} @ {viewModel.LocationDisplay}";
                return RedirectToAction(nameof(Details), new { id = inventory.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating inventory");
                TempData["ErrorMessage"] = "Error creating inventory. Please try again.";

                viewModel = await _inventoryService.PopulateInventoryViewModelAsync(viewModel);
                return View(viewModel);
            }
        }

        // GET: Inventory/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var viewModel = await _inventoryService.GetInventoryViewModelAsync(id);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Inventory not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory for edit, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading inventory for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Inventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InventoryViewModel viewModel)
        {
            try
            {
                if (id != viewModel.Id)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    viewModel = await _inventoryService.PopulateInventoryViewModelAsync(viewModel);
                    return View(viewModel);
                }

                var updatedInventory = await _inventoryService.UpdateInventoryAsync(id, viewModel);

                TempData["SuccessMessage"] = $"Inventory updated successfully. {viewModel.Quantity} {viewModel.ItemUnit} @ {viewModel.LocationDisplay}";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error updating inventory. Please try again.";

                viewModel = await _inventoryService.PopulateInventoryViewModelAsync(viewModel);
                return View(viewModel);
            }
        }

        // GET: Inventory/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var inventory = await _inventoryService.GetInventoryByIdAsync(id);
                if (inventory == null)
                {
                    TempData["ErrorMessage"] = "Inventory not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(inventory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory for delete, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading inventory.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Inventory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var success = await _inventoryService.DeleteInventoryAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Inventory deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Could not delete inventory.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting inventory, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error deleting inventory. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Inventory/Putaway
        public async Task<IActionResult> Putaway()
        {
            try
            {
                var viewModel = await _inventoryService.GetPutawayViewModelAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing putaway form");
                TempData["ErrorMessage"] = "Error preparing putaway form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Inventory/Putaway
        // POST: Inventory/Putaway
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Putaway(PutawayViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    viewModel = await _inventoryService.GetPutawayViewModelAsync(viewModel.ASNId, viewModel.ASNDetailId);
                    return View(viewModel);
                }

                // Validate business rules
                if (!await _inventoryService.ValidatePutawayAsync(viewModel))
                {
                    TempData["ErrorMessage"] = viewModel.ValidationMessage;
                    viewModel = await _inventoryService.GetPutawayViewModelAsync(viewModel.ASNId, viewModel.ASNDetailId);
                    return View(viewModel);
                }

                var success = await _inventoryService.ProcessPutawayAsync(viewModel);

                if (success)
                {
                    TempData["SuccessMessage"] = $"Putaway berhasil: {viewModel.QuantityToPutaway} {viewModel.ItemUnit} {viewModel.ItemDisplay} ke {viewModel.LocationDisplay}";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Putaway gagal diproses. Silakan coba lagi.";
                    viewModel = await _inventoryService.GetPutawayViewModelAsync(viewModel.ASNId, viewModel.ASNDetailId);
                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing putaway");
                TempData["ErrorMessage"] = "Error processing putaway. Please try again.";

                viewModel = await _inventoryService.GetPutawayViewModelAsync(viewModel.ASNId, viewModel.ASNDetailId);
                return View(viewModel);
            }
        }

        // GET: Inventory/Tracking
        public async Task<IActionResult> Tracking(int? itemId = null, string? locationArea = null, string? stockStatus = null)
        {
            try
            {
                var viewModel = new ItemTrackingViewModel
                {
                    ItemId = itemId,
                    LocationArea = locationArea,
                    StockStatus = stockStatus
                };

                // Get tracking data based on filters
                var inventories = await _inventoryService.GetAllInventoryAsync();

                if (itemId.HasValue)
                {
                    inventories = inventories.Where(i => i.ItemId == itemId.Value);
                }

                if (!string.IsNullOrEmpty(locationArea))
                {
                    inventories = inventories.Where(i => i.Location.Code.StartsWith(locationArea));
                }

                if (!string.IsNullOrEmpty(stockStatus))
                {
                    inventories = inventories.Where(i =>
                    {
                        var level = i.StockLevel;
                        return level == stockStatus;
                    });
                }

                // Populate view model
                viewModel.ItemLocations = inventories.Select(i => new ItemLocationInfo
                {
                    InventoryId = i.Id,
                    ItemId = i.ItemId,
                    ItemCode = i.Item.ItemCode,
                    ItemName = i.Item.Name,
                    ItemUnit = i.Item.Unit,
                    LocationId = i.LocationId,
                    LocationCode = i.Location.Code,
                    LocationName = i.Location.Name,
                    Quantity = i.Quantity,
                    LastCostPrice = i.LastCostPrice,
                    Status = i.Status,
                    LastUpdated = i.LastUpdated,
                    Notes = i.Notes
                }).ToList();

                // Populate dropdown lists
                viewModel.AvailableItems = (await _itemService.GetActiveItemsAsync()).ToList();
                viewModel.LocationAreas = inventories.Select(i => i.Location.Code.Split('-')[0]).Distinct().ToList();

                // Calculate statistics
                viewModel.TotalItems = viewModel.ItemLocations.Select(il => il.ItemId).Distinct().Count();
                viewModel.TotalLocations = viewModel.ItemLocations.Select(il => il.LocationId).Distinct().Count();
                viewModel.TotalInventoryRecords = viewModel.ItemLocations.Count;
                viewModel.TotalInventoryValue = viewModel.ItemLocations.Sum(il => il.TotalValue);

                // Stock level counts
                viewModel.HighStockCount = viewModel.ItemLocations.Count(il => il.StockLevel == "TINGGI");
                viewModel.MediumStockCount = viewModel.ItemLocations.Count(il => il.StockLevel == "SEDANG");
                viewModel.LowStockCount = viewModel.ItemLocations.Count(il => il.StockLevel == "RENDAH");
                viewModel.CriticalStockCount = viewModel.ItemLocations.Count(il => il.StockLevel == "KRITIS");
                viewModel.EmptyStockCount = viewModel.ItemLocations.Count(il => il.StockLevel == "KOSONG");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading item tracking");
                TempData["ErrorMessage"] = "Error loading item tracking. Please try again.";
                return View(new ItemTrackingViewModel());
            }
        }

        // GET: Inventory/LocationStatus
        public async Task<IActionResult> LocationStatus()
        {
            try
            {
                ViewBag.LocationUtilization = await _inventoryService.GetLocationUtilizationAsync();
                ViewBag.OverCapacityLocations = await _inventoryService.GetOverCapacityLocationsAsync();

                var inventories = await _inventoryService.GetAllInventoryAsync();
                return View(inventories.GroupBy(i => i.LocationId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading location status");
                TempData["ErrorMessage"] = "Error loading location status.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Inventory/AdjustStock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(int id, int newQuantity, string adjustmentReason)
        {
            try
            {
                var adjustmentViewModel = new InventoryAdjustmentViewModel
                {
                    InventoryId = id,
                    NewQuantity = newQuantity,
                    AdjustmentReason = adjustmentReason
                };

                var success = await _inventoryService.AdjustStockAsync(adjustmentViewModel);

                if (success)
                {
                    TempData["SuccessMessage"] = "Stock adjustment completed successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to adjust stock.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting stock for inventory ID: {Id}", id);
                TempData["ErrorMessage"] = "Error adjusting stock.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Inventory/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? notes = null)
        {
            try
            {
                if (!Enum.TryParse<InventoryStatus>(status, true, out var statusEnum))
                {
                    TempData["ErrorMessage"] = "Invalid status.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var success = await _inventoryService.UpdateStockStatusAsync(id, statusEnum, notes);

                if (success)
                {
                    TempData["SuccessMessage"] = $"Inventory status updated to {statusEnum}.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update status.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for inventory ID: {Id}", id);
                TempData["ErrorMessage"] = "Error updating status.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Inventory/GetASNDetails
        [HttpGet]
        public async Task<JsonResult> GetASNDetails(int asnId)
        {
            try
            {
                var asnDetails = await _inventoryService.GetASNDetailsForPutawayAsync(asnId);

                return Json(new
                {
                    success = true,
                    details = asnDetails.Select(d => new
                    {
                        id = d.Id,
                        itemId = d.ItemId,
                        itemCode = d.Item?.ItemCode ?? "",
                        itemName = d.Item?.Name ?? "",
                        itemUnit = d.Item?.Unit ?? "",
                        receivedQuantity = d.ShippedQuantity,
                        costPrice = d.ActualPricePerItem
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ASN details for ASN ID: {ASNId}", asnId);
                return Json(new { success = false, message = "Error loading ASN details." });
            }
        }

        // GET: Inventory/LowStockReport
        public async Task<IActionResult> LowStockReport()
        {
            try
            {
                var lowStockItems = await _inventoryService.GetLowStockReportAsync();
                ViewBag.Statistics = await _inventoryService.GetInventoryStatisticsAsync();

                return View(lowStockItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading low stock report");
                TempData["ErrorMessage"] = "Error loading low stock report.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Inventory/MovementReport
        public async Task<IActionResult> MovementReport(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var movements = await _inventoryService.GetInventoryMovementReportAsync(fromDate, toDate);

                ViewBag.FromDate = fromDate;
                ViewBag.ToDate = toDate;

                return View(movements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory movement report");
                TempData["ErrorMessage"] = "Error loading movement report.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}