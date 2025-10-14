using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Attributes;
using WMS.Data;
using WMS.Utilities;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller untuk user management dalam company - Hybrid MVC + API
    /// MVC actions use default routing (/User)
    /// API actions use explicit routing (/api/user/*)
    /// </summary>
    [RequirePermission(Constants.USER_VIEW)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditTrailService _auditService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            ApplicationDbContext context,
            IUserService userService,
            ICurrentUserService currentUserService,
            IAuditTrailService auditService,
            ILogger<UserController> logger)
        {
            _context = context;
            _userService = userService;
            _currentUserService = currentUserService;
            _auditService = auditService;
            _logger = logger;
        }

        #region MVC Actions

        /// <summary>
        /// GET: /User
        /// User management index page
        /// </summary>
        public IActionResult Index()
        {
            try
            {
                // Pass current user ID to view for frontend validation
                ViewBag.CurrentUserId = _currentUserService.UserId;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user index page");
                return View("Error");
            }
        }

        #endregion

        #region Dashboard & Statistics

        /// <summary>
        /// GET: api/user/dashboard
        /// Get user statistics for dashboard
        /// </summary>
        [HttpGet("api/user/dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var users = await _context.Users
                    .Where(u => u.CompanyId == companyId.Value && !u.IsDeleted)
                    .ToListAsync();

                var statistics = new
                {
                    totalUsers = users.Count,
                    activeUsers = users.Count(u => u.IsActive),
                    inactiveUsers = users.Count(u => !u.IsActive),
                    adminUsers = users.Count(u => u.IsAdmin),
                    warehouseStaffUsers = users.Count(u => u.RoleNames.Contains("WarehouseStaff")),
                    recentUsers = users.Count(u => u.CreatedDate >= DateTime.Now.AddDays(-30))
                };

                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user dashboard statistics");
                return StatusCode(500, new { success = false, message = "Error loading dashboard statistics" });
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// GET: api/user
        /// Get paginated list of users with filters
        /// </summary>
        [HttpGet("api/user")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? role = null)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var query = _context.Users
                    .Where(u => u.CompanyId == companyId.Value && !u.IsDeleted && u.Username != "superadmin")
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => 
                        u.Username.Contains(search) || 
                        u.FullName.Contains(search) ||
                        u.Email.Contains(search));
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    switch (status.ToLower())
                    {
                        case "active":
                            query = query.Where(u => u.IsActive);
                            break;
                        case "inactive":
                            query = query.Where(u => !u.IsActive);
                            break;
                    }
                }

                // Apply role filter
                if (!string.IsNullOrEmpty(role))
                {
                    query = query.Where(u => u.RoleNames.Contains(role));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var currentUserId = _currentUserService.UserId;
                var users = await query
                    .OrderBy(u => u.Username)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        id = u.Id,
                        username = u.Username,
                        email = u.Email,
                        fullName = u.FullName,
                        phone = u.Phone,
                        isActive = u.IsActive,
                        isAdmin = u.IsAdmin,
                        roleNames = u.RoleNames.ToList(),
                        lastLoginDate = u.LastLoginDate,
                        createdDate = u.CreatedDate,
                        modifiedDate = u.ModifiedDate,
                        createdBy = u.CreatedBy,
                        isSelfEdit = u.Id == currentUserId
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items = users,
                        totalCount = totalCount,
                        totalPages = totalPages,
                        currentPage = page,
                        pageSize = pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users list");
                return StatusCode(500, new { success = false, message = "Error loading users" });
            }
        }

        /// <summary>
        /// GET: api/user/{id}
        /// Get single user by ID
        /// </summary>
        [HttpGet("api/user/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var user = await _context.Users
                    .Where(u => u.CompanyId == companyId.Value && u.Id == id && !u.IsDeleted)
                    .Select(u => new
                    {
                        id = u.Id,
                        username = u.Username,
                        email = u.Email,
                        fullName = u.FullName,
                        phone = u.Phone,
                        isActive = u.IsActive,
                        isAdmin = u.IsAdmin,
                        roleNames = u.RoleNames.ToList(),
                        lastLoginDate = u.LastLoginDate,
                        createdDate = u.CreatedDate,
                        modifiedDate = u.ModifiedDate,
                        createdBy = u.CreatedBy,
                        modifiedBy = u.ModifiedBy
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                return Ok(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", id);
                return StatusCode(500, new { success = false, message = "Error loading user" });
            }
        }

        /// <summary>
        /// POST: api/user
        /// Create new user
        /// </summary>
        [HttpPost("api/user")]
        [RequirePermission(Constants.USER_MANAGE)]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest request)
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

                // Validate username uniqueness
                var existingUsername = await _context.Users
                    .Where(u => u.CompanyId == companyId.Value && u.Username == request.Username && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingUsername != null)
                {
                    return BadRequest(new { success = false, message = "Username already exists" });
                }

                // Validate email uniqueness
                var existingEmail = await _context.Users
                    .Where(u => u.CompanyId == companyId.Value && u.Email == request.Email && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingEmail != null)
                {
                    return BadRequest(new { success = false, message = "Email already exists" });
                }

                // Validate password
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { success = false, message = "Password is required" });
                }

                var passwordValidation = PasswordHelper.ValidatePassword(request.Password);
                if (!passwordValidation.IsValid)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Password tidak valid", 
                        errors = passwordValidation.Errors 
                    });
                }

                // Create user entity
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    FullName = request.FullName,
                    Phone = request.Phone,
                    IsActive = request.IsActive,
                    EmailVerified = true,
                    HashedPassword = PasswordHelper.HashPassword(request.Password),
                    CompanyId = companyId.Value,
                    CreatedDate = DateTime.Now,
                    CreatedBy = _currentUserService.Username ?? "System"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Auto assign WarehouseStaff role
                await _userService.AssignRolesToUserAsync(user.Id, new List<string> { "WarehouseStaff" });

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("CREATE", "User", user.Id, 
                        $"{user.Username} - {user.FullName}", null, new { 
                            Username = user.Username, 
                            Email = user.Email, 
                            FullName = user.FullName,
                            IsActive = user.IsActive,
                            Roles = request.Roles ?? new List<string>()
                        });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for user creation");
                }

                return Ok(new
                {
                    success = true,
                    message = $"User '{user.Username}' created successfully",
                    data = new { id = user.Id }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username} - Exception: {ExceptionMessage}", 
                    request.Username, ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                
                // Check if it's a unique constraint violation
                if (ex.InnerException?.Message.Contains("duplicate key") == true || 
                    ex.InnerException?.Message.Contains("unique constraint") == true)
                {
                    if (ex.InnerException.Message.Contains("Username"))
                    {
                        return BadRequest(new { success = false, message = "Username already exists" });
                    }
                    else if (ex.InnerException.Message.Contains("Email"))
                    {
                        return BadRequest(new { success = false, message = "Email already exists" });
                }
                else
                {
                        return BadRequest(new { success = false, message = "User data already exists" });
                    }
                }
                
                return StatusCode(500, new { 
                    success = false, 
                    message = "Error creating user", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// PUT: api/user/{id}
        /// Update existing user
        /// </summary>
        [HttpPut("api/user/{id}")]
        [RequirePermission(Constants.USER_MANAGE)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateRequest request)
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

                var user = await _context.Users
                    .Where(u => u.CompanyId == companyId.Value && u.Id == id && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Prevent admin from editing their own role
                if (user.Id == _currentUserService.UserId && request.Roles != null)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "You cannot modify your own roles. Please contact another admin." 
                    });
                }

                // Validate username uniqueness (excluding current user)
                var existingUsername = await _context.Users
                    .Where(u => u.CompanyId == companyId.Value && u.Username == request.Username && u.Id != id && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingUsername != null)
                {
                    return BadRequest(new { success = false, message = "Username already exists" });
                }

                // Validate email uniqueness (excluding current user)
                var existingEmail = await _context.Users
                    .Where(u => u.CompanyId == companyId.Value && u.Email == request.Email && u.Id != id && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingEmail != null)
                {
                    return BadRequest(new { success = false, message = "Email already exists" });
                }

                // Store old values for audit trail
                var oldValues = new {
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Phone = user.Phone,
                    IsActive = user.IsActive
                };

                // Update user
                user.Username = request.Username;
                user.Email = request.Email;
                user.FullName = request.FullName;
                user.Phone = request.Phone;
                user.IsActive = request.IsActive;

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    var passwordValidation = PasswordHelper.ValidatePassword(request.NewPassword);
                    if (!passwordValidation.IsValid)
                    {
                        return BadRequest(new { 
                            success = false, 
                            message = "Password tidak valid", 
                            errors = passwordValidation.Errors 
                        });
                    }

                    user.HashedPassword = PasswordHelper.HashPassword(request.NewPassword);
                }

                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = _currentUserService.Username ?? "System";

                await _context.SaveChangesAsync();

                // Auto assign WarehouseStaff role
                await _userService.AssignRolesToUserAsync(id, new List<string> { "WarehouseStaff" });

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("UPDATE", "User", user.Id, 
                        $"{user.Username} - {user.FullName}", oldValues, new { 
                            Username = user.Username, 
                            Email = user.Email, 
                            FullName = user.FullName,
                            Phone = user.Phone,
                            IsActive = user.IsActive,
                            Roles = request.Roles ?? new List<string>()
                        });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for user update");
                }

                return Ok(new
                {
                    success = true,
                    message = $"User '{user.Username}' updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new { success = false, message = "Error updating user" });
            }
        }

        /// <summary>
        /// DELETE: api/user/{id}
        /// Delete user
        /// </summary>
        [HttpDelete("api/user/{id}")]
        [RequirePermission(Constants.USER_MANAGE)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var user = await _context.Users
                    .Where(u => u.CompanyId == companyId.Value && u.Id == id && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Check if user is trying to delete themselves
                if (user.Id == _currentUserService.UserId)
                {
                    return BadRequest(new { success = false, message = "Cannot delete your own account" });
                }

                // Store user data for audit trail
                var userData = new {
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Phone = user.Phone,
                    IsActive = user.IsActive
                };

                // Soft delete - mark as deleted instead of removing from database
                user.IsDeleted = true;
                user.DeletedDate = DateTime.Now;
                user.DeletedBy = _currentUserService.Username ?? "System";
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = _currentUserService.Username ?? "System";
                
                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("DELETE", "User", user.Id, 
                        $"{user.Username} - {user.FullName}", userData, null);
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for user deletion");
                }

                return Ok(new
                {
                    success = true,
                    message = $"User '{user.Username}' deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting user" });
            }
        }

        #endregion

        #region Special Operations

        /// <summary>
        /// PATCH: api/user/{id}/toggle-status
        /// Toggle user active status
        /// </summary>
        [HttpPatch("api/user/{id}/toggle-status")]
        [RequirePermission(Constants.USER_MANAGE)]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var user = await _context.Users
                    .Where(u => u.CompanyId == companyId.Value && u.Id == id && !u.IsDeleted)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                var oldStatus = user.IsActive;
                user.IsActive = !user.IsActive;
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = _currentUserService.Username ?? "System";

                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    var action = user.IsActive ? "ACTIVATE" : "DEACTIVATE";
                    var statusText = user.IsActive ? "activated" : "deactivated";
                    await _auditService.LogActionAsync(action, "User", user.Id, 
                        $"{user.Username} - {user.FullName}", new { IsActive = oldStatus }, new { IsActive = user.IsActive });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for user status toggle");
                }

                var status = user.IsActive ? "activated" : "deactivated";
                return Ok(new
                {
                    success = true,
                    message = $"User '{user.Username}' has been {status} successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status {UserId}", id);
                return StatusCode(500, new { success = false, message = "Error updating user status" });
            }
        }

        #endregion

        #region Validation & Utilities

        /// <summary>
        /// GET: api/user/check-username
        /// Check if username is unique
        /// </summary>
        [HttpGet("api/user/check-username")]
        public async Task<IActionResult> CheckUsername([FromQuery] string username, [FromQuery] int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    return Ok(new { isUnique = false, message = "Username is required" });
                }

                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var query = _context.Users
                    .Where(u => u.CompanyId == companyId.Value && u.Username == username && !u.IsDeleted);

                if (excludeId.HasValue)
                {
                    query = query.Where(u => u.Id != excludeId.Value);
                }

                var existingUser = await query.FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    return Ok(new { isUnique = false, message = "Username already exists" });
                }

                return Ok(new { isUnique = true, message = "Username is available" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username uniqueness: {Username}", username);
                return StatusCode(500, new { isUnique = false, message = "Error checking username" });
            }
        }

        /// <summary>
        /// GET: api/user/check-email
        /// Check if email is unique
        /// </summary>
        [HttpGet("api/user/check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email, [FromQuery] int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return Ok(new { isUnique = false, message = "Email is required" });
                }

                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "No company context found" });
                }

                var query = _context.Users
                    .Where(u => u.CompanyId == companyId.Value && u.Email == email && !u.IsDeleted);

                if (excludeId.HasValue)
                {
                    query = query.Where(u => u.Id != excludeId.Value);
                }

                var existingUser = await query.FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    return Ok(new { isUnique = false, message = "Email already exists" });
                }

                return Ok(new { isUnique = true, message = "Email is available" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email uniqueness: {Email}", email);
                return StatusCode(500, new { isUnique = false, message = "Error checking email" });
            }
        }

        /// <summary>
        /// PATCH: api/user/change-password
        /// Admin change password sendiri (auto logout after success)
        /// </summary>
        [HttpPatch("api/user/change-password")]
        [RequirePermission(Constants.USER_VIEW)]
        public async Task<IActionResult> ChangeOwnPassword([FromBody] ChangeOwnPasswordRequest request)
        {
            try
            {
                var userId = _currentUserService.UserId;
                if (!userId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null || user.IsDeleted)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Verify current password
                if (!PasswordHelper.VerifyPassword(request.CurrentPassword, user.HashedPassword))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Current password is incorrect" 
                    });
                }

                // Validate new password
                var passwordValidation = PasswordHelper.ValidatePassword(request.NewPassword);
                if (!passwordValidation.IsValid)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Password tidak valid", 
                        errors = passwordValidation.Errors 
                    });
                }

                // Update password
                user.HashedPassword = PasswordHelper.HashPassword(request.NewPassword);
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = user.Username;

                await _context.SaveChangesAsync();

                // Log audit trail
                try
                {
                    await _auditService.LogActionAsync("UPDATE", "User", user.Id, 
                        $"{user.Username} - Changed own password", null, new { 
                            Action = "PasswordChanged",
                            ChangedBy = user.Username
                        });
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Failed to log audit trail for password change");
                }

                _logger.LogInformation("User {Username} changed their own password", user.Username);

                // Return success with flag to trigger logout
                return Ok(new { 
                    success = true, 
                    message = "Password updated successfully. You will be logged out.", 
                    requireLogout = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", _currentUserService.UserId);
                return StatusCode(500, new { success = false, message = "Failed to change password" });
            }
        }

        /// <summary>
        /// GET: api/user/roles
        /// Get available roles for dropdown
        /// </summary>
        [HttpGet("api/user/roles")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var currentUserRoles = _currentUserService.Roles;
                var allowedRoles = new List<string>();
                
                // Admin hanya bisa assign WarehouseStaff
                if (currentUserRoles.Contains("Admin"))
                {
                    allowedRoles.Add("WarehouseStaff");
                }
                // SuperAdmin bisa assign semua roles
                else if (currentUserRoles.Contains("SuperAdmin"))
                {
                    allowedRoles = new List<string> { "Admin", "WarehouseStaff" };
                }
                
                var roles = await _context.Roles
                    .Where(r => r.IsActive && allowedRoles.Contains(r.Name))
                    .Select(r => new
                    {
                        name = r.Name,
                        description = r.Description
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = roles });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                return StatusCode(500, new { success = false, message = "Error loading roles" });
            }
        }

        #endregion
    }

    #region Request Models

    public class UserCreateRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public List<string>? Roles { get; set; }
        public string Password { get; set; } = string.Empty; // NEW: Required password
    }

    public class UserUpdateRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public List<string>? Roles { get; set; }
        public string? NewPassword { get; set; } // NEW: Optional untuk change password
    }

    public class ChangeOwnPasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    #endregion
}