using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Service untuk user management operations
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<UserService> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Get all users dalam company yang sama
        /// </summary>
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    _logger.LogWarning("No company context found for current user");
                    return new List<User>();
                }

                return await _context.Users
                    .Include(u => u.Company)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Where(u => u.CompanyId == companyId.Value)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users for company: {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Get user by ID (dengan company filtering)
        /// </summary>
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return null;
                }

                return await _context.Users
                    .Include(u => u.Company)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == companyId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get user by username (dengan company filtering)
        /// </summary>
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return null;
                }

                return await _context.Users
                    .Include(u => u.Company)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Username == username && u.CompanyId == companyId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Get user by email
        /// </summary>
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Company)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Create user baru dalam company yang sama
        /// </summary>
        public async Task<CreateUserResult> CreateUserAsync(User user, string password, IEnumerable<string> roleNames)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return new CreateUserResult
                    {
                        Success = false,
                        ErrorMessage = "No company context found"
                    };
                }

                // Validate password
                var passwordValidation = PasswordHelper.ValidatePassword(password);
                if (!passwordValidation.IsValid)
                {
                    return new CreateUserResult
                    {
                        Success = false,
                        ErrorMessage = "Password tidak valid",
                        ValidationErrors = passwordValidation.Errors
                    };
                }

                // Check username availability
                if (!await IsUsernameAvailableAsync(user.Username))
                {
                    return new CreateUserResult
                    {
                        Success = false,
                        ErrorMessage = "Username sudah digunakan"
                    };
                }

                // Check email availability
                if (!await IsEmailAvailableAsync(user.Email))
                {
                    return new CreateUserResult
                    {
                        Success = false,
                        ErrorMessage = "Email sudah digunakan"
                    };
                }

                // Set company dan hash password
                user.CompanyId = companyId.Value;
                user.HashedPassword = PasswordHelper.HashPassword(password);
                user.CreatedDate = DateTime.Now;
                user.CreatedBy = _currentUserService.Username;
                user.EmailVerified = true; // Auto verify untuk internal users

                // Add user
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Assign roles
                if (roleNames.Any())
                {
                    await AssignRolesToUserAsync(user.Id, roleNames);
                }

                _logger.LogInformation("User created: {Username} for company: {CompanyId}", user.Username, companyId.Value);

                return new CreateUserResult
                {
                    Success = true,
                    User = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", user.Username);
                return new CreateUserResult
                {
                    Success = false,
                    ErrorMessage = "Terjadi kesalahan sistem"
                };
            }
        }

        /// <summary>
        /// Update user data
        /// </summary>
        public async Task<UpdateUserResult> UpdateUserAsync(User user)
        {
            try
            {
                var existingUser = await GetUserByIdAsync(user.Id);
                if (existingUser == null)
                {
                    return new UpdateUserResult
                    {
                        Success = false,
                        ErrorMessage = "User tidak ditemukan"
                    };
                }

                // Check username availability (exclude current user)
                if (!await IsUsernameAvailableAsync(user.Username, user.Id))
                {
                    return new UpdateUserResult
                    {
                        Success = false,
                        ErrorMessage = "Username sudah digunakan"
                    };
                }

                // Check email availability (exclude current user)
                if (!await IsEmailAvailableAsync(user.Email, user.Id))
                {
                    return new UpdateUserResult
                    {
                        Success = false,
                        ErrorMessage = "Email sudah digunakan"
                    };
                }

                // Update fields (exclude sensitive fields)
                existingUser.Username = user.Username;
                existingUser.Email = user.Email;
                existingUser.FullName = user.FullName;
                existingUser.Phone = user.Phone;
                existingUser.ModifiedDate = DateTime.Now;
                existingUser.ModifiedBy = _currentUserService.Username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User updated: {Username}", user.Username);

                return new UpdateUserResult
                {
                    Success = true,
                    User = existingUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", user.Id);
                return new UpdateUserResult
                {
                    Success = false,
                    ErrorMessage = "Terjadi kesalahan sistem"
                };
            }
        }

        /// <summary>
        /// Delete/deactivate user
        /// </summary>
        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // Soft delete - just deactivate
                user.IsActive = false;
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = _currentUserService.Username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User deactivated: {Username}", user.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Reset password user (admin function)
        /// </summary>
        public async Task<ResetPasswordResult> ResetUserPasswordAsync(int userId, string newPassword)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new ResetPasswordResult
                    {
                        Success = false,
                        ErrorMessage = "User tidak ditemukan"
                    };
                }

                // Validate new password
                var validation = PasswordHelper.ValidatePassword(newPassword);
                if (!validation.IsValid)
                {
                    return new ResetPasswordResult
                    {
                        Success = false,
                        ErrorMessage = string.Join(", ", validation.Errors)
                    };
                }

                // Update password
                user.HashedPassword = PasswordHelper.HashPassword(newPassword);
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = _currentUserService.Username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset for user: {Username} by admin: {AdminUsername}",
                    user.Username, _currentUserService.Username);

                return new ResetPasswordResult
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user: {UserId}", userId);
                return new ResetPasswordResult
                {
                    Success = false,
                    ErrorMessage = "Terjadi kesalahan sistem"
                };
            }
        }

        /// <summary>
        /// Activate/deactivate user
        /// </summary>
        public async Task<bool> SetUserActiveStatusAsync(int userId, bool isActive)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.IsActive = isActive;
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = _currentUserService.Username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Username} status changed to: {Status}", user.Username, isActive ? "Active" : "Inactive");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting user status: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Assign roles to user
        /// </summary>
        public async Task<bool> AssignRolesToUserAsync(int userId, IEnumerable<string> roleNames)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // Get roles by names
                var roles = await _context.Roles
                    .Where(r => roleNames.Contains(r.Name) && r.IsActive)
                    .ToListAsync();

                // Remove existing roles
                var existingUserRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .ToListAsync();

                _context.UserRoles.RemoveRange(existingUserRoles);

                // Add new roles
                var newUserRoles = roles.Select(role => new UserRole
                {
                    UserId = userId,
                    RoleId = role.Id,
                    AssignedDate = DateTime.Now,
                    AssignedBy = _currentUserService.Username,
                    CreatedDate = DateTime.Now,
                    CreatedBy = _currentUserService.Username
                }).ToList();

                _context.UserRoles.AddRange(newUserRoles);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Roles assigned to user {Username}: {Roles}",
                    user.Username, string.Join(", ", roleNames));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning roles to user: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Remove roles from user
        /// </summary>
        public async Task<bool> RemoveRolesFromUserAsync(int userId, IEnumerable<string> roleNames)
        {
            try
            {
                var rolesToRemove = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .Where(ur => ur.UserId == userId && roleNames.Contains(ur.Role!.Name))
                    .ToListAsync();

                _context.UserRoles.RemoveRange(rolesToRemove);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Roles removed from user {UserId}: {Roles}",
                    userId, string.Join(", ", roleNames));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing roles from user: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Get available roles untuk assignment
        /// </summary>
        public async Task<IEnumerable<Role>> GetAvailableRolesAsync()
        {
            try
            {
                // Exclude SuperAdmin role from normal assignment
                return await _context.Roles
                    .Where(r => r.IsActive && r.Name != "SuperAdmin")
                    .OrderBy(r => r.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available roles");
                throw;
            }
        }

        /// <summary>
        /// Check username availability dalam company
        /// </summary>
        public async Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return false;
                }

                var query = _context.Users
                    .Where(u => u.Username == username && u.CompanyId == companyId.Value && !u.IsDeleted);

                if (excludeUserId.HasValue)
                {
                    query = query.Where(u => u.Id != excludeUserId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username availability: {Username}", username);
                return false;
            }
        }

        /// <summary>
        /// Check email availability (global)
        /// </summary>
        public async Task<bool> IsEmailAvailableAsync(string email, int? excludeUserId = null)
        {
            try
            {
                var query = _context.Users.Where(u => u.Email == email && !u.IsDeleted);

                if (excludeUserId.HasValue)
                {
                    query = query.Where(u => u.Id != excludeUserId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email availability: {Email}", email);
                return false;
            }
        }

        /// <summary>
        /// Get user statistics untuk dashboard
        /// </summary>
        public async Task<UserStatistics> GetUserStatisticsAsync()
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return new UserStatistics();
                }

                var users = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Where(u => u.CompanyId == companyId.Value)
                    .ToListAsync();

                var lastLoginUser = users
                    .Where(u => u.LastLoginDate.HasValue)
                    .OrderByDescending(u => u.LastLoginDate)
                    .FirstOrDefault();

                return new UserStatistics
                {
                    TotalUsers = users.Count,
                    ActiveUsers = users.Count(u => u.IsActive),
                    InactiveUsers = users.Count(u => !u.IsActive),
                    AdminUsers = users.Count(u => u.UserRoles.Any(ur => ur.Role!.Name == "Admin")),
                    ManagerUsers = users.Count(u => u.UserRoles.Any(ur => ur.Role!.Name == "Manager")),
                    RegularUsers = users.Count(u => u.UserRoles.Any(ur => ur.Role!.Name == "User" || ur.Role!.Name == "Operator")),
                    LastLogin = lastLoginUser?.LastLoginDate ?? DateTime.MinValue,
                    LastLoginUser = lastLoginUser?.FullName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user statistics for company: {CompanyId}", _currentUserService.CompanyId);
                return new UserStatistics();
            }
        }

        /// <summary>
        /// Get user roles by user ID
        /// </summary>
        public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
        {
            try
            {
                var userRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Include(ur => ur.Role)
                    .Where(ur => ur.Role.IsActive)
                    .Select(ur => ur.Role.Name)
                    .ToListAsync();

                return userRoles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles for user: {UserId}", userId);
                return new List<string>();
            }
        }
    }
}