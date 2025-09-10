using Microsoft.AspNetCore.Mvc;
using WMS.Models;
using WMS.Data.Repositories;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Attributes;

namespace WMS.Controllers
{
    [RequireCompany]
    public class ItemController : Controller
    {
        private readonly IItemService _itemService;
        private readonly ILogger<ItemController> _logger;

        public ItemController(
            IItemService itemService,
            ILogger<ItemController> logger)
        {
            _itemService = itemService;
            _logger = logger;
        }

        // GET: Item
        public async Task<IActionResult> Index(ItemIndexViewModel? model)
        {
            try
            {
                model = await _itemService.GetItemIndexViewModelAsync(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading items");
                TempData["ErrorMessage"] = "Error loading items. Please try again.";
                return View(new ItemIndexViewModel());
            }
        }

        // GET: Item/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var viewModel = await _itemService.GetItemDetailsViewModelAsync(id);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading item details for ID {ItemId}", id);
                TempData["ErrorMessage"] = "Error loading item details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Item/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = await _itemService.GetItemViewModelAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create form");
                TempData["ErrorMessage"] = "Error loading create form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Item/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var item = new Item
                    {
                        ItemCode = model.ItemCode,
                        Name = model.Name,
                        Description = model.Description,
                        Unit = model.Unit,
                        StandardPrice = model.StandardPrice,
                        SupplierId = model.SupplierId,
                        IsActive = model.IsActive
                    };

                    await _itemService.CreateItemAsync(item);
                    TempData["SuccessMessage"] = "Item berhasil ditambahkan.";
                    return RedirectToAction(nameof(Index));
                }

