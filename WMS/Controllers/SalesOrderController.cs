using Microsoft.AspNetCore.Mvc;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Controllers
{
    public class SalesOrderController : Controller
    {
        private readonly ISalesOrderService _salesOrderService;
        private readonly IEmailService _emailService;
        private readonly ILogger<SalesOrderController> _logger;

        public SalesOrderController(
            ISalesOrderService salesOrderService,
            IEmailService emailService,
            ILogger<SalesOrderController> logger)
        {
            _salesOrderService = salesOrderService;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: SalesOrder
        public async Task<IActionResult> Index(string? status = null)
        {
            try
            {
                IEnumerable<WMS.Models.SalesOrder> salesOrders;

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<SalesOrderStatus>(status, true, out var statusEnum))
                {
                    salesOrders = await _salesOrderService.GetSalesOrdersByStatusAsync(statusEnum);
                }
                else
                {
                    salesOrders = await _salesOrderService.GetAllSalesOrdersAsync();
                }

                ViewBag.CurrentStatus = status;
                return View(salesOrders.OrderByDescending(so => so.CreatedDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales orders");
                TempData["ErrorMessage"] = "Error loading sales orders. Please try again.";
                return View(new List<WMS.Models.SalesOrder>());
            }
        }

        // GET: SalesOrder/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var salesOrder = await _salesOrderService.GetSalesOrderByIdAsync(id);
                if (salesOrder == null)
                {
                    TempData["ErrorMessage"] = "Sales Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(salesOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales order details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading sales order details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: SalesOrder/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = await _salesOrderService.GetSalesOrderViewModelAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing create sales order form");
                TempData["ErrorMessage"] = "Error preparing form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SalesOrder/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalesOrderViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    viewModel = await _salesOrderService.PopulateSalesOrderViewModelAsync(viewModel);
                    return View(viewModel);
                }

                // Validate stock availability
                viewModel = await _salesOrderService.ValidateAndPopulateStockInfoAsync(viewModel);
                if (viewModel.StockWarnings.Any())
                {
                    foreach (var warning in viewModel.StockWarnings)
                    {
                        ModelState.AddModelError("", warning);
                    }
                    viewModel = await _salesOrderService.PopulateSalesOrderViewModelAsync(viewModel);
                    return View(viewModel);
                }

                // Validate business rules
                if (!await _salesOrderService.ValidateSalesOrderAsync(viewModel))
                {
                    TempData["ErrorMessage"] = "Sales Order validation failed. Please check your data.";
                    viewModel = await _salesOrderService.PopulateSalesOrderViewModelAsync(viewModel);
                    return View(viewModel);
                }

                var salesOrder = await _salesOrderService.CreateSalesOrderAsync(viewModel);

                TempData["SuccessMessage"] = $"Sales Order {salesOrder.SONumber} created successfully.";
                return RedirectToAction(nameof(Details), new { id = salesOrder.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sales order");
                TempData["ErrorMessage"] = "Error creating sales order. Please try again.";

                viewModel = await _salesOrderService.PopulateSalesOrderViewModelAsync(viewModel);
                return View(viewModel);
            }
        }

        // GET: SalesOrder/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (!await _salesOrderService.CanEditSalesOrderAsync(id))
                {
                    TempData["ErrorMessage"] = "This Sales Order cannot be edited.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var viewModel = await _salesOrderService.GetSalesOrderViewModelAsync(id);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Sales Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales order for edit, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading sales order for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SalesOrder/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SalesOrderViewModel viewModel)
        {
            try
            {
                if (id != viewModel.Id)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    viewModel = await _salesOrderService.PopulateSalesOrderViewModelAsync(viewModel);
                    return View(viewModel);
                }

                if (!await _salesOrderService.CanEditSalesOrderAsync(id))
                {
                    TempData["ErrorMessage"] = "This Sales Order cannot be edited.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Validate stock availability for updated order
                viewModel = await _salesOrderService.ValidateAndPopulateStockInfoAsync(viewModel);
                if (viewModel.StockWarnings.Any())
                {
                    foreach (var warning in viewModel.StockWarnings)
                    {
                        ModelState.AddModelError("", warning);
                    }
                    viewModel = await _salesOrderService.PopulateSalesOrderViewModelAsync(viewModel);
                    return View(viewModel);
                }

                var updatedSO = await _salesOrderService.UpdateSalesOrderAsync(id, viewModel);

                TempData["SuccessMessage"] = $"Sales Order {updatedSO.SONumber} updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sales order, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error updating sales order. Please try again.";

                viewModel = await _salesOrderService.PopulateSalesOrderViewModelAsync(viewModel);
                return View(viewModel);
            }
        }

        // GET: SalesOrder/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var salesOrder = await _salesOrderService.GetSalesOrderByIdAsync(id);
                if (salesOrder == null)
                {
                    TempData["ErrorMessage"] = "Sales Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (!salesOrder.CanBeEdited)
                {
                    TempData["ErrorMessage"] = "This Sales Order cannot be deleted.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(salesOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales order for delete, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading sales order.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SalesOrder/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var success = await _salesOrderService.DeleteSalesOrderAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Sales Order deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Could not delete Sales Order.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting sales order, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error deleting sales order. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SalesOrder/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            try
            {
                if (!await _salesOrderService.CanConfirmSalesOrderAsync(id))
                {
                    TempData["ErrorMessage"] = "This Sales Order cannot be confirmed.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var success = await _salesOrderService.ConfirmSalesOrderAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Sales Order confirmed successfully. Stock has been reserved.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to confirm Sales Order. Please check stock availability.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming sales order, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error confirming sales order.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: SalesOrder/Ship/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ship(int id)
        {
            try
            {
                if (!await _salesOrderService.CanShipSalesOrderAsync(id))
                {
                    TempData["ErrorMessage"] = "This Sales Order cannot be shipped.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var success = await _salesOrderService.ShipSalesOrderAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Sales Order shipped successfully. Stock has been reduced.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to ship Sales Order.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shipping sales order, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error shipping sales order.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: SalesOrder/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            try
            {
                if (!await _salesOrderService.CanCompleteSalesOrderAsync(id))
                {
                    TempData["ErrorMessage"] = "This Sales Order cannot be completed.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var success = await _salesOrderService.CompleteSalesOrderAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Sales Order completed successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to complete Sales Order.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing sales order, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error completing sales order.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: SalesOrder/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                if (!await _salesOrderService.CanCancelSalesOrderAsync(id))
                {
                    TempData["ErrorMessage"] = "This Sales Order cannot be cancelled.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var success = await _salesOrderService.CancelSalesOrderAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Sales Order cancelled successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to cancel Sales Order.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling sales order, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error cancelling sales order.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // AJAX: Check stock availability for order validation
        [HttpPost]
        public async Task<IActionResult> CheckStockAvailability([FromBody] List<SalesOrderDetailViewModel> details)
        {
            try
            {
                var stockCheck = await _salesOrderService.CheckItemStockAsync(details);

                var warnings = new List<string>();
                foreach (var detail in details)
                {
                    if (stockCheck.TryGetValue(detail.ItemId, out var availableStock))
                    {
                        if (availableStock < detail.Quantity)
                        {
                            warnings.Add($"{detail.ItemCode}: Stock tidak mencukupi. Tersedia: {availableStock}, Diminta: {detail.Quantity}");
                        }
                    }
                }

                return Json(new { success = warnings.Count == 0, warnings });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking stock availability");
                return Json(new { success = false, warnings = new[] { "Error checking stock availability" } });
            }
        }

        // AJAX: Get warehouse fee for item
        [HttpGet]
        public async Task<IActionResult> GetWarehouseFee(int itemId)
        {
            try
            {
                var warehouseFee = await _salesOrderService.GetWarehouseFeeForItemAsync(itemId);
                return Json(new { success = true, warehouseFee });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting warehouse fee for item ID: {ItemId}", itemId);
                return Json(new { success = false, warehouseFee = 0 });
            }
        }

        // GET: Sales Order Summary Report
        public async Task<IActionResult> Summary()
        {
            try
            {
                var statistics = await _salesOrderService.GetSalesStatisticsAsync();
                return View(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales order summary");
                TempData["ErrorMessage"] = "Error loading summary. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Warehouse Fee Revenue Report
        public async Task<IActionResult> WarehouseFeeRevenue(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var revenue = await _salesOrderService.GetWarehouseFeeRevenueAsync(fromDate, toDate);
                ViewBag.FromDate = fromDate;
                ViewBag.ToDate = toDate;
                return View(revenue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading warehouse fee revenue report");
                TempData["ErrorMessage"] = "Error loading revenue report. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}