using Microsoft.AspNetCore.Mvc;
using WMS.Models;
using WMS.Data.Repositories;

namespace WMS.Controllers
{
    public class SupplierController : Controller
    {
        private readonly ISupplierRepository _supplierRepository;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(
            ISupplierRepository supplierRepository,
            ILogger<SupplierController> logger)
        {
            _supplierRepository = supplierRepository;
            _logger = logger;
        }

        // GET: Supplier
        public async Task<IActionResult> Index(string? searchTerm = null, bool? isActive = null)
        {
            try
            {
                IEnumerable<Supplier> suppliers;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    suppliers = await _supplierRepository.SearchSuppliersAsync(searchTerm);
                }
                else if (isActive.HasValue)
                {
                    if (isActive.Value)
                        suppliers = await _supplierRepository.GetActiveSuppliers();
                    else
                        suppliers = (await _supplierRepository.GetAllAsync()).Where(s => !s.IsActive);
                }
                else
                {
                    suppliers = await _supplierRepository.GetAllAsync();
                }

                ViewBag.SearchTerm = searchTerm;
                ViewBag.IsActive = isActive;

                // Get statistics
                var totalSuppliers = (await _supplierRepository.GetAllAsync()).Count();
                var activeSuppliers = (await _supplierRepository.GetActiveSuppliers()).Count();
                var suppliersWithOrders = (await _supplierRepository.GetAllWithPurchaseOrdersAsync())
                    .Count(s => s.PurchaseOrders.Any());

                ViewBag.Statistics = new Dictionary<string, object>
                {
                    ["TotalSuppliers"] = totalSuppliers,
                    ["ActiveSuppliers"] = activeSuppliers,
                    ["InactiveSuppliers"] = totalSuppliers - activeSuppliers,
                    ["SuppliersWithOrders"] = suppliersWithOrders
                };

                return View(suppliers.OrderBy(s => s.Name));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading suppliers");
                TempData["ErrorMessage"] = "Error loading suppliers. Please try again.";
                return View(new List<Supplier>());
            }
        }

