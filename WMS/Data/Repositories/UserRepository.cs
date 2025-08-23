using Microsoft.EntityFrameworkCore;
using WMS.Models;

namespace WMS.Data.Repositories
{
    /// <summary>
    /// Repository implementation untuk User entity
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all users dalam company
        /// </summary>
        public async Task<IEnumerable<User>> GetAllByCompanyIdAsync(int companyId)
        {
            try
            {
                return await _context.Users
                    .Where(u => u.CompanyId == companyId)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users for company {CompanyId}", companyId);
                throw;
            }
        }

        /// <summary>
        /// Get user by ID dengan company validation
        /// </summary>
        public async Task<User?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Company)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Get user by username
        /// </summary>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Get user by email
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Get user dengan roles
        /// </summary>
        public async Task<User?> GetWithRolesAsync(int id)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Company)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with roles by ID {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Get user by username atau email dengan roles
        /// </summary>
        public async Task<User?> GetByUsernameOrEmailWithRolesAsync(string usernameOrEmail)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Company)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username or email {UsernameOrEmail}", usernameOrEmail);
                throw;
            }
        }

        /// <summary>
        /// Add user baru
        /// </summary>
        public async Task<User> AddAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            try
            {
                user.CreatedDate = DateTime.Now;
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added new user {Username} (ID: {UserId}) for company {CompanyId}",
                    user.Username, user.Id, user.CompanyId);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {Username} for company {CompanyId}",
                    user.Username, user.CompanyId);
                throw;
            }
        }

        /// <summary>
        /// Update user
        /// </summary>
        public async Task<User> UpdateAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            try
            {
                var existingUser = await _context.Users.FindAsync(user.Id);
                if (existingUser == null)
                {
                    throw new InvalidOperationException($"User with ID {user.Id} not found");
                }

                // Update fields
                existingUser.Username = user.Username;
                existingUser.Email = user.Email;
                existingUser.FullName = user.FullName;
                existingUser.Phone = user.Phone;
                existingUser.IsActive = user.IsActive;
                existingUser.ModifiedDate = DateTime.Now;
                existingUser.ModifiedBy = user.ModifiedBy;

                // Don't update password fields here - use separate method
                // Don't update CompanyId - should not be changed

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated user {Username} (ID: {UserId})", user.Username, user.Id);

                return existingUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {Username} (ID: {UserId})", user.Username, user.Id);
                throw;
            }
        }

        /// <summary>
        /// Delete user (soft delete - set IsActive = false)
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return false;
                }

                // Soft delete
                user.IsActive = false;
                user.ModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft deleted user {Username} (ID: {UserId})", user.Username, id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Check if username exists dalam company
        /// </summary>
        public async Task<bool> ExistsByUsernameAsync(string username, int companyId, int? excludeUserId = null)
        {
            try
            {
                var query = _context.Users.Where(u => u.Username == username && u.CompanyId == companyId);

                if (excludeUserId.HasValue)
                {
                    query = query.Where(u => u.Id != excludeUserId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username existence {Username} for company {CompanyId}", username, companyId);
                throw;
            }
        }

        /// <summary>
        /// Check if email exists dalam company
        /// </summary>
        public async Task<bool> ExistsByEmailAsync(string email, int companyId, int? excludeUserId = null)
        {
            try
            {
                var query = _context.Users.Where(u => u.Email == email && u.CompanyId == companyId);

                if (excludeUserId.HasValue)
                {
                    query = query.Where(u => u.Id != excludeUserId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence {Email} for company {CompanyId}", email, companyId);
                throw;
            }
        }

        /// <summary>
        /// Get active users dalam company
        /// </summary>
        public async Task<IEnumerable<User>> GetActiveUsersByCompanyIdAsync(int companyId)
        {
            try
            {
                return await _context.Users
                    .Where(u => u.CompanyId == companyId && u.IsActive)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active users for company {CompanyId}", companyId);
                throw;
            }
        }

        /// <summary>
        /// Get users by role dalam company
        /// </summary>
        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName, int companyId)
        {
            try
            {
                return await _context.Users
                    .Where(u => u.CompanyId == companyId && u.IsActive &&
                               u.UserRoles.Any(ur => ur.Role!.Name == roleName))
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role {RoleName} for company {CompanyId}", roleName, companyId);
                throw;
            }
        }

        /// <summary>
        /// Count users dalam company
        /// </summary>
        public async Task<int> CountUsersByCompanyIdAsync(int companyId)
        {
            try
            {
                return await _context.Users.CountAsync(u => u.CompanyId == companyId && u.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting users for company {CompanyId}", companyId);
                throw;
            }
        }

        /// <summary>
        /// Search users dalam company
        /// </summary>
        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int companyId)
        {
            try
            {
                return await _context.Users
                    .Where(u => u.CompanyId == companyId &&
                               (u.FullName.Contains(searchTerm) ||
                                u.Username.Contains(searchTerm) ||
                                u.Email.Contains(searchTerm)))
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term {SearchTerm} for company {CompanyId}", searchTerm, companyId);
                throw;
            }
        }

        /// <summary>
        /// Get paginated users dalam company
        /// </summary>
        public async Task<PagedResult<User>> GetPagedByCompanyIdAsync(int companyId, int pageNumber, int pageSize, string? searchTerm = null)
        {
            try
            {
                var query = _context.Users
                    .Where(u => u.CompanyId == companyId)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(u => u.FullName.Contains(searchTerm) ||
                                           u.Username.Contains(searchTerm) ||
                                           u.Email.Contains(searchTerm));
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var items = await query
                    .OrderBy(u => u.FullName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new PagedResult<User>
                {
                    Items = items,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged users for company {CompanyId}", companyId);
                throw;
            }
        }
    }
}