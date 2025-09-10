using Microsoft.AspNetCore.Mvc;
using WMS.Models;
using WMS.Data.Repositories;
using WMS.Models.ViewModels;
using WMS.Services;

namespace WMS.Controllers
{
    public class SupplierController : Controller
    {
        private readonly ISupplierRepository _supplierRepository;
        private readonly ILogger<SupplierController> _logger;
        private readonly ICurrentUserService _currentUserService;

        public SupplierController(
            ISupplierRepository supplierRepository,
            ILogger<SupplierController> logger,
            ICurrentUserService currentUserService)
        {
            _supplierRepository = supplierRepository;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        // GET: Supplier
        public async Task<IActionResult> Index(string? searchTerm = null, bool? isActive = null)
        {
            try
            {
                _logger.LogInformation("Supplier Index - Starting with SearchTerm: {SearchTerm}, IsActive: {IsActive}", searchTerm, isActive);

                // Debug CompanyId
                var companyId = _currentUserService.CompanyId;
                _logger.LogInformation("Current CompanyId: {CompanyId}", companyId);

                if (!companyId.HasValue)
                {
                    _logger.LogWarning("No CompanyId found for current user");
                    TempData["ErrorMessage"] = "Company ID tidak ditemukan. Silakan login ulang.";
                    return View(new SupplierIndexViewModel());
                }

                IEnumerable<Supplier> suppliers;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    _logger.LogInformation("Searching suppliers with term: {SearchTerm}", searchTerm);
                    suppliers = await _supplierRepository.SearchSuppliersAsync(searchTerm);
                }
                else if (isActive.HasValue)
                {
                    _logger.LogInformation("Filtering suppliers by active status: {IsActive}", isActive);
                    if (isActive.Value)
                        suppliers = await _supplierRepository.GetActiveSuppliers();
                    else
                        suppliers = (await _supplierRepository.GetAllAsync()).Where(s => !s.IsActive);
                }
                else
                {
                    _logger.LogInformation("Getting all suppliers for company: {CompanyId}", companyId);
                    suppliers = await _supplierRepository.GetAllAsync();
                }

                _logger.LogInformation("Found {Count} suppliers", suppliers.Count());

                // Get statistics - FIXED: Use safe LINQ operations
                _logger.LogInformation("Calculating statistics...");
                var allSuppliers = await _supplierRepository.GetAllAsync();
                var activeSuppliers = await _supplierRepository.GetActiveSuppliers();
                var suppliersWithOrders = await _supplierRepository.GetAllWithPurchaseOrdersAsync();

                var totalSuppliers = allSuppliers.Count();
                var activeSuppliersCount = activeSuppliers.Count();
                var suppliersWithOrdersCount = suppliersWithOrders.Count(s => s.PurchaseOrders?.Any() == true);

                _logger.LogInformation("Statistics - Total: {Total}, Active: {Active}, WithOrders: {WithOrders}", 
                    totalSuppliers, activeSuppliersCount, suppliersWithOrdersCount);

                // Convert to ViewModel - FIXED: Safe LINQ operations
                _logger.LogInformation("Converting suppliers to ViewModel...");
                var supplierViewModels = suppliers.OrderBy(s => s.Name).Select(s => new SupplierViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Email = s.Email,
                    Phone = s.Phone,
                    Address = s.Address,
                    IsActive = s.IsActive,
                    CreatedBy = s.CreatedBy,
                    CreatedDate = s.CreatedDate,
                    ModifiedBy = s.ModifiedBy,
                    ModifiedDate = s.ModifiedDate,
                    PurchaseOrderCount = s.PurchaseOrders?.Count() ?? 0,
                    ItemCount = s.Items?.Count() ?? 0,
                    TotalOrderValue = s.PurchaseOrders?.Sum(po => po.TotalAmount) ?? 0,
                    LastOrderDate = s.PurchaseOrders?.Any() == true ? s.PurchaseOrders.Max(po => po.OrderDate) : null
                }).ToList();

                _logger.LogInformation("Converted {Count} suppliers to ViewModel", supplierViewModels.Count);

                var viewModel = new SupplierIndexViewModel
                {
                    Suppliers = supplierViewModels,
                    SearchTerm = searchTerm,
                    IsActive = isActive,
                    TotalSuppliers = totalSuppliers,
                    ActiveSuppliers = activeSuppliersCount,
                    SuppliersWithOrders = suppliersWithOrdersCount,
                    InactiveSuppliers = totalSuppliers - activeSuppliersCount
                };

                _logger.LogInformation("Supplier Index completed successfully");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading suppliers - Exception: {ExceptionMessage}", ex.Message);
                
                // Try to get suppliers without company filtering for debugging
                try
                {
                    _logger.LogInformation("Attempting fallback query without company filtering...");
                    var allSuppliers = await _supplierRepository.GetAllAsync();
                    _logger.LogInformation("Fallback query found {Count} suppliers", allSuppliers.Count());
                    
                    // Log first few suppliers for debugging
                    foreach (var supplier in allSuppliers.Take(3))
                    {
                        _logger.LogInformation("Supplier: Id={Id}, Name={Name}, CompanyId={CompanyId}", 
                            supplier.Id, supplier.Name, supplier.CompanyId);
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback query also failed: {FallbackException}", fallbackEx.Message);
                }

                TempData["ErrorMessage"] = $"Error loading suppliers: {ex.Message}. Silakan periksa log untuk detail.";
                return View(new SupplierIndexViewModel());
            }
        }

