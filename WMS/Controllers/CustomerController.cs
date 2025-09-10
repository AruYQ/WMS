using Microsoft.AspNetCore.Mvc;
using WMS.Models;
using WMS.Data.Repositories;
using WMS.Attributes;
using WMS.Services;
using WMS.Models.ViewModels;
using System.Security.Claims;

namespace WMS.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomerController> _logger;
        private readonly ICurrentUserService _currentUserService;

        public CustomerController(
            ICustomerService customerService,
            ILogger<CustomerController> logger,
            ICurrentUserService currentUserService)
        {
            _customerService = customerService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        // GET: Customer
        public async Task<IActionResult> Index(string? searchTerm = null, bool? isActive = null)
        {
            try
            {
                _logger.LogInformation("=== CUSTOMER INDEX DEBUG START ===");
                _logger.LogInformation("Customer Index - Starting with SearchTerm: {SearchTerm}, IsActive: {IsActive}", searchTerm, isActive);

                // Debug CompanyId dengan detail
                var companyId = _currentUserService.CompanyId;
                var userId = _currentUserService.UserId;
                var username = _currentUserService.Username;
                var isAuthenticated = _currentUserService.IsAuthenticated;
                
                _logger.LogInformation("Current User Debug - CompanyId: {CompanyId}, UserId: {UserId}, Username: {Username}, IsAuthenticated: {IsAuthenticated}", 
                    companyId, userId, username, isAuthenticated);

                // Debug specific claims
                var companyIdClaim = _currentUserService.GetClaimValue("CompanyId");
                var userIdClaim = _currentUserService.GetClaimValue("UserId");
                var usernameClaim = _currentUserService.GetClaimValue(ClaimTypes.Name);
                var emailClaim = _currentUserService.GetClaimValue(ClaimTypes.Email);
                
                _logger.LogInformation("Claims Debug - CompanyId: {CompanyIdClaim}, UserId: {UserIdClaim}, Username: {UsernameClaim}, Email: {EmailClaim}", 
                    companyIdClaim, userIdClaim, usernameClaim, emailClaim);

                if (!companyId.HasValue)
                {
                    _logger.LogWarning("No CompanyId found for current user - returning empty view");
                    TempData["ErrorMessage"] = "Company ID tidak ditemukan. Silakan login ulang.";
                    return View(new CustomerIndexViewModel());
                }

                _logger.LogInformation("Calling CustomerService.GetCustomerIndexViewModelAsync...");
                var viewModel = await _customerService.GetCustomerIndexViewModelAsync(searchTerm, isActive);
                
                _logger.LogInformation("CustomerService returned - TotalCustomers: {TotalCustomers}, CustomersCount: {CustomersCount}", 
                    viewModel.TotalCustomers, viewModel.Customers?.Count() ?? 0);
                
                _logger.LogInformation("=== CUSTOMER INDEX DEBUG END ===");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers - Exception: {Message}", ex.Message);
                _logger.LogError(ex, "Stack Trace: {StackTrace}", ex.StackTrace);
                TempData["ErrorMessage"] = "Error loading customers. Please try again.";
                return View(new CustomerIndexViewModel());
            }
        }

        // GET: Customer/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var viewModel = await _customerService.GetCustomerDetailsViewModelAsync(id);
                if (viewModel.Id == 0)
                {
                    return NotFound();
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer details: {CustomerId}", id);
                TempData["ErrorMessage"] = "Error loading customer details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Customer/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = new CustomerViewModel();
                viewModel = await _customerService.PopulateCustomerViewModelAsync(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create customer form");
                TempData["ErrorMessage"] = "Error loading create customer form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Check if email already exists
                    if (await _customerService.ExistsByEmailAsync(viewModel.Email))
                    {
                        ModelState.AddModelError("Email", "A customer with this email already exists in your company. Please use a different email.");
                        viewModel = await _customerService.PopulateCustomerViewModelAsync(viewModel);
                        return View(viewModel);
                    }

                    var customer = new Customer
                    {
                        Name = viewModel.Name,
                        Email = viewModel.Email,
                        Phone = viewModel.Phone,
                        Address = viewModel.Address,
                        IsActive = viewModel.IsActive
                    };

                    await _customerService.CreateAsync(customer);
                    TempData["SuccessMessage"] = "Customer created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                viewModel = await _customerService.PopulateCustomerViewModelAsync(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                TempData["ErrorMessage"] = "Error creating customer. Please try again.";
                viewModel = await _customerService.PopulateCustomerViewModelAsync(viewModel);
                return View(viewModel);
            }
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var viewModel = await _customerService.GetCustomerViewModelAsync(id);
                if (viewModel.Id == 0)
                {
                    return NotFound();
                }

                viewModel = await _customerService.PopulateCustomerViewModelAsync(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer for edit: {Id}", id);
                TempData["ErrorMessage"] = "Error loading customer for edit.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerViewModel viewModel)
        {
            try
            {
                if (id != viewModel.Id)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    viewModel = await _customerService.PopulateCustomerViewModelAsync(viewModel);
                    return View(viewModel);
                }

                // Check if email is being changed and if new email already exists
                if (await _customerService.ExistsByEmailAsync(viewModel.Email, id))
                {
                    ModelState.AddModelError("Email", "A customer with this email already exists in your company. Please use a different email.");
                    viewModel = await _customerService.PopulateCustomerViewModelAsync(viewModel);
                    return View(viewModel);
                }

                var customer = new Customer
                {
                    Id = viewModel.Id,
                    Name = viewModel.Name,
                    Email = viewModel.Email,
                    Phone = viewModel.Phone,
                    Address = viewModel.Address,
                    IsActive = viewModel.IsActive,
                    ModifiedDate = DateTime.Now,
                    ModifiedBy = User.Identity?.Name ?? "System"
                };

                await _customerService.UpdateAsync(customer);

                TempData["SuccessMessage"] = "Customer updated successfully.";
                return RedirectToAction(nameof(Details), new { id = customer.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error updating customer. Please try again.";
                viewModel = await _customerService.PopulateCustomerViewModelAsync(viewModel);
                return View(viewModel);
            }
        }

        // GET: Customer/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var viewModel = await _customerService.GetCustomerDetailsViewModelAsync(id);
                if (viewModel.Id == 0)
                {
                    return NotFound();
                }

                // Check if customer can be deleted
                if (viewModel.SalesOrders.Any())
                {
                    TempData["ErrorMessage"] = "This customer cannot be deleted because it has associated sales orders.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer for delete, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading customer.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Customer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var customer = await _customerService.GetByIdAsync(id);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Double-check if customer can be deleted
                var customerWithOrders = await _customerService.GetCustomersWithSalesOrdersAsync();
                if (customerWithOrders.Any(c => c.Id == id && c.SalesOrders.Any()))
                {
                    TempData["ErrorMessage"] = "This customer cannot be deleted because it has associated sales orders.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _customerService.DeleteAsync(id);
                TempData["SuccessMessage"] = "Customer deleted successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error deleting customer. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Customer/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var customer = await _customerService.GetByIdAsync(id);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }

                customer.IsActive = !customer.IsActive;
                customer.ModifiedDate = DateTime.Now;
                customer.ModifiedBy = User.Identity?.Name ?? "System";

                await _customerService.UpdateAsync(customer);

                var status = customer.IsActive ? "activated" : "deactivated";
                TempData["SuccessMessage"] = $"Customer '{customer.Name}' {status} successfully.";

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling customer status, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error updating customer status.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Customer/CheckEmail
        [HttpGet]
        public async Task<JsonResult> CheckEmail(string email, int? excludeId = null)
        {
            try
            {
                var exists = await _customerService.CheckEmailExistsAsync(email, excludeId);

                return Json(new
                {
                    isUnique = !exists,
                    message = exists ? "Email already exists" : "Email is available"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email uniqueness: {Email}", email);
                return Json(new { isUnique = false, message = "Error checking email" });
            }
        }

        // GET: Customer/GetCustomersBySearch
        [HttpGet]
        public async Task<JsonResult> GetCustomersBySearch(string searchTerm)
        {
            try
            {
                var customers = await _customerService.SearchCustomersForAjaxAsync(searchTerm);

                return Json(new
                {
                    success = true,
                    customers = customers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customers: {SearchTerm}", searchTerm);
                return Json(new { success = false, message = "Error searching customers" });
            }
        }

        // GET: Customer/GetCustomerDetails
        [HttpGet]
        public async Task<JsonResult> GetCustomerDetails(int id)
        {
            try
            {
                var customer = await _customerService.GetCustomerDetailsForAjaxAsync(id);
                return Json(new
                {
                    success = true,
                    customer = customer
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer details for ID: {Id}", id);
                return Json(new { success = false, message = "Error getting customer details" });
            }
        }

        // GET: Customer/PerformanceReport
        public async Task<IActionResult> PerformanceReport()
        {
            try
            {
                var viewModel = await _customerService.GetPerformanceReportViewModelAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer performance report");
                TempData["ErrorMessage"] = "Error loading performance report.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Customer/Export
        public async Task<IActionResult> Export()
        {
            try
            {
                var customers = await _customerService.GetCustomersForExportAsync();

                // Here you would implement Excel export logic
                // For now, returning the view for demonstration
                return View(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting customers");
                TempData["ErrorMessage"] = "Error exporting customers.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}