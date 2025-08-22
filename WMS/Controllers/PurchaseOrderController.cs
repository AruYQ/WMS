using Microsoft.AspNetCore.Mvc;
using WMS.Data.Repositories;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Controllers
{
    public class PurchaseOrderController : Controller
    {
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly IEmailService _emailService;
        private readonly ILogger<PurchaseOrderController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IItemRepository _itemRepository;

        public PurchaseOrderController(
            IPurchaseOrderService purchaseOrderService,
            IEmailService emailService,
            ILogger<PurchaseOrderController> logger,
            IConfiguration configuration,
            IItemRepository itemRepository)


        {
            _purchaseOrderService = purchaseOrderService;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
            _itemRepository = itemRepository;
        }

        // GET: PurchaseOrder
        public async Task<IActionResult> Index(string? status = null)
        {
            try
            {
                IEnumerable<WMS.Models.PurchaseOrder> purchaseOrders;

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<PurchaseOrderStatus>(status, true, out var statusEnum))
                {
                    purchaseOrders = await _purchaseOrderService.GetPurchaseOrdersByStatusAsync(statusEnum);
                }
                else
                {
                    purchaseOrders = await _purchaseOrderService.GetAllPurchaseOrdersAsync();
                }

                ViewBag.CurrentStatus = status;
                return View(purchaseOrders.OrderByDescending(po => po.CreatedDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading purchase orders");
                TempData["ErrorMessage"] = "Error loading purchase orders. Please try again.";
                return View(new List<WMS.Models.PurchaseOrder>());
            }
        }

        // GET: PurchaseOrder/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var purchaseOrder = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
                if (purchaseOrder == null)
                {
                    TempData["ErrorMessage"] = "Purchase Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(purchaseOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading purchase order details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading purchase order details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: PurchaseOrder/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = await _purchaseOrderService.GetPurchaseOrderViewModelAsync();

                // Get items with their details for JavaScript
                var items = await _itemRepository.GetActiveItemsAsync();
                var itemsData = items.ToDictionary(
                    item => item.Id.ToString(),
                    item => new {
                        unit = item.Unit,
                        standardPrice = item.StandardPrice,
                        name = item.Name,
                        code = item.ItemCode
                    }
                );

                ViewBag.ItemsData = itemsData;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing create purchase order form");
                TempData["ErrorMessage"] = "Error preparing form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: PurchaseOrder/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    viewModel = await _purchaseOrderService.PopulatePurchaseOrderViewModelAsync(viewModel);
                    return View(viewModel);
                }

                // Validate business rules
                if (!await _purchaseOrderService.ValidatePurchaseOrderAsync(viewModel))
                {
                    TempData["ErrorMessage"] = "Purchase Order validation failed. Please check your data.";
                    viewModel = await _purchaseOrderService.PopulatePurchaseOrderViewModelAsync(viewModel);
                    return View(viewModel);
                }

                var purchaseOrder = await _purchaseOrderService.CreatePurchaseOrderAsync(viewModel);

                TempData["SuccessMessage"] = $"Purchase Order {purchaseOrder.PONumber} created successfully.";
                return RedirectToAction(nameof(Details), new { id = purchaseOrder.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating purchase order");
                TempData["ErrorMessage"] = "Error creating purchase order. Please try again.";

                viewModel = await _purchaseOrderService.PopulatePurchaseOrderViewModelAsync(viewModel);
                return View(viewModel);
            }
        }

        // GET: PurchaseOrder/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (!await _purchaseOrderService.CanEditPurchaseOrderAsync(id))
                {
                    TempData["ErrorMessage"] = "This Purchase Order cannot be edited.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var viewModel = await _purchaseOrderService.GetPurchaseOrderViewModelAsync(id);
                if (viewModel == null)
                {
                    TempData["ErrorMessage"] = "Purchase Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading purchase order for edit, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading purchase order for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: PurchaseOrder/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PurchaseOrderViewModel viewModel)
        {
            try
            {
                if (id != viewModel.Id)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    viewModel = await _purchaseOrderService.PopulatePurchaseOrderViewModelAsync(viewModel);
                    return View(viewModel);
                }

                if (!await _purchaseOrderService.CanEditPurchaseOrderAsync(id))
                {
                    TempData["ErrorMessage"] = "This Purchase Order cannot be edited.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var updatedPO = await _purchaseOrderService.UpdatePurchaseOrderAsync(id, viewModel);

                TempData["SuccessMessage"] = $"Purchase Order {updatedPO.PONumber} updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating purchase order, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error updating purchase order. Please try again.";

                viewModel = await _purchaseOrderService.PopulatePurchaseOrderViewModelAsync(viewModel);
                return View(viewModel);
            }
        }

        // GET: PurchaseOrder/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var purchaseOrder = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
                if (purchaseOrder == null)
                {
                    TempData["ErrorMessage"] = "Purchase Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (!purchaseOrder.CanBeEdited)
                {
                    TempData["ErrorMessage"] = "This Purchase Order cannot be deleted.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(purchaseOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading purchase order for delete, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading purchase order.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: PurchaseOrder/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var success = await _purchaseOrderService.DeletePurchaseOrderAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Purchase Order deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Could not delete Purchase Order.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting purchase order, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error deleting purchase order. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: PurchaseOrder/Send/5
        public async Task<IActionResult> Send(int id)
        {
            try
            {
                var purchaseOrder = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
                if (purchaseOrder == null)
                {
                    TempData["ErrorMessage"] = "Purchase Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (!await _purchaseOrderService.CanSendPurchaseOrderAsync(id))
                {
                    TempData["ErrorMessage"] = "This Purchase Order cannot be sent.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                ViewBag.EmailContent = await _purchaseOrderService.GeneratePurchaseOrderEmailContentAsync(purchaseOrder);
                return View(purchaseOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing to send purchase order, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error preparing to send purchase order.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: PurchaseOrder/Send/5
        // REPLACE your SendConfirmed action in PurchaseOrderController with this
        // POST: PurchaseOrder/Send/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendConfirmed(int id)
        {
            try
            {
                _logger.LogInformation("Starting email send process for PO ID: {Id}", id);

                // Get PO details
                var purchaseOrder = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
                if (purchaseOrder == null)
                {
                    _logger.LogWarning("PO {Id} not found", id);
                    TempData["ErrorMessage"] = "Purchase Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate PO can be sent
                if (!await _purchaseOrderService.CanSendPurchaseOrderAsync(id))
                {
                    _logger.LogWarning("PO {Id} cannot be sent (status: {Status})", id, purchaseOrder.Status);
                    TempData["ErrorMessage"] = "This Purchase Order cannot be sent in its current status.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate supplier email
                if (string.IsNullOrEmpty(purchaseOrder.Supplier?.Email))
                {
                    _logger.LogWarning("PO {Id} has no supplier email", id);
                    TempData["ErrorMessage"] = "Cannot send email: Supplier email address is missing.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate email format
                if (!await _emailService.ValidateEmailAddressAsync(purchaseOrder.Supplier.Email))
                {
                    _logger.LogWarning("PO {Id} has invalid supplier email: {Email}", id, purchaseOrder.Supplier.Email);
                    TempData["ErrorMessage"] = "Cannot send email: Supplier email address is invalid.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Sending PO {PONumber} to {Email}", purchaseOrder.PONumber, purchaseOrder.Supplier.Email);

                // Generate email content
                var emailContent = await _purchaseOrderService.GeneratePurchaseOrderEmailContentAsync(purchaseOrder);
                var subject = $"Purchase Order {purchaseOrder.PONumber} from {_configuration["WMSSettings:CompanyName"] ?? "PT. Vera Co."}";

                // Send email
                var emailSent = await _emailService.SendEmailAsync(
                    purchaseOrder.Supplier.Email,
                    subject,
                    emailContent
                );

                if (emailSent)
                {
                    // Update email tracking
                    await _purchaseOrderService.MarkEmailAsSentAsync(id);

                    // Update status to Sent
                    await _purchaseOrderService.UpdateStatusAsync(id, PurchaseOrderStatus.Sent);

                    _logger.LogInformation("Successfully sent PO {PONumber} to {Email}", purchaseOrder.PONumber, purchaseOrder.Supplier.Email);

                    TempData["SuccessMessage"] = $"Purchase Order {purchaseOrder.PONumber} has been sent successfully to {purchaseOrder.Supplier.Email}.";

                    // Redirect to Index page after successful send
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogError("Failed to send email for PO {Id} to {Email}", id, purchaseOrder.Supplier.Email);
                    TempData["ErrorMessage"] = "Failed to send email. Please check your email configuration and try again.";

                    // Redirect to Index page even on failure
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending PO email for ID: {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while sending the email. Please try again.";

                // Always redirect to Index to prevent infinite loading
                return RedirectToAction(nameof(Index));
            }
        }
        [HttpGet]
        public async Task<IActionResult> DebugEmail(int id)
        {
            try
            {
                var purchaseOrder = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
                if (purchaseOrder == null)
                {
                    return Json(new { success = false, message = "PO not found" });
                }

                // Test 1: Check email service configuration
                var emailStatus = await _emailService.GetEmailServiceStatusAsync();

                // Test 2: Validate supplier email
                var supplierEmail = purchaseOrder.Supplier?.Email ?? "";
                var isEmailValid = await _emailService.ValidateEmailAddressAsync(supplierEmail);

                // Test 3: Try sending a simple test email
                var testEmailResult = false;
                var testError = "";

                try
                {
                    testEmailResult = await _emailService.SendEmailAsync(
                        "verayq4@gmail.com", // Send to yourself for testing
                        "Test Email from WMS",
                        "This is a test email. If you receive this, email configuration is working."
                    );
                }
                catch (Exception ex)
                {
                    testError = ex.Message;
                }

                return Json(new
                {
                    success = true,
                    poNumber = purchaseOrder.PONumber,
                    supplierName = purchaseOrder.Supplier?.Name,
                    supplierEmail = supplierEmail,
                    isEmailValid = isEmailValid,
                    emailServiceStatus = emailStatus,
                    testEmailSent = testEmailResult,
                    testEmailError = testError,
                    message = "Debug information retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DebugEmail for PO {Id}", id);
                return Json(new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // POST: PurchaseOrder/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                if (!await _purchaseOrderService.CanCancelPurchaseOrderAsync(id))
                {
                    TempData["ErrorMessage"] = "This Purchase Order cannot be cancelled.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var success = await _purchaseOrderService.CancelPurchaseOrderAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Purchase Order cancelled successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to cancel Purchase Order.";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling purchase order, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error cancelling purchase order.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}