        // GET: Supplier/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                _logger.LogInformation("Loading supplier details for ID: {Id}", id);
                
                var supplier = await _supplierRepository.GetByIdWithPurchaseOrdersAsync(id);
                if (supplier == null)
                {
                    _logger.LogWarning("Supplier not found for ID: {Id}", id);
                    TempData["ErrorMessage"] = "Supplier not found.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Supplier found: {Name}, CompanyId: {CompanyId}", supplier.Name, supplier.CompanyId);

                // Convert to ViewModel - FIXED: Safe LINQ operations
                var supplierViewModel = new SupplierViewModel
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    Email = supplier.Email,
                    Phone = supplier.Phone,
                    Address = supplier.Address,
                    IsActive = supplier.IsActive,
                    CreatedBy = supplier.CreatedBy,
                    CreatedDate = supplier.CreatedDate,
                    ModifiedBy = supplier.ModifiedBy,
                    ModifiedDate = supplier.ModifiedDate,
                    PurchaseOrderCount = supplier.PurchaseOrders?.Count() ?? 0,
                    ItemCount = supplier.Items?.Count() ?? 0,
                    TotalOrderValue = supplier.PurchaseOrders?.Sum(po => po.TotalAmount) ?? 0,
                    LastOrderDate = supplier.PurchaseOrders?.Any() == true ? supplier.PurchaseOrders.Max(po => po.OrderDate) : null,
                    PurchaseOrders = supplier.PurchaseOrders?.Select(po => new PurchaseOrderSummary
                    {
                        Id = po.Id,
                        PONumber = po.PONumber,
                        OrderDate = po.OrderDate,
                        Status = po.Status,
                        TotalAmount = po.TotalAmount,
                        EmailSent = po.EmailSent,
                        EmailSentDate = po.EmailSentDate
                    }).ToList() ?? new List<PurchaseOrderSummary>(),
                    Items = supplier.Items?.Select(item => new ItemSummary
                    {
                        Id = item.Id,
                        ItemCode = item.ItemCode,
                        Name = item.Name,
                        Description = item.Description ?? "",
                        Unit = item.Unit,
                        StandardPrice = item.StandardPrice,
                        IsActive = item.IsActive,
                        TotalStock = item.Inventories?.Sum(inv => inv.Quantity) ?? 0,
                        TotalValue = (item.Inventories?.Sum(inv => inv.Quantity) ?? 0) * item.StandardPrice
                    }).ToList() ?? new List<ItemSummary>()
                };