        // GET: Supplier/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var supplier = await _supplierRepository.GetByIdWithPurchaseOrdersAsync(id);
                if (supplier == null)
                {
                    TempData["ErrorMessage"] = "Supplier not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Calculate additional details
                var totalOrders = supplier.PurchaseOrders.Count();
                var totalOrderValue = supplier.PurchaseOrders.Sum(po => po.TotalAmount);
                var recentOrders = supplier.PurchaseOrders.OrderByDescending(po => po.OrderDate).Take(5);

                ViewBag.TotalOrders = totalOrders;
                ViewBag.TotalOrderValue = totalOrderValue;
                ViewBag.RecentOrders = recentOrders;

                return View(supplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading supplier details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Supplier/Create
        public IActionResult Create()
        {
            return View(new Supplier());
        }

        // POST: Supplier/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(supplier);
                }

                // Validate email uniqueness
                if (await _supplierRepository.ExistsByEmailAsync(supplier.Email))
                {
                    ModelState.AddModelError("Email", "A supplier with this email already exists. Please use a different email.");
                    return View(supplier);
                }

                // Set created date and user
                supplier.CreatedDate = DateTime.Now;
                supplier.CreatedBy = User.Identity?.Name ?? "System";

                var createdSupplier = await _supplierRepository.AddAsync(supplier);

                TempData["SuccessMessage"] = $"Supplier '{createdSupplier.Name}' created successfully.";
                return RedirectToAction(nameof(Details), new { id = createdSupplier.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier");
                TempData["ErrorMessage"] = "Error creating supplier. Please try again.";
                return View(supplier);
            }
        }

        // GET: Supplier/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var supplier = await _supplierRepository.GetByIdAsync(id);
                if (supplier == null)
                {
                    TempData["ErrorMessage"] = "Supplier not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(supplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier for edit, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading supplier for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Supplier/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            try
            {
                if (id != supplier.Id)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    return View(supplier);
                }

                // Get existing supplier to check email change
                var existingSupplier = await _supplierRepository.GetByIdAsync(id);
                if (existingSupplier == null)
                {
                    TempData["ErrorMessage"] = "Supplier not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate email uniqueness (if email changed)
                if (existingSupplier.Email != supplier.Email && await _supplierRepository.ExistsByEmailAsync(supplier.Email))
                {
                    ModelState.AddModelError("Email", "A supplier with this email already exists. Please use a different email.");
                    return View(supplier);
                }

                // Update fields
                existingSupplier.Name = supplier.Name;
                existingSupplier.Email = supplier.Email;
                existingSupplier.Phone = supplier.Phone;
                existingSupplier.Address = supplier.Address;
                existingSupplier.IsActive = supplier.IsActive;
                existingSupplier.ModifiedDate = DateTime.Now;
                existingSupplier.ModifiedBy = User.Identity?.Name ?? "System";

                await _supplierRepository.UpdateAsync(existingSupplier);

                TempData["SuccessMessage"] = $"Supplier '{existingSupplier.Name}' updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error updating supplier. Please try again.";
                return View(supplier);
            }
        }

        // GET: Supplier/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var supplier = await _supplierRepository.GetByIdWithPurchaseOrdersAsync(id);
                if (supplier == null)
                {
                    TempData["ErrorMessage"] = "Supplier not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if supplier can be deleted
                if (supplier.PurchaseOrders.Any())
                {
                    TempData["ErrorMessage"] = "This supplier cannot be deleted because it has associated purchase orders.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(supplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier for delete, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error loading supplier.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Supplier/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var supplier = await _supplierRepository.GetByIdWithPurchaseOrdersAsync(id);
                if (supplier == null)
                {
                    TempData["ErrorMessage"] = "Supplier not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Double-check if supplier can be deleted
                if (supplier.PurchaseOrders.Any())
                {
                    TempData["ErrorMessage"] = "This supplier cannot be deleted because it has associated purchase orders.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _supplierRepository.DeleteAsync(id);
                TempData["SuccessMessage"] = "Supplier deleted successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error deleting supplier. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Supplier/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var supplier = await _supplierRepository.GetByIdAsync(id);
                if (supplier == null)
                {
                    TempData["ErrorMessage"] = "Supplier not found.";
                    return RedirectToAction(nameof(Index));
                }

                supplier.IsActive = !supplier.IsActive;
                supplier.ModifiedDate = DateTime.Now;
                supplier.ModifiedBy = User.Identity?.Name ?? "System";

                await _supplierRepository.UpdateAsync(supplier);

                var status = supplier.IsActive ? "activated" : "deactivated";
                TempData["SuccessMessage"] = $"Supplier '{supplier.Name}' {status} successfully.";

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling supplier status, ID: {Id}", id);
                TempData["ErrorMessage"] = "Error updating supplier status.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Supplier/CheckEmail
        [HttpGet]
        public async Task<JsonResult> CheckEmail(string email, int? excludeId = null)
        {
            try
            {
                var exists = await _supplierRepository.ExistsByEmailAsync(email);

                // If we're editing an existing supplier, check if the email belongs to a different supplier
                if (excludeId.HasValue && exists)
                {
                    var existingSupplier = await _supplierRepository.GetByIdAsync(excludeId.Value);
                    exists = existingSupplier?.Email != email;
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

        // GET: Supplier/GetSuppliersBySearch
        [HttpGet]
        public async Task<JsonResult> GetSuppliersBySearch(string searchTerm)
        {
            try
            {
                var suppliers = await _supplierRepository.SearchSuppliersAsync(searchTerm);

                return Json(new
                {
                    success = true,
                    suppliers = suppliers.Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        email = s.Email,
                        phone = s.Phone,
                        address = s.Address,
                        isActive = s.IsActive
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching suppliers: {SearchTerm}", searchTerm);
                return Json(new { success = false, message = "Error searching suppliers" });
            }
        }

        // GET: Supplier/GetSupplierDetails
        [HttpGet]
        public async Task<JsonResult> GetSupplierDetails(int id)
        {
            try
            {
                var supplier = await _supplierRepository.GetByIdWithPurchaseOrdersAsync(id);
                if (supplier == null)
                {
                    return Json(new { success = false, message = "Supplier not found" });
                }

                return Json(new
                {
                    success = true,
                    id = supplier.Id,
                    name = supplier.Name,
                    email = supplier.Email,
                    phone = supplier.Phone,
                    address = supplier.Address,
                    isActive = supplier.IsActive,
                    totalOrders = supplier.PurchaseOrders.Count(),
                    totalValue = supplier.PurchaseOrders.Sum(po => po.TotalAmount)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supplier details for ID: {Id}", id);
                return Json(new { success = false, message = "Error getting supplier details" });
            }
        }

        // GET: Supplier/PerformanceReport
        public async Task<IActionResult> PerformanceReport()
        {
            try
            {
                var suppliersWithOrders = await _supplierRepository.GetAllWithPurchaseOrdersAsync();

                var performanceData = suppliersWithOrders.Select(s => new
                {
                    Supplier = s,
                    TotalOrders = s.PurchaseOrders.Count(),
                    TotalValue = s.PurchaseOrders.Sum(po => po.TotalAmount),
                    LastOrderDate = s.PurchaseOrders.Max(po => po.OrderDate),
                    AverageOrderValue = s.PurchaseOrders.Any() ? s.PurchaseOrders.Average(po => po.TotalAmount) : 0
                }).OrderByDescending(p => p.TotalValue);

                return View(performanceData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier performance report");
                TempData["ErrorMessage"] = "Error loading performance report.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Supplier/Export
        public async Task<IActionResult> Export()
        {
            try
            {
                var suppliers = await _supplierRepository.GetAllWithPurchaseOrdersAsync();

                // Here you would implement Excel export logic
                // For now, returning the view for demonstration
                return View(suppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting suppliers");
                TempData["ErrorMessage"] = "Error exporting suppliers.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}