using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Attributes;
using WMS.Data;
using WMS.Models;
using WMS.Services;
using WMS.Utilities;
using System.ComponentModel.DataAnnotations;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller untuk Customer management - Hybrid MVC + API
    /// </summary>
    [RequirePermission(Constants.CUSTOMER_VIEW)]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditTrailService _auditService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            IAuditTrailService auditService,
            ILogger<CustomerController> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _auditService = auditService;
            _logger = logger;
        }

        #region MVC Actions

        /// <summary>
        /// GET: /Customer
        /// Customer management index page
        /// </summary>
        public IActionResult Index()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer index page");
                return View("Error");
            }
        }

        #endregion

        #region Dashboard & Statistics

        /// <summary>
        /// GET: api/customer/dashboard
        /// Get customer statistics for dashboard
        /// </summary>
        [HttpGet("api/customer/dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var customers = await _context.Customers
                    .Where(c => c.CompanyId == companyId.Value && !c.IsDeleted)
                    .ToListAsync();

                var statistics = new
                {
                    totalCustomers = customers.Count,
                    activeCustomers = customers.Count(c => c.IsActive),
                    inactiveCustomers = customers.Count(c => !c.IsActive),
                    customersWithOrders = customers.Count(c => c.SalesOrders.Any()),
                    newCustomersThisMonth = customers.Count(c => c.CreatedDate >= DateTime.Now.AddMonths(-1)),
                    topCustomerType = customers.GroupBy(c => c.CustomerType)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? "Unknown"
                };

                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer dashboard statistics");
                return StatusCode(500, new { success = false, message = "Error loading dashboard statistics" });
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// GET: api/customer
        /// Get paginated list of customers with filters
        /// </summary>
        [HttpGet("api/customer")]
        public async Task<IActionResult> GetCustomers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? type = null)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var query = _context.Customers
                    .Where(c => c.CompanyId == companyId.Value && !c.IsDeleted)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c => 
                        c.Name.Contains(search) || 
                        c.Email.Contains(search) ||
                        c.Code.Contains(search) ||
                        (c.Phone != null && c.Phone.Contains(search)) ||
                        (c.Address != null && c.Address.Contains(search)));
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    switch (status.ToLower())
                    {
                        case "active":
                            query = query.Where(c => c.IsActive);
                            break;
                        case "inactive":
                            query = query.Where(c => !c.IsActive);
                            break;
                    }
                }

                // Apply type filter
                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(c => c.CustomerType == type);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var customers = await query
                    .OrderBy(c => c.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new
                    {
                        id = c.Id,
                        code = c.Code,
                        name = c.Name,
                        email = c.Email,
                        phone = c.Phone,
                        address = c.Address,
                        city = c.City,
                        customerType = c.CustomerType,
                        isActive = c.IsActive,
                        totalOrders = c.SalesOrders.Count,
                        totalValue = c.SalesOrders.Sum(so => so.TotalAmount),
                        createdDate = c.CreatedDate,
                        modifiedDate = c.ModifiedDate,
                        createdBy = c.CreatedBy,
                        modifiedBy = c.ModifiedBy
                    })
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return Ok(new
                {
                    success = true,
                    data = customers,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                return StatusCode(500, new { success = false, message = "Error loading customers" });
            }
        }

        /// <summary>
        /// GET: api/customer/{id}
        /// Get single customer by ID
        /// </summary>
        [HttpGet("api/customer/{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var customer = await _context.Customers
                    .Where(c => c.CompanyId == companyId.Value && c.Id == id && !c.IsDeleted)
                    .Select(c => new
                    {
                        id = c.Id,
                        code = c.Code,
                        name = c.Name,
                        email = c.Email,
                        phone = c.Phone,
                        address = c.Address,
                        city = c.City,
                        customerType = c.CustomerType,
                        isActive = c.IsActive,
                        totalOrders = c.SalesOrders.Count,
                        totalValue = c.SalesOrders.Sum(so => so.TotalAmount),
                        createdDate = c.CreatedDate,
                        modifiedDate = c.ModifiedDate,
                        createdBy = c.CreatedBy,
                        modifiedBy = c.ModifiedBy
                    })
                    .FirstOrDefaultAsync();

                if (customer == null)
                {
                    return NotFound(new { success = false, message = "Customer not found" });
                }

                return Ok(new { success = true, data = customer });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer: {CustomerId}", id);
                return StatusCode(500, new { success = false, message = "Error loading customer" });
            }
        }

        /// <summary>
        /// POST: api/customer
        /// Create new customer
        /// </summary>
        [HttpPost("api/customer")]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
                }

                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                // Validate required fields
                if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new { success = false, message = "Name and Email are required" });
                }

                // Check if email already exists
                var emailExists = await _context.Customers
                    .AnyAsync(c => c.CompanyId == companyId.Value && c.Email == request.Email && !c.IsDeleted);

                if (emailExists)
                {
                    return BadRequest(new { success = false, message = "A customer with this email already exists" });
                }

                // Check if phone already exists (if provided)
                if (!string.IsNullOrEmpty(request.Phone))
                {
                    var phoneExists = await _context.Customers
                        .AnyAsync(c => c.CompanyId == companyId.Value && c.Phone == request.Phone && !c.IsDeleted);

                    if (phoneExists)
                    {
                        return BadRequest(new { success = false, message = "A customer with this phone number already exists" });
                    }
                }

                // Generate customer code if not provided
                var customerCode = request.Code;
                if (string.IsNullOrEmpty(customerCode))
                {
                    var lastCustomer = await _context.Customers
                        .Where(c => c.CompanyId == companyId.Value)
                        .OrderByDescending(c => c.Id)
                        .FirstOrDefaultAsync();
                    
                    var nextId = (lastCustomer?.Id ?? 0) + 1;
                    customerCode = $"CUST{nextId:D4}";
                }

                var customer = new Customer
                {
                    Code = customerCode,
                    Name = request.Name,
                    Email = request.Email,
                    Phone = request.Phone,
                    Address = request.Address,
                    City = request.City,
                    CustomerType = request.CustomerType ?? "Individual",
                    IsActive = request.IsActive,
                    CompanyId = companyId.Value,
                    CreatedDate = DateTime.Now,
                    CreatedBy = _currentUserService.Username ?? "System"
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("CREATE", "Customer", customer.Id, 
                        $"{customer.Code} - {customer.Name}", null, new { 
                            Code = customer.Code, 
                            Name = customer.Name, 
                            Email = customer.Email,
                            Phone = customer.Phone,
                            CustomerType = customer.CustomerType,
                            IsActive = customer.IsActive 
                        });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for customer creation");
                }

                return Ok(new
                {
                    success = true,
                    message = $"Customer '{customer.Name}' created successfully",
                    data = new { id = customer.Id }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer: {Email} - Exception: {ExceptionMessage}", 
                    request.Email, ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                
                // Check if it's a unique constraint violation
                if (ex.InnerException?.Message.Contains("duplicate key") == true || 
                    ex.InnerException?.Message.Contains("unique constraint") == true)
                {
                    return BadRequest(new { success = false, message = "A customer with this email already exists" });
                }
                
                return StatusCode(500, new { 
                    success = false, 
                    message = "Error creating customer", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// PUT: api/customer/{id}
        /// Update existing customer
        /// </summary>
        [HttpPut("api/customer/{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerUpdateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                    
                    return BadRequest(new { 
                        success = false, 
                        message = "Validation failed", 
                        errors = errors 
                    });
                }

                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId.Value && c.Id == id && !c.IsDeleted);

                if (customer == null)
                {
                    return NotFound(new { success = false, message = "Customer not found" });
                }

                // Store old values for audit trail
                var oldValues = new {
                    Name = customer.Name,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Address = customer.Address,
                    City = customer.City,
                    CustomerType = customer.CustomerType,
                    IsActive = customer.IsActive
                };

                // Check if email already exists (excluding current customer)
                if (!string.IsNullOrEmpty(request.Email) && request.Email != customer.Email)
                {
                    var emailExists = await _context.Customers
                        .AnyAsync(c => c.CompanyId == companyId.Value && c.Email == request.Email && c.Id != id && !c.IsDeleted);

                    if (emailExists)
                    {
                        return BadRequest(new { success = false, message = "A customer with this email already exists" });
                    }
                }

                // Check if phone already exists (excluding current customer)
                if (!string.IsNullOrEmpty(request.Phone) && request.Phone != customer.Phone)
                {
                    var phoneExists = await _context.Customers
                        .AnyAsync(c => c.CompanyId == companyId.Value && c.Phone == request.Phone && c.Id != id && !c.IsDeleted);

                    if (phoneExists)
                    {
                        return BadRequest(new { success = false, message = "A customer with this phone number already exists" });
                    }
                }

                // Update customer properties
                if (!string.IsNullOrEmpty(request.Name))
                    customer.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Email))
                    customer.Email = request.Email;
                if (request.Phone != null)
                    customer.Phone = request.Phone;
                if (request.Address != null)
                    customer.Address = request.Address;
                if (request.City != null)
                    customer.City = request.City;
                if (!string.IsNullOrEmpty(request.CustomerType))
                    customer.CustomerType = request.CustomerType;
                if (request.IsActive.HasValue)
                    customer.IsActive = request.IsActive.Value;

                customer.ModifiedDate = DateTime.Now;
                customer.ModifiedBy = _currentUserService.Username ?? "System";

                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("UPDATE", "Customer", customer.Id, 
                        $"{customer.Code} - {customer.Name}", oldValues, new { 
                            Name = customer.Name,
                            Email = customer.Email,
                            Phone = customer.Phone,
                            Address = customer.Address,
                            City = customer.City,
                            CustomerType = customer.CustomerType,
                            IsActive = customer.IsActive 
                        });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for customer update");
                }

                return Ok(new
                {
                    success = true,
                    message = $"Customer '{customer.Name}' updated successfully",
                    data = new { id = customer.Id }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer: {CustomerId}", id);
                return StatusCode(500, new { success = false, message = "Error updating customer" });
            }
        }

        /// <summary>
        /// DELETE: api/customer/{id}
        /// Delete customer (soft delete)
        /// </summary>
        [HttpDelete("api/customer/{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId.Value && c.Id == id && !c.IsDeleted);

                if (customer == null)
                {
                    return NotFound(new { success = false, message = "Customer not found" });
                }

                // Check if customer has active sales orders
                var hasActiveOrders = await _context.SalesOrders
                    .AnyAsync(so => so.CustomerId == id && so.Status != "Completed" && so.Status != "Cancelled");

                if (hasActiveOrders)
                {
                    return BadRequest(new { success = false, message = "Cannot delete customer with active sales orders" });
                }

                // Soft delete
                customer.IsDeleted = true;
                customer.ModifiedDate = DateTime.Now;
                customer.ModifiedBy = _currentUserService.Username ?? "System";

                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("DELETE", "Customer", customer.Id, 
                        $"{customer.Code} - {customer.Name}", new { 
                            Code = customer.Code,
                            Name = customer.Name,
                            Email = customer.Email,
                            Phone = customer.Phone,
                            CustomerType = customer.CustomerType,
                            IsActive = customer.IsActive 
                        }, null);
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for customer deletion");
                }

                return Ok(new
                {
                    success = true,
                    message = $"Customer '{customer.Name}' deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer: {CustomerId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting customer" });
            }
        }

        #endregion

        #region Export Operations

        /// <summary>
        /// GET: api/customer/export
        /// Export customers to CSV
        /// </summary>
        [HttpGet("api/customer/export")]
        public async Task<IActionResult> ExportCustomers()
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var customers = await _context.Customers
                    .Where(c => c.CompanyId == companyId.Value && !c.IsDeleted)
                    .OrderBy(c => c.Name)
                    .Select(c => new
                    {
                        Code = c.Code,
                        Name = c.Name,
                        Email = c.Email,
                        Phone = c.Phone,
                        Address = c.Address,
                        City = c.City,
                        CustomerType = c.CustomerType,
                        IsActive = c.IsActive ? "Yes" : "No",
                        TotalOrders = c.SalesOrders.Count,
                        TotalValue = c.SalesOrders.Sum(so => so.TotalAmount),
                        CreatedDate = c.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        CreatedBy = c.CreatedBy
                    })
                    .ToListAsync();

                // Generate CSV content
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Code,Name,Email,Phone,Address,City,CustomerType,IsActive,TotalOrders,TotalValue,CreatedDate,CreatedBy");

                foreach (var customer in customers)
                {
                    csv.AppendLine($"{customer.Code},{customer.Name},{customer.Email},{customer.Phone},{customer.Address},{customer.City},{customer.CustomerType},{customer.IsActive},{customer.TotalOrders},{customer.TotalValue},{customer.CreatedDate},{customer.CreatedBy}");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                var fileName = $"Customers_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting customers");
                return StatusCode(500, new { success = false, message = "Error exporting customers" });
            }
        }

        #endregion
    }

    #region Request Models

    public class CustomerCreateRequest
    {
        public string? Code { get; set; }
        
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "Invalid phone format")]
        [MaxLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
        public string? Phone { get; set; }
        
        [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string? Address { get; set; }
        
        [MaxLength(50, ErrorMessage = "City cannot exceed 50 characters")]
        public string? City { get; set; }
        
        [MaxLength(20, ErrorMessage = "Customer type cannot exceed 20 characters")]
        public string? CustomerType { get; set; }
        
        public bool IsActive { get; set; } = true;
    }

    public class CustomerUpdateRequest
    {
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }
        
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string? Email { get; set; }
        
        [Phone(ErrorMessage = "Invalid phone format")]
        [MaxLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
        public string? Phone { get; set; }
        
        [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string? Address { get; set; }
        
        [MaxLength(50, ErrorMessage = "City cannot exceed 50 characters")]
        public string? City { get; set; }
        
        [MaxLength(20, ErrorMessage = "Customer type cannot exceed 20 characters")]
        public string? CustomerType { get; set; }
        
        public bool? IsActive { get; set; }
    }

    #endregion
}