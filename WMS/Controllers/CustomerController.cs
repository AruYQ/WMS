using Microsoft.AspNetCore.Mvc;
using WMS.Models;
using WMS.Data.Repositories;

namespace WMS.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            ICustomerRepository customerRepository,
            ILogger<CustomerController> logger)
        {
            _customerRepository = customerRepository;
            _logger = logger;
        }

        // GET: Customer
        public async Task<IActionResult> Index(string? searchTerm = null, bool? isActive = null)
        {
            try
            {
                IEnumerable<Customer> customers;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    customers = await _customerRepository.SearchCustomersAsync(searchTerm);
                }
                else if (isActive.HasValue)
                {
                    if (isActive.Value)
                        customers = await _customerRepository.GetActiveCustomersAsync();
                    else
                        customers = (await _customerRepository.GetAllAsync()).Where(c => !c.IsActive);
                }
                else
                {
                    customers = await _customerRepository.GetAllAsync();
                }

                ViewBag.SearchTerm = searchTerm;
                ViewBag.IsActive = isActive;

                // Get statistics
                var totalCustomers = (await _customerRepository.GetAllAsync()).Count();
                var activeCustomers = (await _customerRepository.GetActiveCustomersAsync()).Count();
                var customersWithOrders = (await _customerRepository.GetAllWithSalesOrdersAsync())
                    .Count(c => c.SalesOrders.Any());

                ViewBag.Statistics = new Dictionary<string, object>
                {
                    ["TotalCustomers"] = totalCustomers,
                    ["ActiveCustomers"] = activeCustomers,
                    ["InactiveCustomers"] = totalCustomers - activeCustomers,
                    ["CustomersWithOrders"] = customersWithOrders
                };

                return View(customers.OrderBy(c => c.Name));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers");
                TempData["ErrorMessage"] = "Error loading customers. Please try again.";
                return View(new List<Customer>());
            }
        }

        // GET: Customer/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var customer = await _customerRepository.GetByIdWithSalesOrdersAsync(id);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Calculate additional details
                var totalOrders = customer.SalesOrders.Count();
                var totalOrderValue = customer.SalesOrders.Sum(so => so.TotalAmount);
                var recentOrders = customer.SalesOrders.OrderByDescending(so => so.OrderDate).Take(5);

                ViewBag.TotalOrders = totalOrders;
                ViewBag.TotalOrderValue = totalOrderValue;
                ViewBag.RecentOrders = recentOrders;

                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading customer details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            return View(new Customer());
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(customer);
                }

                // Validate email uniqueness
                if (await _customerRepository.ExistsByEmailAsync(customer.Email))
                {
                    ModelState.AddModelError("Email", "A customer with this email already exists. Please use a different email.");
                    return View(customer);
                }

                // Set created date and user
                customer.CreatedDate = DateTime.Now;
                customer.CreatedBy = User.Identity?.Name ?? "System";

                var createdCustomer = await _customerRepository.AddAsync(customer);

                TempData["SuccessMessage"] = $"Customer '{createdCustomer.Name}' created successfully.";
                return RedirectToAction(nameof(Details), new { id = createdCustomer.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                TempData["ErrorMessage"] = "Error creating customer. Please try again.";
                return View(customer);
            }
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(id);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer for edit, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading customer for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer customer)
        {
            try
            {
                if (id != customer.Id)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    return View(customer);
                }

                // Get existing customer to check email change
                var existingCustomer = await _customerRepository.GetByIdAsync(id);
                if (existingCustomer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate email uniqueness (if email changed)
                if (existingCustomer.Email != customer.Email && await _customerRepository.ExistsByEmailAsync(customer.Email))
                {
                    ModelState.AddModelError("Email", "A customer with this email already exists. Please use a different email.");
                    return View(customer);
                }

                // Update fields
                existingCustomer.Name = customer.Name;
                existingCustomer.Email = customer.Email;
                existingCustomer.Phone = customer.Phone;
                existingCustomer.Address = customer.Address;
                existingCustomer.IsActive = customer.IsActive;
                existingCustomer.ModifiedDate = DateTime.Now;
                existingCustomer.ModifiedBy = User.Identity?.Name ?? "System";

                await _customerRepository.UpdateAsync(existingCustomer);

                TempData["SuccessMessage"] = $"Customer '{existingCustomer.Name}' updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error updating customer. Please try again.";
                return View(customer);
            }
        }

        // GET: Customer/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var customer = await _customerRepository.GetByIdWithSalesOrdersAsync(id);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if customer can be deleted
                if (customer.SalesOrders.Any())
                {
                    TempData["ErrorMessage"] = "This customer cannot be deleted because it has associated sales orders.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(customer);
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
                var customer = await _customerRepository.GetByIdWithSalesOrdersAsync(id);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Double-check if customer can be deleted
                if (customer.SalesOrders.Any())
                {
                    TempData["ErrorMessage"] = "This customer cannot be deleted because it has associated sales orders.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _customerRepository.DeleteAsync(id);
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
                var customer = await _customerRepository.GetByIdAsync(id);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }

                customer.IsActive = !customer.IsActive;
                customer.ModifiedDate = DateTime.Now;
                customer.ModifiedBy = User.Identity?.Name ?? "System";

                await _customerRepository.UpdateAsync(customer);

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
                var exists = await _customerRepository.ExistsByEmailAsync(email);

                // If we're editing an existing customer, check if the email belongs to a different customer
                if (excludeId.HasValue && exists)
                {
                    var existingCustomer = await _customerRepository.GetByIdAsync(excludeId.Value);
                    exists = existingCustomer?.Email != email;
                }

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
                var customers = await _customerRepository.SearchCustomersAsync(searchTerm);

                return Json(new
                {
                    success = true,
                    customers = customers.Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        email = c.Email,
                        phone = c.Phone,
                        address = c.Address,
                        isActive = c.IsActive
                    })
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
                var customer = await _customerRepository.GetByIdWithSalesOrdersAsync(id);
                if (customer == null)
                {
                    return Json(new { success = false, message = "Customer not found" });
                }

                return Json(new
                {
                    success = true,
                    id = customer.Id,
                    name = customer.Name,
                    email = customer.Email,
                    phone = customer.Phone,
                    address = customer.Address,
                    isActive = customer.IsActive,
                    totalOrders = customer.SalesOrders.Count(),
                    totalValue = customer.SalesOrders.Sum(so => so.TotalAmount)
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
                var customersWithOrders = await _customerRepository.GetAllWithSalesOrdersAsync();

                var performanceData = customersWithOrders.Select(c => new
                {
                    Customer = c,
                    TotalOrders = c.SalesOrders.Count(),
                    TotalValue = c.SalesOrders.Sum(so => so.TotalAmount),
                    LastOrderDate = c.SalesOrders.Any() ? c.SalesOrders.Max(so => so.OrderDate) : (DateTime?)null,
                    AverageOrderValue = c.SalesOrders.Any() ? c.SalesOrders.Average(so => so.TotalAmount) : 0
                }).OrderByDescending(p => p.TotalValue);

                return View(performanceData);
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
                var customers = await _customerRepository.GetAllWithSalesOrdersAsync();

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