                return View(supplierViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier details for ID: {Id} - Exception: {ExceptionMessage}", id, ex.Message);
                TempData["ErrorMessage"] = $"Error loading supplier details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Supplier/Create
        public IActionResult Create()
        {
            return View(new SupplierViewModel());
        }

        // POST: Supplier/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierViewModel supplierViewModel)
        {
            try
            {
                _logger.LogInformation("Creating supplier: {Name}, Email: {Email}", supplierViewModel.Name, supplierViewModel.Email);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model validation failed for supplier creation");
                    return View(supplierViewModel);
                }

                // Validate email uniqueness
                if (await _supplierRepository.ExistsByEmailAsync(supplierViewModel.Email))
                {
                    _logger.LogWarning("Email already exists: {Email}", supplierViewModel.Email);
                    ModelState.AddModelError("Email", "A supplier with this email already exists. Please use a different email.");
                    return View(supplierViewModel);
                }

                // Convert ViewModel to Model
                var supplier = new Supplier
                {
                    Name = supplierViewModel.Name,
                    Email = supplierViewModel.Email,
                    Phone = supplierViewModel.Phone,
                    Address = supplierViewModel.Address,
                    IsActive = supplierViewModel.IsActive,
                    CreatedDate = DateTime.Now,
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                var createdSupplier = await _supplierRepository.AddAsync(supplier);
                _logger.LogInformation("Supplier created successfully: ID={Id}, Name={Name}, CompanyId={CompanyId}", 
                    createdSupplier.Id, createdSupplier.Name, createdSupplier.CompanyId);

                TempData["SuccessMessage"] = $"Supplier '{createdSupplier.Name}' created successfully.";
                return RedirectToAction(nameof(Details), new { id = createdSupplier.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier - Exception: {ExceptionMessage}", ex.Message);
                TempData["ErrorMessage"] = $"Error creating supplier: {ex.Message}";
                return View(supplierViewModel);
            }
        }

        // GET: Supplier/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                _logger.LogInformation("Loading supplier for edit: ID={Id}", id);
                
                var supplier = await _supplierRepository.GetByIdAsync(id);
                if (supplier == null)
                {
                    _logger.LogWarning("Supplier not found for edit: ID={Id}", id);
                    TempData["ErrorMessage"] = "Supplier not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Convert to ViewModel
                var supplierViewModel = new SupplierViewModel
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    Email = supplier.Email,
                    Phone = supplier.Phone,
                    Address = supplier.Address,
                    IsActive = supplier.IsActive,
                    CreatedBy = supplier.CreatedBy,
                    CreatedDate = supplier.CreatedDate,
                    ModifiedBy = supplier.ModifiedBy,
                    ModifiedDate = supplier.ModifiedDate
                };

                return View(supplierViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier for edit, ID: {Id} - Exception: {ExceptionMessage}", id, ex.Message);
                TempData["ErrorMessage"] = $"Error loading supplier for editing: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Supplier/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SupplierViewModel supplierViewModel)
        {
            try
            {
                _logger.LogInformation("Updating supplier: ID={Id}, Name={Name}", id, supplierViewModel.Name);

                if (id != supplierViewModel.Id)
                {
                    _logger.LogWarning("ID mismatch in edit: {Id} != {ViewModelId}", id, supplierViewModel.Id);
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model validation failed for supplier update");
                    return View(supplierViewModel);
                }

                // Get existing supplier to check email change
                var existingSupplier = await _supplierRepository.GetByIdAsync(id);
                if (existingSupplier == null)
                {
                    _logger.LogWarning("Existing supplier not found for update: ID={Id}", id);
                    TempData["ErrorMessage"] = "Supplier not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate email uniqueness (if email changed)
                if (existingSupplier.Email != supplierViewModel.Email && await _supplierRepository.ExistsByEmailAsync(supplierViewModel.Email))
                {
                    _logger.LogWarning("Email already exists for update: {Email}", supplierViewModel.Email);
                    ModelState.AddModelError("Email", "A supplier with this email already exists. Please use a different email.");
                    return View(supplierViewModel);
                }

                // Update fields
                existingSupplier.Name = supplierViewModel.Name;
                existingSupplier.Email = supplierViewModel.Email;
                existingSupplier.Phone = supplierViewModel.Phone;
                existingSupplier.Address = supplierViewModel.Address;
                existingSupplier.IsActive = supplierViewModel.IsActive;
                existingSupplier.ModifiedDate = DateTime.Now;
                existingSupplier.ModifiedBy = User.Identity?.Name ?? "System";

                await _supplierRepository.UpdateAsync(existingSupplier);
                _logger.LogInformation("Supplier updated successfully: ID={Id}, Name={Name}", id, existingSupplier.Name);

                TempData["SuccessMessage"] = $"Supplier '{existingSupplier.Name}' updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier, ID: {Id} - Exception: {ExceptionMessage}", id, ex.Message);
                TempData["ErrorMessage"] = $"Error updating supplier: {ex.Message}";
                return View(supplierViewModel);
            }
        }

        // GET: Supplier/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Loading supplier for delete: ID={Id}", id);
                
                var supplier = await _supplierRepository.GetByIdWithPurchaseOrdersAsync(id);
                if (supplier == null)
                {
                    _logger.LogWarning("Supplier not found for delete: ID={Id}", id);
                    TempData["ErrorMessage"] = "Supplier not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Convert to ViewModel - FIXED: Safe LINQ operations
                var supplierViewModel = new SupplierViewModel
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    Email = supplier.Email,
                    Phone = supplier.Phone,
                    Address = supplier.Address,
                    IsActive = supplier.IsActive,
                    CreatedBy = supplier.CreatedBy,
                    CreatedDate = supplier.CreatedDate,
                    ModifiedBy = supplier.ModifiedBy,
                    ModifiedDate = supplier.ModifiedDate,
                    PurchaseOrderCount = supplier.PurchaseOrders?.Count() ?? 0,
                    ItemCount = supplier.Items?.Count() ?? 0,
                    TotalOrderValue = supplier.PurchaseOrders?.Sum(po => po.TotalAmount) ?? 0,
                    LastOrderDate = supplier.PurchaseOrders?.Any() == true ? supplier.PurchaseOrders.Max(po => po.OrderDate) : null
                };

                return View(supplierViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier for delete, ID: {Id} - Exception: {ExceptionMessage}", id, ex.Message);
                TempData["ErrorMessage"] = $"Error loading supplier: {ex.Message}";
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
                _logger.LogInformation("Deleting supplier: ID={Id}", id);
                
                var supplier = await _supplierRepository.GetByIdWithPurchaseOrdersAsync(id);
                if (supplier == null)
                {
                    _logger.LogWarning("Supplier not found for deletion: ID={Id}", id);
                    TempData["ErrorMessage"] = "Supplier not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Double-check if supplier can be deleted
                if (supplier.PurchaseOrders?.Any() == true)
                {
                    _logger.LogWarning("Cannot delete supplier with purchase orders: ID={Id}, OrderCount={Count}", 
                        id, supplier.PurchaseOrders.Count());
                    TempData["ErrorMessage"] = "This supplier cannot be deleted because it has associated purchase orders.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _supplierRepository.DeleteAsync(id);
                _logger.LogInformation("Supplier deleted successfully: ID={Id}, Name={Name}", id, supplier.Name);
                
                TempData["SuccessMessage"] = "Supplier deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier, ID: {Id} - Exception: {ExceptionMessage}", id, ex.Message);
                TempData["ErrorMessage"] = $"Error deleting supplier: {ex.Message}";
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
                _logger.LogInformation("Toggling supplier status: ID={Id}", id);
                
                var supplier = await _supplierRepository.GetByIdAsync(id);
                if (supplier == null)
                {
                    _logger.LogWarning("Supplier not found for status toggle: ID={Id}", id);
                    TempData["ErrorMessage"] = "Supplier not found.";
                    return RedirectToAction(nameof(Index));
                }

                supplier.IsActive = !supplier.IsActive;
                supplier.ModifiedDate = DateTime.Now;
                supplier.ModifiedBy = User.Identity?.Name ?? "System";

                await _supplierRepository.UpdateAsync(supplier);

                var status = supplier.IsActive ? "activated" : "deactivated";
                _logger.LogInformation("Supplier status toggled: ID={Id}, NewStatus={Status}", id, status);
                
                TempData["SuccessMessage"] = $"Supplier '{supplier.Name}' {status} successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling supplier status, ID: {Id} - Exception: {ExceptionMessage}", id, ex.Message);
                TempData["ErrorMessage"] = $"Error updating supplier status: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Supplier/CheckEmailUniqueness
        [HttpGet]
        public async Task<JsonResult> CheckEmailUniqueness(string email, int? excludeId = null)
        {
            try
            {
                _logger.LogInformation("Checking email uniqueness: {Email}, ExcludeId: {ExcludeId}", email, excludeId);
                
                var exists = await _supplierRepository.ExistsByEmailAsync(email);

                // If we're editing an existing supplier, check if the email belongs to a different supplier
                if (excludeId.HasValue && exists)
                {
                    var existingSupplier = await _supplierRepository.GetByIdAsync(excludeId.Value);
                    exists = existingSupplier?.Email != email;
                }

                _logger.LogInformation("Email uniqueness check result: {Email} - IsUnique: {IsUnique}", email, !exists);

                return Json(new
                {
                    isUnique = !exists,
                    message = exists ? "Email already exists" : "Email is available"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email uniqueness: {Email} - Exception: {ExceptionMessage}", email, ex.Message);
                return Json(new { isUnique = false, message = "Error checking email" });
            }
        }

        // GET: Supplier/GetSuppliersBySearch
        [HttpGet]
        public async Task<JsonResult> GetSuppliersBySearch(string searchTerm)
        {
            try
            {
                _logger.LogInformation("Searching suppliers: {SearchTerm}", searchTerm);
                
                var suppliers = await _supplierRepository.SearchSuppliersAsync(searchTerm);

                _logger.LogInformation("Search found {Count} suppliers", suppliers.Count());

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
                _logger.LogError(ex, "Error searching suppliers: {SearchTerm} - Exception: {ExceptionMessage}", searchTerm, ex.Message);
                return Json(new { success = false, message = "Error searching suppliers" });
            }
        }

        // GET: Supplier/GetSupplierDetails
        [HttpGet]
        public async Task<JsonResult> GetSupplierDetails(int id)
        {
            try
            {
                _logger.LogInformation("Getting supplier details: ID={Id}", id);
                
                var supplier = await _supplierRepository.GetByIdWithPurchaseOrdersAsync(id);
                if (supplier == null)
                {
                    _logger.LogWarning("Supplier not found for details: ID={Id}", id);
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
                    totalOrders = supplier.PurchaseOrders?.Count() ?? 0,
                    totalValue = supplier.PurchaseOrders?.Sum(po => po.TotalAmount) ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supplier details for ID: {Id} - Exception: {ExceptionMessage}", id, ex.Message);
                return Json(new { success = false, message = "Error getting supplier details" });
            }
        }

        // GET: Supplier/PerformanceReport
        public async Task<IActionResult> PerformanceReport()
        {
            try
            {
                _logger.LogInformation("Loading supplier performance report");
                
                var suppliersWithOrders = await _supplierRepository.GetAllWithPurchaseOrdersAsync();

                var performanceData = suppliersWithOrders.Select(s => new
                {
                    Supplier = s,
                    TotalOrders = s.PurchaseOrders?.Count() ?? 0,
                    TotalValue = s.PurchaseOrders?.Sum(po => po.TotalAmount) ?? 0,
                    LastOrderDate = s.PurchaseOrders?.Any() == true ? (DateTime?)s.PurchaseOrders.Max(po => po.OrderDate) : null,
                    AverageOrderValue = s.PurchaseOrders?.Any() == true ? s.PurchaseOrders.Average(po => po.TotalAmount) : 0
                }).OrderByDescending(p => p.TotalValue);

                return View(performanceData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier performance report - Exception: {ExceptionMessage}", ex.Message);
                TempData["ErrorMessage"] = $"Error loading performance report: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Supplier/Export
        public async Task<IActionResult> Export()
        {
            try
            {
                _logger.LogInformation("Exporting suppliers");
                
                var suppliers = await _supplierRepository.GetAllWithPurchaseOrdersAsync();

                // Here you would implement Excel export logic
                // For now, returning the view for demonstration
                return View(suppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting suppliers - Exception: {ExceptionMessage}", ex.Message);
                TempData["ErrorMessage"] = $"Error exporting suppliers: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}