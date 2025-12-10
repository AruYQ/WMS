using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.Data.Repositories;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Attributes;
using WMS.Utilities;
using System.Text.Json;
using WMS.Data;

namespace WMS.Controllers
{
    [RequirePermission(Constants.SUPPLIER_VIEW)]
    public class SupplierController : Controller
    {
        private readonly ISupplierRepository _supplierRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SupplierController> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditTrailService _auditService;

        public SupplierController(
            ISupplierRepository supplierRepository,
            ApplicationDbContext context,
            ILogger<SupplierController> logger,
            ICurrentUserService currentUserService,
            IAuditTrailService auditService)
        {
            _supplierRepository = supplierRepository;
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
            _auditService = auditService;
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

        #region API Endpoints

        /// <summary>
        /// GET: api/supplier/dashboard
        /// Get supplier statistics for dashboard
        /// </summary>
        [HttpGet("api/supplier/dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var suppliers = await _supplierRepository.GetAllAsync();
                var totalSuppliers = suppliers.Count();
                var activeSuppliers = suppliers.Count(s => s.IsActive);
                var suppliersWithOrders = await _supplierRepository.GetAllWithPurchaseOrdersAsync();
                var suppliersWithOrdersCount = suppliersWithOrders.Count(s => s.PurchaseOrders?.Any() == true);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        TotalSuppliers = totalSuppliers,
                        ActiveSuppliers = activeSuppliers,
                        SuppliersWithOrders = suppliersWithOrdersCount,
                        InactiveSuppliers = totalSuppliers - activeSuppliers
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supplier dashboard");
                return StatusCode(500, new { success = false, message = "Error loading dashboard data" });
            }
        }

        /// <summary>
        /// GET: api/supplier
        /// Get paginated supplier list
        /// </summary>
        [HttpGet("api/supplier")]
        public async Task<IActionResult> GetSuppliers([FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var suppliers = await _supplierRepository.GetAllAsync();
                
                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    suppliers = suppliers.Where(s => s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                                   s.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                if (isActive.HasValue)
                {
                    suppliers = suppliers.Where(s => s.IsActive == isActive.Value);
                }

                var totalCount = suppliers.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var paginatedSuppliers = suppliers
                    .OrderBy(s => s.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        Id = s.Id,
                        Code = s.Code,
                        Name = s.Name,
                        Email = s.Email,
                        Phone = s.Phone,
                        ContactPerson = s.ContactPerson,
                        Address = s.Address,
                        City = s.City,
                        IsActive = s.IsActive,
                        CreatedDate = s.CreatedDate,
                        PurchaseOrderCount = s.PurchaseOrders?.Count() ?? 0
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = paginatedSuppliers,
                    totalCount = totalCount,
                    totalPages = totalPages,
                    currentPage = page
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting suppliers");
                return StatusCode(500, new { success = false, message = "Error loading suppliers" });
            }
        }

        #endregion

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
        [RequirePermission(Constants.SUPPLIER_MANAGE)]
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

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("CREATE", "Supplier", createdSupplier.Id, 
                        $"{createdSupplier.Code} - {createdSupplier.Name}", null, new { 
                            Code = createdSupplier.Code, 
                            Name = createdSupplier.Name, 
                            Email = createdSupplier.Email,
                            Phone = createdSupplier.Phone,
                            IsActive = createdSupplier.IsActive 
                        });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for supplier creation");
                }

                TempData["SuccessMessage"] = $"Supplier '{createdSupplier.Name}' created successfully.";
                return RedirectToAction(nameof(Details), new { id = createdSupplier.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier: {Email} - Exception: {ExceptionMessage}", 
                    supplierViewModel.Email, ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                
                // Check if it's a unique constraint violation
                if (ex.InnerException?.Message.Contains("duplicate key") == true || 
                    ex.InnerException?.Message.Contains("unique constraint") == true)
                {
                    ModelState.AddModelError("Email", "A supplier with this email already exists. Please use a different email.");
                    return View(supplierViewModel);
                }
                
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
        [RequirePermission(Constants.SUPPLIER_MANAGE)]
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

                // Store old values for audit trail
                var oldValues = new {
                    Name = existingSupplier.Name,
                    Email = existingSupplier.Email,
                    Phone = existingSupplier.Phone,
                    Address = existingSupplier.Address,
                    IsActive = existingSupplier.IsActive
                };

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

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("UPDATE", "Supplier", existingSupplier.Id, 
                        $"{existingSupplier.Code} - {existingSupplier.Name}", oldValues, new { 
                            Name = existingSupplier.Name,
                            Email = existingSupplier.Email,
                            Phone = existingSupplier.Phone,
                            Address = existingSupplier.Address,
                            IsActive = existingSupplier.IsActive 
                        });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for supplier update");
                }

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
        [RequirePermission(Constants.SUPPLIER_MANAGE)]
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
                
                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("DELETE", "Supplier", supplier.Id, 
                        $"{supplier.Code} - {supplier.Name}", new { 
                            Code = supplier.Code,
                            Name = supplier.Name,
                            Email = supplier.Email,
                            Phone = supplier.Phone,
                            IsActive = supplier.IsActive 
                        }, null);
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for supplier deletion");
                }
                
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
        [RequirePermission(Constants.SUPPLIER_MANAGE)]
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

                var oldStatus = supplier.IsActive;
                supplier.IsActive = !supplier.IsActive;
                supplier.ModifiedDate = DateTime.Now;
                supplier.ModifiedBy = User.Identity?.Name ?? "System";

                await _supplierRepository.UpdateAsync(supplier);

                // Log audit trail
                try
                {
                    var action = supplier.IsActive ? "ACTIVATE" : "DEACTIVATE";
                    var statusText = supplier.IsActive ? "activated" : "deactivated";
                    await _auditService.LogActionAsync(action, "Supplier", supplier.Id, 
                        $"{supplier.Code} - {supplier.Name}", new { IsActive = oldStatus }, new { IsActive = supplier.IsActive });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for supplier status toggle");
                }

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

        // GET: api/supplier/search
        // API endpoint for dropdown search
        [HttpGet("api/supplier/search")]
        [RequirePermission(Constants.SUPPLIER_VIEW)]
        public async Task<IActionResult> SearchSuppliers([FromQuery] string? search = null, [FromQuery] int limit = 20)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                IEnumerable<Supplier> suppliers;

                if (!string.IsNullOrEmpty(search))
                {
                    suppliers = await _supplierRepository.SearchSuppliersAsync(search);
                }
                else
                {
                    suppliers = await _supplierRepository.GetAllAsync();
                }

                // Filter by company and active status, then limit results
                var filteredSuppliers = suppliers
                    .Where(s => s.CompanyId == companyId.Value && s.IsActive && !s.IsDeleted)
                    .Take(limit)
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        email = s.Email,
                        phone = s.Phone,
                        address = s.Address
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = filteredSuppliers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching suppliers for dropdown: {SearchTerm}", search);
                return StatusCode(500, new { success = false, message = "Error searching suppliers" });
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

        /// <summary>
        /// GET: api/supplier/{id}
        /// Get single supplier by ID
        /// </summary>
        [HttpGet("api/supplier/{id}")]
        [RequirePermission(Constants.SUPPLIER_VIEW)]
        public async Task<IActionResult> GetSupplier(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var supplier = await _supplierRepository.GetByIdAsync(id);
                if (supplier == null || supplier.CompanyId != companyId.Value)
                {
                    return NotFound(new { success = false, message = "Supplier not found" });
                }

                var items = await _context.Items
                    .Where(i => i.CompanyId == companyId.Value &&
                                i.SupplierId == supplier.Id &&
                                !i.IsDeleted)
                    .Select(i => new
                    {
                        itemId = i.Id,
                        itemCode = i.ItemCode,
                        itemName = i.Name,
                        isActive = i.IsActive,
                        totalStock = i.Inventories.Sum(inv => inv.Quantity),
                        unit = i.Unit,
                        standardPrice = i.StandardPrice,
                        lastUpdated = i.ModifiedDate ?? i.CreatedDate
                    })
                    .OrderByDescending(i => i.totalStock)
                    .ToListAsync();

                var purchaseOrdersQuery = _context.PurchaseOrders
                    .Where(po =>
                        po.CompanyId == companyId.Value &&
                        po.SupplierId == supplier.Id &&
                        !po.IsDeleted);

                var totalPurchaseOrders = await purchaseOrdersQuery.CountAsync();
                var totalPurchaseValue = await purchaseOrdersQuery.SumAsync(po => (decimal?)po.TotalAmount) ?? 0m;

                var recentPurchaseOrders = await purchaseOrdersQuery
                    .OrderByDescending(po => po.OrderDate)
                    .Take(5)
                    .Select(po => new
                    {
                        poNumber = po.PONumber,
                        orderDate = po.OrderDate,
                        status = po.Status,
                        totalAmount = po.TotalAmount,
                        itemsCount = po.PurchaseOrderDetails.Sum(d => d.Quantity)
                    })
                    .ToListAsync();

                var topItems = items
                    .OrderByDescending(i => i.totalStock)
                    .ThenBy(i => i.itemCode)
                    .Take(10)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = supplier.Id,
                        code = supplier.Code,
                        name = supplier.Name,
                        email = supplier.Email,
                        phone = supplier.Phone,
                        contactPerson = supplier.ContactPerson,
                        address = supplier.Address,
                        city = supplier.City,
                        isActive = supplier.IsActive,
                        audit = new
                        {
                            createdDate = supplier.CreatedDate,
                            modifiedDate = supplier.ModifiedDate,
                            createdBy = supplier.CreatedBy,
                            modifiedBy = supplier.ModifiedBy
                        },
                        metrics = new
                        {
                            totalPurchaseOrders,
                            totalPurchaseValue,
                            totalItemsSupplied = items.Count,
                            activeItems = items.Count(i => i.isActive)
                        },
                        topItems,
                        recentPurchaseOrders
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supplier");
                return StatusCode(500, new { success = false, message = "Error getting supplier" });
            }
        }

        /// <summary>
        /// POST: api/supplier
        /// Create new supplier
        /// </summary>
        [HttpPost("api/supplier")]
        [RequirePermission(Constants.SUPPLIER_MANAGE)]
        public async Task<IActionResult> CreateSupplier([FromBody] SupplierCreateRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                // Validate email uniqueness
                if (await _supplierRepository.ExistsByEmailAsync(request.Email))
                {
                    return BadRequest(new { success = false, message = "A supplier with this email already exists" });
                }

                var supplier = new Supplier
                {
                    Code = request.Code,
                    Name = request.Name,
                    Email = request.Email,
                    Phone = request.Phone,
                    ContactPerson = request.ContactPerson,
                    Address = request.Address,
                    City = request.City,
                    IsActive = request.IsActive,
                    CompanyId = companyId.Value,
                    CreatedDate = DateTime.Now,
                    CreatedBy = _currentUserService.Username ?? "System"
                };

                await _supplierRepository.AddAsync(supplier);
                await _context.SaveChangesAsync();

                // Log audit trail
                await _auditService.LogActionAsync(
                    "CREATE",
                    "Supplier",
                    supplier.Id,
                    $"Supplier: {supplier.Name}",
                    null,
                    JsonSerializer.Serialize(new { Name = supplier.Name, Email = supplier.Email }),
                    "Supplier created successfully"
                );

                return Ok(new
                {
                    success = true,
                    message = "Supplier created successfully",
                    data = new
                    {
                        Id = supplier.Id,
                        Code = supplier.Code,
                        Name = supplier.Name,
                        Email = supplier.Email,
                        Phone = supplier.Phone,
                        ContactPerson = supplier.ContactPerson,
                        Address = supplier.Address,
                        City = supplier.City,
                        IsActive = supplier.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier");
                return StatusCode(500, new { success = false, message = "Error creating supplier" });
            }
        }

        /// <summary>
        /// PUT: api/supplier/{id}
        /// Update supplier
        /// </summary>
        [HttpPut("api/supplier/{id}")]
        [RequirePermission(Constants.SUPPLIER_MANAGE)]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] SupplierUpdateRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var supplier = await _supplierRepository.GetByIdAsync(id);
                if (supplier == null || supplier.CompanyId != companyId.Value)
                {
                    return NotFound(new { success = false, message = "Supplier not found" });
                }

                // Check email uniqueness if changed
                if (supplier.Email != request.Email && await _supplierRepository.ExistsByEmailAsync(request.Email))
                {
                    return BadRequest(new { success = false, message = "A supplier with this email already exists" });
                }

                var oldData = JsonSerializer.Serialize(new { 
                    Name = supplier.Name, 
                    Email = supplier.Email, 
                    Phone = supplier.Phone,
                    ContactPerson = supplier.ContactPerson,
                    Address = supplier.Address,
                    City = supplier.City,
                    IsActive = supplier.IsActive
                });

                supplier.Code = request.Code;
                supplier.Name = request.Name;
                supplier.Email = request.Email;
                supplier.Phone = request.Phone;
                supplier.ContactPerson = request.ContactPerson;
                supplier.Address = request.Address;
                supplier.City = request.City;
                supplier.IsActive = request.IsActive;
                supplier.ModifiedDate = DateTime.Now;
                supplier.ModifiedBy = _currentUserService.Username ?? "System";

                await _supplierRepository.UpdateAsync(supplier);
                await _context.SaveChangesAsync();

                // Log audit trail
                await _auditService.LogActionAsync(
                    "UPDATE",
                    "Supplier",
                    supplier.Id,
                    $"Supplier: {supplier.Name}",
                    oldData,
                    JsonSerializer.Serialize(new { 
                        Name = supplier.Name, 
                        Email = supplier.Email, 
                        Phone = supplier.Phone,
                        ContactPerson = supplier.ContactPerson,
                        Address = supplier.Address,
                        City = supplier.City,
                        IsActive = supplier.IsActive
                    }),
                    "Supplier updated successfully"
                );

                return Ok(new
                {
                    success = true,
                    message = "Supplier updated successfully",
                    data = new
                    {
                        Id = supplier.Id,
                        Code = supplier.Code,
                        Name = supplier.Name,
                        Email = supplier.Email,
                        Phone = supplier.Phone,
                        ContactPerson = supplier.ContactPerson,
                        Address = supplier.Address,
                        City = supplier.City,
                        IsActive = supplier.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier");
                return StatusCode(500, new { success = false, message = "Error updating supplier" });
            }
        }

        /// <summary>
        /// DELETE: api/supplier/{id}
        /// Delete supplier (soft delete)
        /// </summary>
        [HttpDelete("api/supplier/{id}")]
        [RequirePermission(Constants.SUPPLIER_MANAGE)]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var supplier = await _supplierRepository.GetByIdAsync(id);
                if (supplier == null || supplier.CompanyId != companyId.Value)
                {
                    return NotFound(new { success = false, message = "Supplier not found" });
                }

                var oldData = JsonSerializer.Serialize(new { 
                    Name = supplier.Name, 
                    Email = supplier.Email, 
                    Phone = supplier.Phone,
                    ContactPerson = supplier.ContactPerson,
                    Address = supplier.Address,
                    City = supplier.City,
                    IsActive = supplier.IsActive
                });

                supplier.IsDeleted = true;
                supplier.ModifiedDate = DateTime.Now;
                supplier.ModifiedBy = _currentUserService.Username ?? "System";

                await _supplierRepository.UpdateAsync(supplier);
                await _context.SaveChangesAsync();

                // Log audit trail
                await _auditService.LogActionAsync(
                    "DELETE",
                    "Supplier",
                    supplier.Id,
                    $"Supplier: {supplier.Name}",
                    oldData,
                    null,
                    "Supplier deleted successfully"
                );

                return Ok(new { success = true, message = "Supplier deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier");
                return StatusCode(500, new { success = false, message = "Error deleting supplier" });
            }
        }

        /// <summary>
        /// PATCH: api/supplier/{id}/toggle-status
        /// Toggle supplier status
        /// </summary>
        [HttpPatch("api/supplier/{id}/toggle-status")]
        [RequirePermission(Constants.SUPPLIER_MANAGE)]
        public async Task<IActionResult> ToggleSupplierStatus(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var supplier = await _supplierRepository.GetByIdAsync(id);
                if (supplier == null || supplier.CompanyId != companyId.Value)
                {
                    return NotFound(new { success = false, message = "Supplier not found" });
                }

                var oldStatus = supplier.IsActive;
                supplier.IsActive = !supplier.IsActive;
                supplier.ModifiedDate = DateTime.Now;
                supplier.ModifiedBy = _currentUserService.Username ?? "System";

                await _supplierRepository.UpdateAsync(supplier);
                await _context.SaveChangesAsync();

                // Log audit trail
                await _auditService.LogActionAsync(
                    "TOGGLE_STATUS",
                    "Supplier",
                    supplier.Id,
                    $"Supplier: {supplier.Name}",
                    JsonSerializer.Serialize(new { IsActive = oldStatus }),
                    JsonSerializer.Serialize(new { IsActive = supplier.IsActive }),
                    $"Supplier status changed to {(supplier.IsActive ? "Active" : "Inactive")}"
                );

                return Ok(new 
                { 
                    success = true, 
                    message = $"Supplier {(supplier.IsActive ? "activated" : "deactivated")} successfully",
                    data = new { IsActive = supplier.IsActive }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling supplier status");
                return StatusCode(500, new { success = false, message = "Error updating supplier status" });
            }
        }
    }
}