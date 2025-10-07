using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Attributes;
using WMS.Data;
using WMS.Data.Repositories;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller untuk Picking operations
    /// Mengelola proses pengambilan barang dari warehouse untuk Sales Order
    /// </summary>
    [RequirePermission("PICKING_MANAGE")]
    public class PickingController : Controller
    {
        private readonly IPickingService _pickingService;
        private readonly ISalesOrderService _salesOrderService;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PickingController> _logger;

        public PickingController(
            IPickingService pickingService,
            ISalesOrderService salesOrderService,
            IInventoryRepository inventoryRepository,
            ApplicationDbContext context,
            ILogger<PickingController> logger)
        {
            _pickingService = pickingService;
            _salesOrderService = salesOrderService;
            _inventoryRepository = inventoryRepository;
            _context = context;
            _logger = logger;
        }

        #region Index & List

        /// <summary>
        /// GET: Picking
        /// Display list of all picking documents
        /// </summary>
        public async Task<IActionResult> Index(string? status = null)
        {
            try
            {
                IEnumerable<PickingListViewModel> pickings;

                if (!string.IsNullOrEmpty(status))
                {
                    var pickingsByStatus = await _pickingService.GetPickingsByStatusAsync(status);
                    pickings = pickingsByStatus.Select(p => new PickingListViewModel
                    {
                        Id = p.Id,
                        PickingNumber = p.PickingNumber,
                        SONumber = p.SONumber,
                        CustomerName = p.CustomerName,
                        PickingDate = p.PickingDate,
                        Status = p.Status,
                        StatusIndonesia = p.StatusIndonesia,
                        StatusCssClass = p.StatusCssClass,
                        CompletionPercentage = p.CompletionPercentage,
                        TotalQuantityRequired = p.TotalQuantityRequired,
                        TotalQuantityPicked = p.TotalQuantityPicked,
                        TotalItemTypes = p.TotalItemTypes
                    });
                }
                else
                {
                    pickings = await _pickingService.GetPickingListSummaryAsync();
                }

                ViewBag.CurrentStatus = status;
                return View(pickings.OrderByDescending(p => p.PickingDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading picking list");
                TempData["ErrorMessage"] = "Error loading picking list. Please try again.";
                return View(new List<PickingListViewModel>());
            }
        }

        #endregion

        #region Details

        /// <summary>
        /// GET: Picking/Details/5
        /// Display picking document details
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var viewModel = await _pickingService.GetPickingViewModelAsync(id);
                if (viewModel == null || viewModel.Id == 0)
                {
                    TempData["ErrorMessage"] = "Picking document not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading picking details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading picking details.";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Generate Picking

        /// <summary>
        /// GET: Picking/Generate
        /// Show available Sales Orders for picking
        /// </summary>
        public async Task<IActionResult> Generate()
        {
            try
            {
                // Get confirmed sales orders that don't have picking yet
                var confirmedSalesOrders = await _salesOrderService.GetConfirmedSalesOrdersAsync();
                var salesOrdersWithoutPicking = new List<SalesOrder>();

                foreach (var so in confirmedSalesOrders)
                {
                    if (!await _pickingService.CanGeneratePickingAsync(so.Id))
                        continue;
                    
                    salesOrdersWithoutPicking.Add(so);
                }

                ViewBag.AvailableSalesOrders = salesOrdersWithoutPicking;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available sales orders for picking");
                TempData["ErrorMessage"] = "Error loading available sales orders.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: Picking/Generate?salesOrderId=5
        /// Generate picking list from Sales Order
        /// </summary>
        public async Task<IActionResult> GenerateFromSalesOrder(int salesOrderId)
        {
            try
            {
                if (!await _pickingService.CanGeneratePickingAsync(salesOrderId))
                {
                    TempData["ErrorMessage"] = "Cannot generate picking list for this Sales Order. Check SO status or existing picking.";
                    return RedirectToAction("Details", "SalesOrder", new { id = salesOrderId });
                }

                var picking = await _pickingService.GeneratePickingListAsync(salesOrderId);

                TempData["SuccessMessage"] = $"Picking list {picking.PickingNumber} generated successfully!";
                return RedirectToAction(nameof(ProcessPicking), new { id = picking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating picking list for SO: {SalesOrderId}", salesOrderId);
                TempData["ErrorMessage"] = $"Error generating picking list: {ex.Message}";
                return RedirectToAction("Details", "SalesOrder", new { id = salesOrderId });
            }
        }

        #endregion

        #region Process Picking

        /// <summary>
        /// GET: Picking/ProcessPicking/5
        /// Display form to process picking
        /// </summary>
        public async Task<IActionResult> ProcessPicking(int id)
        {
            try
            {
                _logger.LogInformation("Loading ProcessPicking form for Picking {PickingId}", id);

                var viewModel = await _pickingService.GetPickingViewModelAsync(id);

                if (viewModel == null || viewModel.Id == 0)
                {
                    TempData["ErrorMessage"] = "Picking document not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (!viewModel.CanBeEdited)
                {
                    TempData["ErrorMessage"] = "This picking document cannot be edited.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                _logger.LogInformation("Successfully loaded ProcessPicking form for Picking {PickingNumber} with {ItemCount} items",
                    viewModel.PickingNumber, viewModel.PickingDetails?.Count ?? 0);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading process picking form for Picking {PickingId}", id);
                TempData["ErrorMessage"] = "Error loading process picking form.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: Picking/ProcessPicking
        /// Process single picking detail
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPicking(int pickingDetailId, int quantityToPick, int pickingId, int itemId, int locationId)
        {
            try
            {
                _logger.LogInformation("ProcessPicking POST - Received parameters: pickingDetailId={PickingDetailId}, quantityToPick={QuantityToPick}, locationId={LocationId}, pickingId={PickingId}, itemId={ItemId}",
                    pickingDetailId, quantityToPick, locationId, pickingId, itemId);

                // Validate parameters
                if (pickingDetailId <= 0 || quantityToPick <= 0 || locationId <= 0 || itemId <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid parameters for picking process.";
                    return RedirectToAction(nameof(ProcessPicking), new { id = pickingId });
                }

                // Get complete picking detail from database to ensure we have all required data
                var pickingDetail = await _context.PickingDetails
                    .Include(pd => pd.Item)
                    .Include(pd => pd.Location)
                    .FirstOrDefaultAsync(pd => pd.Id == pickingDetailId);
                    
                if (pickingDetail == null)
                {
                    TempData["ErrorMessage"] = "Picking detail not found.";
                    return RedirectToAction(nameof(ProcessPicking), new { id = pickingId });
                }

                // Create complete detail with all required data
                var detail = new PickingDetailViewModel
                {
                    Id = pickingDetail.Id,
                    PickingId = pickingDetail.PickingId,
                    SalesOrderDetailId = pickingDetail.SalesOrderDetailId,
                    ItemId = pickingDetail.ItemId,
                    ItemCode = pickingDetail.ItemCode,
                    ItemName = pickingDetail.ItemName,
                    ItemUnit = pickingDetail.ItemUnit,
                    LocationId = pickingDetail.LocationId,
                    LocationCode = pickingDetail.LocationCode,
                    LocationName = pickingDetail.LocationName,
                    QuantityRequired = pickingDetail.QuantityRequired,
                    QuantityPicked = pickingDetail.QuantityPicked,
                    RemainingQuantity = pickingDetail.RemainingQuantity,
                    QuantityToPick = quantityToPick,
                    Status = pickingDetail.Status,
                    StatusIndonesia = pickingDetail.StatusIndonesia,
                    Notes = pickingDetail.Notes
                };

                _logger.LogInformation("Complete picking detail - Required: {Required}, Picked: {Picked}, Remaining: {Remaining}, ToPick: {ToPick}",
                    detail.QuantityRequired, detail.QuantityPicked, detail.RemainingQuantity, detail.QuantityToPick);

                var result = await _pickingService.ProcessPickingAsync(detail);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Successfully picked {quantityToPick} units from location.";
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage; // Detailed error message
                }

                return RedirectToAction(nameof(ProcessPicking), new { id = pickingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing picking for PickingDetail {PickingDetailId}", pickingDetailId);
                TempData["ErrorMessage"] = $"Error processing picking: {ex.Message}";
                return RedirectToAction(nameof(ProcessPicking), new { id = pickingId });
            }
        }

        #endregion

        #region Complete Picking

        /// <summary>
        /// POST: Picking/Complete/5
        /// Complete picking process
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            try
            {
                if (!await _pickingService.CanCompletePickingAsync(id))
                {
                    TempData["ErrorMessage"] = "Cannot complete picking. No items have been picked yet.";
                    return RedirectToAction(nameof(ProcessPicking), new { id });
                }

                var success = await _pickingService.CompletePickingAsync(id);

                if (success)
                {
                    var picking = await _pickingService.GetPickingByIdAsync(id);
                    TempData["SuccessMessage"] = $"Picking {picking?.PickingNumber} completed successfully!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to complete picking.";
                    return RedirectToAction(nameof(ProcessPicking), new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing picking {PickingId}", id);
                TempData["ErrorMessage"] = $"Error completing picking: {ex.Message}";
                return RedirectToAction(nameof(ProcessPicking), new { id });
            }
        }

        #endregion

        #region Cancel Picking

        /// <summary>
        /// POST: Picking/Cancel/5
        /// Cancel picking and restore inventory
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var picking = await _pickingService.GetPickingByIdAsync(id);
                if (picking == null)
                {
                    TempData["ErrorMessage"] = "Picking document not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (!picking.CanBeCancelled)
                {
                    TempData["ErrorMessage"] = "This picking document cannot be cancelled.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var success = await _pickingService.CancelPickingAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = $"Picking {picking.PickingNumber} cancelled and inventory restored.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to cancel picking.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling picking {PickingId}", id);
                TempData["ErrorMessage"] = $"Error cancelling picking: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// GET: Picking/Delete/5
        /// Display delete confirmation
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var viewModel = await _pickingService.GetPickingViewModelAsync(id);
                if (viewModel == null || viewModel.Id == 0)
                {
                    TempData["ErrorMessage"] = "Picking document not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete confirmation for Picking {PickingId}", id);
                TempData["ErrorMessage"] = "Error loading delete confirmation.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: Picking/Delete/5
        /// Delete picking document
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var picking = await _pickingService.GetPickingByIdAsync(id);
                if (picking == null)
                {
                    TempData["ErrorMessage"] = "Picking document not found.";
                    return RedirectToAction(nameof(Index));
                }

                var success = await _pickingService.DeletePickingAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = $"Picking {picking.PickingNumber} deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete picking. Completed pickings cannot be deleted.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting picking {PickingId}", id);
                TempData["ErrorMessage"] = $"Error deleting picking: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Debug & Validation Endpoints

        /// <summary>
        /// GET: Picking/CheckInventory?itemId=1&locationId=2
        /// Debug endpoint to check inventory availability
        /// </summary>
        public async Task<IActionResult> CheckInventory(int itemId, int locationId)
        {
            try
            {
                var inventory = await _inventoryRepository.GetByItemAndLocationAsync(itemId, locationId);
                
                if (inventory == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No inventory found",
                        data = new
                        {
                            itemId,
                            locationId,
                            inventory = (object)null
                        }
                    });
                }

                return Json(new
                {
                    success = true,
                    message = "Inventory found",
                    data = new
                    {
                        itemId,
                        locationId,
                        inventory = new
                        {
                            id = inventory.Id,
                            quantity = inventory.Quantity,
                            status = inventory.Status,
                            lastUpdated = inventory.LastUpdated,
                            itemCode = inventory.Item?.ItemCode,
                            itemName = inventory.Item?.Name,
                            locationCode = inventory.Location?.Code,
                            locationName = inventory.Location?.Name
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking inventory for Item {ItemId} at Location {LocationId}", itemId, locationId);
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}",
                    data = new { itemId, locationId }
                });
            }
        }

        /// <summary>
        /// GET: Picking/CheckItemInventory?itemId=1
        /// Debug endpoint to check all inventory for an item
        /// </summary>
        public async Task<IActionResult> CheckItemInventory(int itemId)
        {
            try
            {
                var inventories = await _inventoryRepository.GetByItemIdAsync(itemId);
                var availableInventories = inventories
                    .Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE && inv.Quantity > 0)
                    .OrderBy(inv => inv.LastUpdated)
                    .ToList();

                return Json(new
                {
                    success = true,
                    message = $"Found {availableInventories.Count} available inventories",
                    data = new
                    {
                        itemId,
                        totalInventories = inventories.Count(),
                        availableInventories = availableInventories.Select(inv => new
                        {
                            id = inv.Id,
                            locationId = inv.LocationId,
                            locationCode = inv.Location?.Code,
                            locationName = inv.Location?.Name,
                            quantity = inv.Quantity,
                            status = inv.Status,
                            lastUpdated = inv.LastUpdated
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking item inventory for Item {ItemId}", itemId);
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}",
                    data = new { itemId }
                });
            }
        }

        #endregion

        #region AJAX Endpoints

        /// <summary>
        /// GET: Picking/GetLocationSuggestions?itemId=5&quantity=100
        /// Get FIFO location suggestions for picking
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLocationSuggestions(int itemId, int quantity)
        {
            try
            {
                var suggestions = await _pickingService.GetPickingSuggestionsAsync(itemId, quantity);
                return Json(new { success = true, data = suggestions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location suggestions for Item {ItemId}", itemId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion
    }
}
