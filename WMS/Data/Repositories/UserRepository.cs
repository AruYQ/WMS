using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;
using WMS.Services;

namespace WMS.Data.Repositories
{
    /// <summary>
    /// Repository untuk User dengan company filtering
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<UserRepository> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Get base query dengan company filtering
        /// </summary>
        private IQueryable<User> GetBaseQuery()
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue)
            {
                _logger.LogWarning("No company ID found for current user");
                return _context.Users.Where(x => false); // Return empty query
            }

            return _context.Users
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.CompanyId == companyId.Value);
        }

        /// <summary>
        /// Get all users dalam company yang sama
        /// </summary>
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            try
            {
                return await GetBaseQuery().OrderBy(u => u.FullName).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users for company: {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Get user by ID dengan company filtering
        /// </summary>
        public async Task<User?> GetByIdAsync(int id)
        {
            try
            {
                return await GetBaseQuery().FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Get user by username dalam company yang sama
        /// </summary>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            try
            {
                return await GetBaseQuery().FirstOrDefaultAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Get user by email (global search)
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
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
        /// Get user by username or email untuk login
        /// </summary>
        public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Company)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u =>
                        (u.Username == usernameOrEmail || u.Email == usernameOrEmail) &&
                        u.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username or email: {UsernameOrEmail}", usernameOrEmail);
                throw;
            }
        }

        /// <summary>
        /// Create user baru
        /// </summary>
        public async Task<User> CreateAsync(User user)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    throw new InvalidOperationException("No company context found");
                }

                user.CompanyId = companyId.Value;
                user.CreatedDate = DateTime.Now;
                user.CreatedBy = _currentUserService.Username;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created: {Username} for company: {CompanyId}", user.Username, companyId.Value);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", user.Username);
                throw;
            }
        }

        /// <summary>
        /// Update user data
        /// </summary>
        public async Task<User> UpdateAsync(User user)
        {
            try
            {
                var existingUser = await GetByIdAsync(user.Id);
                if (existingUser == null)
                {
                    throw new InvalidOperationException($"User with ID {user.Id} not found or does not belong to current company");
                }

                // Update fields
                _context.Entry(existingUser).CurrentValues.SetValues(user);
                existingUser.ModifiedDate = DateTime.Now;
                existingUser.ModifiedBy = _currentUserService.Username;

                // Ensure CompanyId tidak berubah
                existingUser.CompanyId = _currentUserService.CompanyId!.Value;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User updated: {Username}", user.Username);
                return existingUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Delete user (soft delete)
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var user = await GetByIdAsync(id);
                if (user == null)
                {
                    return false;
                }

                user.IsActive = false;
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = _currentUserService.Username;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User soft deleted: {Username}", user.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                return false;
            }
        }

        /// <summary>
        /// Check username exists dalam company
        /// </summary>
        public async Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null)
        {
            try
            {
                var query = GetBaseQuery().Where(u => u.Username == username);

                if (excludeUserId.HasValue)
                {
                    query = query.Where(u => u.Id != excludeUserId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username exists: {Username}", username);
                return false;
            }
        }

        /// <summary>
        /// Check email exists (global)
        /// </summary>
        public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
        {
            try
            {
                var query = _context.Users.Where(u => u.Email == email);

                if (excludeUserId.HasValue)
                {
                    query = query.Where(u => u.Id != excludeUserId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email exists: {Email}", email);
                return false;
            }
        }

        /// <summary>
        /// Get users by role dalam company
        /// </summary>
        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName)
        {
            try
            {
                return await GetBaseQuery()
                    .Where(u => u.UserRoles.Any(ur => ur.Role!.Name == roleName && ur.Role.IsActive))
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role: {RoleName}", roleName);
                throw;
            }
        }

        /// <summary>
        /// Get user count dalam company
        /// </summary>
        public async Task<int> GetUserCountAsync()
        {
            try
            {
                return await GetBaseQuery().CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user count for company: {CompanyId}", _currentUserService.CompanyId);
                return 0;
            }
        }

        /// <summary>
        /// Get active user count dalam company
        /// </summary>
        public async Task<int> GetActiveUserCountAsync()
        {
            try
            {
                return await GetBaseQuery().CountAsync(u => u.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active user count for company: {CompanyId}", _currentUserService.CompanyId);
                return 0;
            }
        }
    }
}