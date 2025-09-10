using Microsoft.AspNetCore.Mvc;
using WMS.Data.Repositories;
using WMS.Models;
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
        private readonly IASNRepository _asnRepository;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ILocationRepository _locationRepository;

        public ASNController(
            IASNService asnService,
            IPurchaseOrderService purchaseOrderService,
            IASNRepository asnRepository,
            IInventoryRepository inventoryRepository,
            ILocationRepository locationRepository,
            ILogger<ASNController> logger)
        {
            _asnService = asnService;
            _purchaseOrderService = purchaseOrderService;
            _asnRepository = asnRepository;
            _inventoryRepository = inventoryRepository;
            _locationRepository = locationRepository;
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

                // Use new method with automatic date setting
                var success = await _asnService.MarkAsArrivedWithActualDateAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = $"ASN marked as arrived successfully at {DateTime.Now:dd/MM/yyyy HH:mm}. Ready for processing.";
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

        // POST: ASN/MarkAsArrivedWithDate/5 - Allow manual date entry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsArrivedWithDate(int id, DateTime? customArrivalDate)
        {
            try
            {
                var asn = await _asnService.GetASNByIdAsync(id);
                if (asn == null)
                {
                    return Json(new { success = false, message = "ASN not found." });
                }

                if (asn.Status != "In Transit")
                {
                    return Json(new { success = false, message = "ASN can only be marked as arrived when it's in transit." });
                }

                var success = await _asnService.MarkAsArrivedWithActualDateAsync(id, customArrivalDate);

                if (success)
                {
                    var arrivalDate = customArrivalDate ?? DateTime.Now;
                    return Json(new
                    {
                        success = true,
                        message = $"ASN marked as arrived at {arrivalDate:dd/MM/yyyy HH:mm}.",
                        actualArrivalDate = arrivalDate.ToString("dd/MM/yyyy HH:mm")
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to mark ASN as arrived." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking ASN as arrived with custom date, ID: {Id}", id);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
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

        // GET: ASN/GetSuggestedLocations
        [HttpGet]
        public async Task<JsonResult> GetSuggestedLocations(int itemId)
        {
            try
            {
                var suggestedLocations = await _locationRepository.GetSuggestedPutawayLocationsAsync(itemId);

                var locations = suggestedLocations.Select(l => new
                {
                    locationId = l.Id,
                    locationCode = l.Code,
                    locationName = l.Name,
                    availableCapacity = l.AvailableCapacity,
                    displayText = l.DisplayName
                });

                return Json(locations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting suggested locations for item ID: {ItemId}", itemId);
                return Json(new { error = "Failed to load suggested locations" });
            }
        }

        // POST: ASN/ProcessASNPutAway - Fixed version
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ProcessASNPutAway(int asnDetailId, int itemId, int locationId, int quantity, decimal costPrice, string? notes = null)
        {
            try
            {
                // 1. Validate ASN Detail exists and can be put away
                var asnDetail = await _asnRepository.GetASNDetailByIdAsync(asnDetailId);
                if (asnDetail == null)
                {
                    return Json(new { success = false, message = "ASN Detail tidak ditemukan" });
                }

                // 2. Check if ASN is in correct status (should be Processed)
                var asn = await _asnService.GetASNByIdAsync(asnDetail.ASNId);
                if (asn == null || asn.Status != "Processed")
                {
                    return Json(new { success = false, message = "ASN belum dalam status 'Processed'" });
                }

                // 3. Calculate remaining quantity that can be put away
                var alreadyPutAway = await _asnRepository.GetPutAwayQuantityByASNDetailAsync(asnDetailId);
                var remainingQuantity = asnDetail.ShippedQuantity - alreadyPutAway;

                if (quantity > remainingQuantity)
                {
                    return Json(new { success = false, message = $"Quantity melebihi sisa yang bisa di-putaway. Sisa: {remainingQuantity}" });
                }

                // 4. Validate location exists and has capacity
                var location = await _locationRepository.GetByIdAsync(locationId);
                if (location == null)
                {
                    return Json(new { success = false, message = "Lokasi tidak ditemukan" });
                }

                if (location.AvailableCapacity < quantity)
                {
                    return Json(new { success = false, message = $"Kapasitas lokasi tidak mencukupi. Tersedia: {location.AvailableCapacity}" });
                }

                // 5. Create inventory record
                var existingInventory = await _inventoryRepository.GetByItemAndLocationAsync(itemId, locationId);
                if (existingInventory != null)
                {
                    // Update existing inventory
                    existingInventory.Quantity += quantity;
                    existingInventory.LastCostPrice = costPrice;
                    existingInventory.SourceReference = $"ASN-{asn.ASNNumber}-{asnDetailId}";
                    if (!string.IsNullOrEmpty(notes))
                    {
                        existingInventory.Notes = notes;
                    }
                    existingInventory.ModifiedDate = DateTime.Now;
                    await _inventoryRepository.UpdateAsync(existingInventory);
                }
                else
                {
                    // Create new inventory record
                    var inventory = new Inventory
                    {
                        ItemId = itemId,
                        LocationId = locationId,
                        Quantity = quantity,
                        LastCostPrice = costPrice,
                        Status = "Available",
                        SourceReference = $"ASN-{asn.ASNNumber}-{asnDetailId}",
                        Notes = notes,
                        CreatedDate = DateTime.Now
                    };

                    await _inventoryRepository.AddAsync(inventory);
                }

                // 6. Update location capacity
                await _locationRepository.AddCapacityAsync(locationId, quantity);

                // 7. Log the putaway transaction
                _logger.LogInformation("Putaway processed: ASN Detail {ASNDetailId}, Item {ItemId}, Location {LocationId}, Qty {Quantity}",
                    asnDetailId, itemId, locationId, quantity);

                return Json(new
                {
                    success = true,
                    message = $"Berhasil putaway {quantity} unit ke lokasi {location.Code}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing putaway for ASN Detail ID: {AsnDetailId}", asnDetailId);
                return Json(new { success = false, message = "Error processing putaway: " + ex.Message });
            }
        }

        // POST: ASN/ProcessMultiplePutAway - Fixed version
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ProcessMultiplePutAway(int[] asnDetailIds)
        {
            try
            {
                if (asnDetailIds == null || asnDetailIds.Length == 0)
                {
                    return Json(new { success = false, message = "Tidak ada item yang dipilih" });
                }

                var processedCount = 0;
                var errorMessages = new List<string>();

                foreach (var asnDetailId in asnDetailIds)
                {
                    try
                    {
                        // 1. Get ASN Detail
                        var asnDetail = await _asnRepository.GetASNDetailByIdAsync(asnDetailId);
                        if (asnDetail == null)
                        {
                            errorMessages.Add($"ASN Detail {asnDetailId} tidak ditemukan");
                            continue;
                        }

                        // 2. Check remaining quantity
                        var alreadyPutAway = await _asnRepository.GetPutAwayQuantityByASNDetailAsync(asnDetailId);
                        var remainingQuantity = asnDetail.ShippedQuantity - alreadyPutAway;

                        if (remainingQuantity <= 0)
                        {
                            continue; // Skip already completed items
                        }

                        // 3. Get suggested location (first available location with enough capacity)
                        var suggestedLocations = await _locationRepository.GetSuggestedPutawayLocationsAsync(asnDetail.ItemId);
                        var targetLocation = suggestedLocations.FirstOrDefault(l => l.AvailableCapacity >= remainingQuantity);

                        if (targetLocation == null)
                        {
                            errorMessages.Add($"Tidak ada lokasi dengan kapasitas cukup untuk item {asnDetail.Item.ItemCode}");
                            continue;
                        }

                        // 4. Create/Update inventory
                        var existingInventory = await _inventoryRepository.GetByItemAndLocationAsync(asnDetail.ItemId, targetLocation.Id);
                        if (existingInventory != null)
                        {
                            existingInventory.Quantity += remainingQuantity;
                            existingInventory.LastCostPrice = asnDetail.ActualPricePerItem;
                            existingInventory.SourceReference = $"ASN-{asnDetail.ASN.ASNNumber}-{asnDetailId}";
                            existingInventory.Notes = "Auto putaway (bulk process)";
                            existingInventory.ModifiedDate = DateTime.Now;
                            await _inventoryRepository.UpdateAsync(existingInventory);
                        }
                        else
                        {
                            var inventory = new Inventory
                            {
                                ItemId = asnDetail.ItemId,
                                LocationId = targetLocation.Id,
                                Quantity = remainingQuantity,
                                LastCostPrice = asnDetail.ActualPricePerItem,
                                Status = "Available",
                                SourceReference = $"ASN-{asnDetail.ASN.ASNNumber}-{asnDetailId}",
                                Notes = "Auto putaway (bulk process)",
                                CreatedDate = DateTime.Now
                            };

                            await _inventoryRepository.AddAsync(inventory);
                        }

                        // 5. Update location capacity
                        await _locationRepository.AddCapacityAsync(targetLocation.Id, remainingQuantity);

                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing putaway for ASN Detail {ASNDetailId}", asnDetailId);
                        errorMessages.Add($"Error processing ASN Detail {asnDetailId}: {ex.Message}");
                    }
                }

                if (processedCount > 0)
                {
                    var message = $"Berhasil memproses putaway untuk {processedCount} item";
                    if (errorMessages.Any())
                    {
                        message += $". {errorMessages.Count} item gagal diproses.";
                    }

                    return Json(new { success = true, message = message });
                }
                else
                {
                    var message = "Tidak ada item yang berhasil diproses";
                    if (errorMessages.Any())
                    {
                        message += ": " + string.Join("; ", errorMessages);
                    }

                    return Json(new { success = false, message = message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing multiple putaway for ASN Details: {AsnDetailIds}", string.Join(",", asnDetailIds));
                return Json(new { success = false, message = "Error processing multiple putaway: " + ex.Message });
            }
        }

        // GET: ASN/GetASNDetailsForPutAway - Fixed version
        [HttpGet]
        public async Task<JsonResult> GetASNDetailsForPutAway(int asnId)
        {
            try
            {
                var asn = await _asnService.GetASNByIdAsync(asnId);
                if (asn == null)
                {
                    return Json(new { success = false, message = "ASN not found" });
                }

                // Get ASN details with putaway calculations
                var asnDetailsWithPutaway = new List<object>();

                foreach (var detail in asn.ASNDetails.Where(d => d.ShippedQuantity > 0))
                {
                    // Calculate how much has already been put away
                    var alreadyPutAway = await _asnRepository.GetPutAwayQuantityByASNDetailAsync(detail.Id);
                    var remainingQuantity = detail.ShippedQuantity - alreadyPutAway;

                    asnDetailsWithPutaway.Add(new
                    {
                        asnDetailId = detail.Id,
                        itemId = detail.ItemId,
                        itemDisplay = $"{detail.Item.ItemCode} - {detail.Item.Name}",
                        unit = detail.Item.Unit,
                        shippedQuantity = detail.ShippedQuantity,
                        alreadyPutAwayQuantity = alreadyPutAway,
                        remainingQuantity = remainingQuantity,
                        actualPricePerItem = detail.ActualPricePerItem
                    });
                }

                var asnInfo = new
                {
                    asnNumber = asn.ASNNumber,
                    poNumber = asn.PurchaseOrder?.PONumber ?? "",
                    supplierName = asn.PurchaseOrder?.Supplier?.Name ?? "",
                    actualArrivalDate = asn.ActualArrivalDate,
                    totalItems = asnDetailsWithPutaway.Count,
                    statusIndonesia = asn.StatusIndonesia,
                    statusCssClass = asn.StatusCssClass
                };

                return Json(new
                {
                    success = true,
                    asnInfo = asnInfo,
                    asnDetails = asnDetailsWithPutaway
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ASN details for putaway, ASN ID: {AsnId}", asnId);
                return Json(new { success = false, message = "Error loading ASN details: " + ex.Message });
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

        // GET: ASN/CalculateWarehouseFee - Fixed version
        [HttpGet]
        public async Task<JsonResult> CalculateWarehouseFee(decimal actualPrice)
        {
            try
            {
                decimal feeRate;
                string tier;
                string tierClass;

                // Calculate fee rate based on actual price
                if (actualPrice <= 1000000m)
                {
                    feeRate = 0.03m; // 3%
                    tier = "Low (≤ 1M)";
                    tierClass = "badge bg-success";
                }
                else if (actualPrice <= 10000000m)
                {
                    feeRate = 0.02m; // 2%
                    tier = "Medium (1M-10M)";
                    tierClass = "badge bg-warning";
                }
                else
                {
                    feeRate = 0.01m; // 1%
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
                var arrivedASNs = asns.Where(asn => asn.ActualArrivalDate.HasValue)
                                   .OrderByDescending(asn => asn.ActualArrivalDate);

                return View(arrivedASNs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading arrival performance report");
                TempData["ErrorMessage"] = "Error loading arrival performance report.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
