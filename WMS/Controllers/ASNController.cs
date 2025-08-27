using Microsoft.AspNetCore.Mvc;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Controllers
{
    public class ASNController : Controller
    {
        private readonly IASNService _asnService;
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly ILogger<ASNController> _logger;

        public ASNController(
            IASNService asnService,
            IPurchaseOrderService purchaseOrderService,
            ILogger<ASNController> logger)
        {
            _asnService = asnService;
            _purchaseOrderService = purchaseOrderService;
            _logger = logger;
        }

        // GET: ASN
        public async Task<IActionResult> Index(string? status = null)
        {
            try
            {
                IEnumerable<WMS.Models.AdvancedShippingNotice> asns;

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<ASNStatus>(status, true, out var statusEnum))
                {
                    asns = await _asnService.GetASNsByStatusAsync(statusEnum);
                }
                else
                {
                    asns = await _asnService.GetAllASNsAsync();
                }

                ViewBag.CurrentStatus = status;
                return View(asns.OrderByDescending(asn => asn.CreatedDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ASNs");
                TempData["ErrorMessage"] = "Error loading ASNs. Please try again.";
                return View(new List<WMS.Models.AdvancedShippingNotice>());
            }
        }

        // GET: ASN/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var asn = await _asnService.GetASNByIdAsync(id);
                if (asn == null)
                {
                    TempData["ErrorMessage"] = "ASN not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Get warehouse fee statistics for this ASN
                ViewBag.WarehouseFeeStats = await _asnService.GetPriceVarianceAnalysisAsync(id);

                return View(asn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ASN details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading ASN details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: ASN/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = await _asnService.GetASNViewModelAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing create ASN form");
                TempData["ErrorMessage"] = "Error preparing form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ASN/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ASNViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    viewModel = await _asnService.PopulateASNViewModelAsync(viewModel);
                    return View(viewModel);
                }

                // Validate business rules
                if (!await _asnService.ValidateASNAsync(viewModel))
                {
                    TempData["ErrorMessage"] = "ASN validation failed. Please check your data.";
                    viewModel = await _asnService.PopulateASNViewModelAsync(viewModel);
                    return View(viewModel);
                }

                // Validate against Purchase Order
                if (!await _asnService.ValidateASNAgainstPOAsync(viewModel))
                {
                    TempData["ErrorMessage"] = "ASN data doesn't match the Purchase Order. Please verify quantities and items.";
                    viewModel = await _asnService.PopulateASNViewModelAsync(viewModel);
                    return View(viewModel);
                }

                var asn = await _asnService.CreateASNAsync(viewModel);

                TempData["SuccessMessage"] = $"ASN {asn.ASNNumber} created successfully. Total warehouse fee: {asn.TotalWarehouseFee:C}";
                return RedirectToAction(nameof(Details), new { id = asn.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ASN");
                TempData["ErrorMessage"] = "Error creating ASN. Please try again.";

                viewModel = await _asnService.PopulateASNViewModelAsync(viewModel);
                return View(viewModel);
            }
        }

        // GET: ASN/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (!await _asnService.CanEditASNAsync(id))
                {
                    TempData["ErrorMessage"] = "This ASN cannot be edited.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var viewModel = await _asnService.GetASNViewModelAsync(id);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "ASN not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ASN for edit, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading ASN for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ASN/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ASNViewModel viewModel)
        {
            try
            {
                if (id != viewModel.Id)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    viewModel = await _asnService.PopulateASNViewModelAsync(viewModel);
                    return View(viewModel);
                }

                if (!await _asnService.CanEditASNAsync(id))
                {
                    TempData["ErrorMessage"] = "This ASN cannot be edited.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var updatedASN = await _asnService.UpdateASNAsync(id, viewModel);

                TempData["SuccessMessage"] = $"ASN {updatedASN.ASNNumber} updated successfully. Total warehouse fee: {updatedASN.TotalWarehouseFee:C}";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ASN, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error updating ASN. Please try again.";

                viewModel = await _asnService.PopulateASNViewModelAsync(viewModel);
                return View(viewModel);
            }
        }

        // GET: ASN/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var asn = await _asnService.GetASNByIdAsync(id);
                if (asn == null)
                {
                    TempData["ErrorMessage"] = "ASN not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (!asn.CanBeEdited)
                {
                    TempData["ErrorMessage"] = "This ASN cannot be deleted.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(asn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ASN for delete, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading ASN.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ASN/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var success = await _asnService.DeleteASNAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "ASN deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Could not delete ASN.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ASN, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error deleting ASN. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ASN/MarkAsArrived/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsArrived(int id)
        {
            try
            {
                // Check if ASN exists and can be marked as arrived
                var asn = await _asnService.GetASNByIdAsync(id);
                if (asn == null)
                {
                    TempData["ErrorMessage"] = "ASN not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if ASN is in correct status
                if (asn.Status != "In Transit")
                {
                    TempData["ErrorMessage"] = "ASN can only be marked as arrived when it's in transit.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var success = await _asnService.MarkAsArrivedAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "ASN marked as arrived successfully. Ready for processing.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to mark ASN as arrived. Please try again.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking ASN as arrived, ID: {Id}", id);
                TempData["ErrorMessage"] = $"Error updating ASN status: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: ASN/Process/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(int id)
        {
            try
            {
                // Check if ASN exists
                var asn = await _asnService.GetASNByIdAsync(id);
                if (asn == null)
                {
                    TempData["ErrorMessage"] = "ASN not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if ASN can be processed
                if (!await _asnService.CanProcessASNAsync(id))
                {
                    TempData["ErrorMessage"] = "This ASN cannot be processed. Please ensure it has arrived first.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var success = await _asnService.ProcessASNAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "ASN processed successfully. Inventory has been updated.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to process ASN. Please try again.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ASN, ID: {Id}", id);
                TempData["ErrorMessage"] = $"Error processing ASN: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: ASN/GetPODetails
        [HttpGet]
        public async Task<JsonResult> GetPODetails(int purchaseOrderId)
        {
            try
            {
                var po = await _purchaseOrderService.GetPurchaseOrderByIdAsync(purchaseOrderId);

                if (po == null)
                {
                    return Json(new { success = false, message = "Purchase Order not found." });
                }

                // Map PO details to the format expected by JavaScript
                var details = po.PurchaseOrderDetails.Select(d => new
                {
                    itemId = d.ItemId,
                    itemCode = d.Item?.ItemCode ?? "",
                    itemName = d.Item?.Name ?? "",
                    itemUnit = d.Item?.Unit ?? "",
                    orderedQuantity = d.Quantity,
                    orderedPrice = d.UnitPrice
                }).ToList();

                return Json(new
                {
                    success = true,
                    poNumber = po.PONumber,
                    supplierName = po.Supplier?.Name ?? "",
                    details = details
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PO details for PO ID: {PurchaseOrderId}", purchaseOrderId);
                return Json(new { success = false, message = "Error loading purchase order details: " + ex.Message });
            }
        }

        // GET: ASN/CalculateWarehouseFee - FIXED VERSION
        [HttpGet]
        public async Task<JsonResult> CalculateWarehouseFee(decimal actualPrice)
        {
            try
            {
                decimal feeRate;
                string tier;
                string tierClass;

                // FIXED: Calculate fee rate based on actual price sesuai requirement baru
                if (actualPrice <= 1000000m)
                {
                    feeRate = 0.03m; // FIXED: 3% (was 0.05m)
                    tier = "Low (≤ 1M)";
                    tierClass = "badge bg-success";
                }
                else if (actualPrice <= 10000000m)
                {
                    feeRate = 0.02m; // FIXED: 2% (was 0.03m)
                    tier = "Medium (1M-10M)";
                    tierClass = "badge bg-warning";
                }
                else
                {
                    feeRate = 0.01m; // 1% (unchanged)
                    tier = "High (> 10M)";
                    tierClass = "badge bg-danger";
                }

                var feeAmount = actualPrice * feeRate;

                return Json(new
                {
                    success = true,
                    feeRate = feeRate,
                    feeAmount = feeAmount,
                    feePercentage = $"{feeRate * 100:0.##}%",
                    tier = tier,
                    tierClass = tierClass
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating warehouse fee for price: {ActualPrice}", actualPrice);
                return Json(new { success = false, message = "Error calculating warehouse fee: " + ex.Message });
            }
        }

        // GET: ASN/WarehouseFeeReport
        public async Task<IActionResult> WarehouseFeeReport()
        {
            try
            {
                ViewBag.Statistics = await _asnService.GetWarehouseFeeStatisticsAsync();
                ViewBag.HighFeeItems = await _asnService.GetHighWarehouseFeeItemsAsync();

                var asns = await _asnService.GetAllASNsAsync();
                return View(asns.Where(asn => asn.Status == "Processed").OrderByDescending(asn => asn.TotalWarehouseFee));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading warehouse fee report");
                TempData["ErrorMessage"] = "Error loading warehouse fee report.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}