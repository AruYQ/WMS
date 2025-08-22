using Microsoft.AspNetCore.Mvc;
using WMS.Models;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Controllers
{
    public class ItemController : Controller
    {
        private readonly IItemService _itemService;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<ItemController> _logger;

        public ItemController(
            IItemService itemService,
            IInventoryService inventoryService,
            ILogger<ItemController> logger)
        {
            _itemService = itemService;
            _inventoryService = inventoryService;
            _logger = logger;
        }

        // GET: Item
        public async Task<IActionResult> Index(string? searchTerm = null, bool? isActive = null)
        {
            try
            {
                IEnumerable<Item> items;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    items = await _itemService.SearchItemsAsync(searchTerm);
                }
                else if (isActive.HasValue)
                {
                    if (isActive.Value)
                        items = await _itemService.GetActiveItemsAsync();
                    else
                        items = (await _itemService.GetAllItemsAsync()).Where(i => !i.IsActive);
                }
                else
                {
                    items = await _itemService.GetAllItemsAsync();
                }

                ViewBag.SearchTerm = searchTerm;
                ViewBag.IsActive = isActive;

                // Get stock summary for all items
                ViewBag.StockSummary = await _itemService.GetItemStockSummaryAsync();
                ViewBag.Statistics = await _itemService.GetItemStatisticsAsync();

                return View(items.OrderBy(i => i.ItemCode));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading items");
                TempData["ErrorMessage"] = "Error loading items. Please try again.";
                return View(new List<Item>());
            }
        }

        // GET: Item/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var item = await _itemService.GetItemByIdAsync(id);
                if (item == null)
                {
                    TempData["ErrorMessage"] = "Item not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Get inventory details for this item
                ViewBag.InventoryDetails = await _itemService.GetItemInventoryDetailsAsync(id);
                ViewBag.TotalStock = await _itemService.GetItemTotalStockAsync(id);
                ViewBag.TotalValue = await _itemService.GetItemTotalValueAsync(id);
                ViewBag.PriceHistory = await _itemService.GetItemPriceHistoryAsync(id);

                return View(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading item details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading item details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Item/Create
        public IActionResult Create()
        {
            return View(new Item());
        }

        // POST: Item/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Item item)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(item);
                }

                // Validate item code uniqueness
                if (!await _itemService.IsItemCodeUniqueAsync(item.ItemCode))
                {
                    ModelState.AddModelError("ItemCode", "Item code already exists. Please use a different code.");
                    return View(item);
                }

                // Validate business rules
                if (!await _itemService.ValidateItemAsync(item))
                {
                    TempData["ErrorMessage"] = "Item validation failed. Please check your data.";
                    return View(item);
                }

                var createdItem = await _itemService.CreateItemAsync(item);

                TempData["SuccessMessage"] = $"Item '{createdItem.ItemCode} - {createdItem.Name}' created successfully.";
                return RedirectToAction(nameof(Details), new { id = createdItem.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item");
                TempData["ErrorMessage"] = "Error creating item. Please try again.";
                return View(item);
            }
        }

        // GET: Item/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var item = await _itemService.GetItemByIdAsync(id);
                if (item == null)
                {
                    TempData["ErrorMessage"] = "Item not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading item for edit, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading item for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Item/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Item item)
        {
            try
            {
                if (id != item.Id)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    return View(item);
                }

                // Validate item code uniqueness (excluding current item)
                if (!await _itemService.IsItemCodeUniqueAsync(item.ItemCode, id))
                {
                    ModelState.AddModelError("ItemCode", "Item code already exists. Please use a different code.");
                    return View(item);
                }

                // Validate business rules
                if (!await _itemService.ValidateItemAsync(item))
                {
                    TempData["ErrorMessage"] = "Item validation failed. Please check your data.";
                    return View(item);
                }

                var updatedItem = await _itemService.UpdateItemAsync(id, item);

                TempData["SuccessMessage"] = $"Item '{updatedItem.ItemCode} - {updatedItem.Name}' updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error updating item. Please try again.";
                return View(item);
            }
        }

        // GET: Item/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var item = await _itemService.GetItemByIdAsync(id);
                if (item == null)
                {
                    TempData["ErrorMessage"] = "Item not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if item can be deleted
                if (!await _itemService.CanDeleteItemAsync(id))
                {
                    TempData["ErrorMessage"] = "This item cannot be deleted because it has associated transactions or inventory.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Show stock information
                ViewBag.TotalStock = await _itemService.GetItemTotalStockAsync(id);
                ViewBag.InventoryDetails = await _itemService.GetItemInventoryDetailsAsync(id);

                return View(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading item for delete, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading item.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Item/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Double-check if item can be deleted
                if (!await _itemService.CanDeleteItemAsync(id))
                {
                    TempData["ErrorMessage"] = "This item cannot be deleted because it has associated transactions or inventory.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var success = await _itemService.DeleteItemAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Item deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Could not delete item.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error deleting item. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Item/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var item = await _itemService.GetItemByIdAsync(id);
                if (item == null)
                {
                    TempData["ErrorMessage"] = "Item not found.";
                    return RedirectToAction(nameof(Index));
                }

                var success = await _itemService.UpdateItemStatusAsync(id, !item.IsActive);

                if (success)
                {
                    var status = !item.IsActive ? "activated" : "deactivated";
                    TempData["SuccessMessage"] = $"Item '{item.ItemCode}' {status} successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update item status.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling item status, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error updating item status.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Item/CheckItemCode
        [HttpGet]
        public async Task<JsonResult> CheckItemCode(string itemCode, int? excludeId = null)
        {
            try
            {
                var isUnique = await _itemService.IsItemCodeUniqueAsync(itemCode, excludeId);

                return Json(new
                {
                    isUnique = isUnique,
                    message = isUnique ? "Item code is available" : "Item code already exists"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking item code uniqueness: {ItemCode}", itemCode);
                return Json(new { isUnique = false, message = "Error checking item code" });
            }
        }

        // GET: Item/GetItemsBySearch
        [HttpGet]
        public async Task<JsonResult> GetItemsBySearch(string searchTerm)
        {
            try
            {
                var items = await _itemService.SearchItemsAsync(searchTerm);

                return Json(new
                {
                    success = true,
                    items = items.Select(i => new
                    {
                        id = i.Id,
                        itemCode = i.ItemCode,
                        name = i.Name,
                        unit = i.Unit,
                        standardPrice = i.StandardPrice,
                        isActive = i.IsActive,
                        displayName = i.DisplayName
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching items: {SearchTerm}", searchTerm);
                return Json(new { success = false, message = "Error searching items" });
            }
        }

        // GET: Item/GetItemDetails
        [HttpGet]
        public async Task<JsonResult> GetItemDetails(int id)
        {
            try
            {
                var item = await _itemService.GetItemByIdAsync(id);
                if (item == null)
                {
                    return Json(new { success = false, message = "Item not found" });
                }

                var totalStock = await _itemService.GetItemTotalStockAsync(id);
                var averageCost = await _itemService.GetItemAverageCostAsync(id);
                var lastCost = await _itemService.GetItemLastCostAsync(id);

                return Json(new
                {
                    success = true,
                    id = item.Id,
                    itemCode = item.ItemCode,
                    name = item.Name,
                    description = item.Description,
                    unit = item.Unit,
                    standardPrice = item.StandardPrice,
                    isActive = item.IsActive,
                    totalStock = totalStock,
                    averageCost = averageCost,
                    lastCost = lastCost,
                    displayName = item.DisplayName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item details for ID: {Id}", id);
                return Json(new { success = false, message = "Error getting item details" });
            }
        }

        // GET: Item/LowStockReport
        public async Task<IActionResult> LowStockReport(int threshold = Constants.LOW_STOCK_THRESHOLD)
        {
            try
            {
                var lowStockItems = await _itemService.GetItemsWithLowStockAsync(threshold);
                var statistics = await _itemService.GetItemStatisticsAsync();

                ViewBag.Threshold = threshold;
                ViewBag.Statistics = statistics;

                return View(lowStockItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading low stock report");
                TempData["ErrorMessage"] = "Error loading low stock report.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Item/UsageReport
        public async Task<IActionResult> UsageReport(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                // Default to last 30 days if no dates provided
                fromDate ??= DateTime.Now.AddDays(-30);
                toDate ??= DateTime.Now;

                var usageReport = await _itemService.GetItemUsageReportAsync(fromDate, toDate);

                ViewBag.FromDate = fromDate;
                ViewBag.ToDate = toDate;

                return View(usageReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading item usage report");
                TempData["ErrorMessage"] = "Error loading usage report.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Item/PerformanceReport
        public async Task<IActionResult> PerformanceReport()
        {
            try
            {
                var performanceReport = await _itemService.GetItemPerformanceReportAsync();
                var topSellingItems = await _itemService.GetTopSellingItemsAsync();
                var slowMovingItems = await _itemService.GetSlowMovingItemsAsync();

                ViewBag.TopSellingItems = topSellingItems;
                ViewBag.SlowMovingItems = slowMovingItems;

                return View(performanceReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading item performance report");
                TempData["ErrorMessage"] = "Error loading performance report.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Item/PriceVarianceReport
        public async Task<IActionResult> PriceVarianceReport()
        {
            try
            {
                var priceVarianceReport = await _itemService.GetItemPriceVarianceReportAsync();

                return View(priceVarianceReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading price variance report");
                TempData["ErrorMessage"] = "Error loading price variance report.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Item/SyncWithInventory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncWithInventory(int id)
        {
            try
            {
                var success = await _itemService.SyncItemWithInventoryAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Item synchronized with inventory successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to synchronize item with inventory.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing item with inventory, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error synchronizing item with inventory.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Item/RestockSuggestions
        public async Task<IActionResult> RestockSuggestions()
        {
            try
            {
                var itemsNeedingRestock = await _itemService.GetItemsNeedingRestockAsync();

                return View(itemsNeedingRestock);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading restock suggestions");
                TempData["ErrorMessage"] = "Error loading restock suggestions.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Item/SupplierInfo/5
        public async Task<JsonResult> SupplierInfo(int id)
        {
            try
            {
                var supplierInfo = await _itemService.GetItemSupplierInfoAsync(id);

                return Json(new
                {
                    success = true,
                    data = supplierInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supplier info for item ID: {Id}", id);
                return Json(new { success = false, message = "Error getting supplier information" });
            }
        }

        // GET: Item/Export
        public async Task<IActionResult> Export()
        {
            try
            {
                var items = await _itemService.GetAllItemsAsync();
                var stockSummary = await _itemService.GetItemStockSummaryAsync();

                // Here you would implement Excel export logic
                // For now, returning the view for demonstration
                ViewBag.StockSummary = stockSummary;
                return View(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting items");
                TempData["ErrorMessage"] = "Error exporting items.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}