                model = await _itemService.PopulateItemViewModelAsync(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item");
                TempData["ErrorMessage"] = "Error creating item. Please try again.";
                model = await _itemService.PopulateItemViewModelAsync(model);
                return View(model);
            }
        }

        // GET: Item/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var viewModel = await _itemService.GetItemViewModelAsync(id);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for item ID {ItemId}", id);
                TempData["ErrorMessage"] = "Error loading edit form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Item/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ItemViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    var item = new Item
                    {
                        Id = model.Id,
                        ItemCode = model.ItemCode,
                        Name = model.Name,
                        Description = model.Description,
                        Unit = model.Unit,
                        StandardPrice = model.StandardPrice,
                        SupplierId = model.SupplierId,
                        IsActive = model.IsActive
                    };

                    await _itemService.UpdateItemAsync(id, item);
                    TempData["SuccessMessage"] = "Item berhasil diperbarui.";
                    return RedirectToAction(nameof(Index));
                }

                model = await _itemService.PopulateItemViewModelAsync(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item ID {ItemId}", id);
                TempData["ErrorMessage"] = "Error updating item. Please try again.";
                model = await _itemService.PopulateItemViewModelAsync(model);
                return View(model);
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
                    return NotFound();
                }

                var viewModel = new ItemViewModel
                {
                    Id = item.Id,
                    ItemCode = item.ItemCode,
                    Name = item.Name,
                    Description = item.Description,
                    Unit = item.Unit,
                    StandardPrice = item.StandardPrice,
                    SupplierId = item.SupplierId ?? 0,
                    IsActive = item.IsActive,
                    SupplierName = item.Supplier?.Name ?? "Unknown"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete form for item ID {ItemId}", id);
                TempData["ErrorMessage"] = "Error loading delete form. Please try again.";
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
                var result = await _itemService.DeleteItemAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Item berhasil dihapus.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Item tidak dapat dihapus karena masih digunakan dalam transaksi atau memiliki stok.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item ID {ItemId}", id);
                TempData["ErrorMessage"] = "Error deleting item. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // AJAX: Check if item code is unique
        [HttpPost]
        public async Task<IActionResult> CheckItemCodeUnique(string itemCode, int? id)
        {
            try
            {
                var isUnique = await _itemService.IsItemCodeUniqueAsync(itemCode, id);
                return Json(new { isUnique });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking item code uniqueness");
                return Json(new { isUnique = false });
            }
        }

        // AJAX: Search items
        [HttpGet]
        public async Task<IActionResult> SearchItems(string term)
        {
            try
            {
                var items = await _itemService.SearchItemsAsync(term);
                var searchResults = items
                    .Take(10)
                    .Select(i => new
                    {
                        id = i.Id,
                        text = $"{i.ItemCode} - {i.Name}",
                        itemCode = i.ItemCode,
                        name = i.Name,
                        unit = i.Unit,
                        price = i.StandardPrice
                    })
                    .ToList();

                return Json(searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching items");
                return Json(new List<object>());
            }
        }

        // AJAX: Get item details for dropdown
        [HttpGet]
        public async Task<IActionResult> GetItemDetails(int itemId)
        {
            try
            {
                var item = await _itemService.GetItemByIdAsync(itemId);
                if (item == null)
                {
                    return Json(new { success = false, message = "Item not found" });
                }

                var totalStock = await _itemService.GetItemTotalStockAsync(itemId);
                var totalValue = await _itemService.GetItemTotalValueAsync(itemId);

                return Json(new
                {
                    success = true,
                    itemCode = item.ItemCode,
                    name = item.Name,
                    unit = item.Unit,
                    standardPrice = item.StandardPrice,
                    totalStock = totalStock,
                    totalValue = totalValue,
                    isActive = item.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item details for ID {ItemId}", itemId);
                return Json(new { success = false, message = "Error retrieving item details" });
            }
        }

        // AJAX: Get items by supplier
        [HttpGet]
        public async Task<IActionResult> GetItemsBySupplier(int supplierId)
        {
            try
            {
                var items = await _itemService.GetItemsBySupplierAsync(supplierId);
                var itemList = items
                    .Where(i => i.IsActive)
                    .Select(i => new
                    {
                        id = i.Id,
                        text = $"{i.ItemCode} - {i.Name}",
                        itemCode = i.ItemCode,
                        name = i.Name,
                        unit = i.Unit,
                        price = i.StandardPrice
                    })
                    .ToList();

                return Json(itemList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items by supplier {SupplierId}", supplierId);
                return Json(new List<object>());
            }
        }

        // AJAX: Update item status
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, bool isActive)
        {
            try
            {
                var result = await _itemService.UpdateItemStatusAsync(id, isActive);
                if (result)
                {
                    return Json(new { success = true, message = "Item status updated successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Item not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item status for ID {ItemId}", id);
                return Json(new { success = false, message = "Error updating item status" });
            }
        }

        // AJAX: Get item statistics
        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var stats = await _itemService.GetItemStatisticsAsync();
                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item statistics");
                return Json(new { error = "Error retrieving statistics" });
            }
        }

        // AJAX: Get low stock items
        [HttpGet]
        public async Task<IActionResult> GetLowStockItems(int threshold = 10)
        {
            try
            {
                var items = await _itemService.GetItemsWithLowStockAsync(threshold);
                var lowStockList = items.Select(i => new
                {
                    id = i.Id,
                    itemCode = i.ItemCode,
                    name = i.Name,
                    unit = i.Unit,
                    totalStock = i.Inventories?.Sum(inv => inv.Quantity) ?? 0,
                    threshold = threshold
                }).ToList();

                return Json(lowStockList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock items");
                return Json(new List<object>());
            }
        }

        // AJAX: Get item performance report
        [HttpGet]
        public async Task<IActionResult> GetPerformanceReport()
        {
            try
            {
                var report = await _itemService.GetItemPerformanceReportAsync();
                return Json(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item performance report");
                return Json(new { error = "Error retrieving performance report" });
            }
        }

        // AJAX: Get slow moving items
        [HttpGet]
        public async Task<IActionResult> GetSlowMovingItems(int daysThreshold = 90)
        {
            try
            {
                var items = await _itemService.GetSlowMovingItemsAsync(daysThreshold);
                return Json(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slow moving items");
                return Json(new List<object>());
            }
        }

        // AJAX: Get price variance report
        [HttpGet]
        public async Task<IActionResult> GetPriceVarianceReport()
        {
            try
            {
                var report = await _itemService.GetItemPriceVarianceReportAsync();
                return Json(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting price variance report");
                return Json(new { error = "Error retrieving price variance report" });
            }
        }

        // AJAX: Get item supplier info
        [HttpGet]
        public async Task<IActionResult> GetSupplierInfo(int itemId)
        {
            try
            {
                var info = await _itemService.GetItemSupplierInfoAsync(itemId);
                return Json(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supplier info for item ID {ItemId}", itemId);
                return Json(new { error = "Error retrieving supplier information" });
            }
        }

        // AJAX: Sync item with inventory
        [HttpPost]
        public async Task<IActionResult> SyncWithInventory(int itemId)
        {
            try
            {
                var result = await _itemService.SyncItemWithInventoryAsync(itemId);
                if (result)
                {
                    return Json(new { success = true, message = "Item synced with inventory successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Item not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing item with inventory for ID {ItemId}", itemId);
                return Json(new { success = false, message = "Error syncing item with inventory" });
            }
        }
    }
}