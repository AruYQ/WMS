using WMS.Models;
using System.Linq.Expressions;

namespace WMS.Data.Repositories
{
    /// <summary>
    /// Repository interface untuk User entity
    /// User tidak menggunakan generic repository karena tidak inherit dari BaseEntity
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Get all users dalam company
        /// </summary>
        Task<IEnumerable<User>> GetAllByCompanyIdAsync(int companyId);

        /// <summary>
        /// Get user by ID dengan company validation
        /// </summary>
        Task<User?> GetByIdAsync(int id);

        /// <summary>
        /// Get user by username
        /// </summary>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// Get user by email
        /// </summary>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Get user dengan roles
        /// </summary>
        Task<User?> GetWithRolesAsync(int id);

        /// <summary>
        /// Get user by username atau email dengan roles
        /// </summary>
        Task<User?> GetByUsernameOrEmailWithRolesAsync(string usernameOrEmail);

        /// <summary>
        /// Add user baru
        /// </summary>
        Task<User> AddAsync(User user);

        /// <summary>
        /// Update user
        /// </summary>
        Task<User> UpdateAsync(User user);

        /// <summary>
        /// Delete user (soft delete - set IsActive = false)
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Check if username exists dalam company
        /// </summary>
        Task<bool> ExistsByUsernameAsync(string username, int companyId, int? excludeUserId = null);

        /// <summary>
        /// Check if email exists dalam company
        /// </summary>
        Task<bool> ExistsByEmailAsync(string email, int companyId, int? excludeUserId = null);

        /// <summary>
        /// Get active users dalam company
        /// </summary>
        Task<IEnumerable<User>> GetActiveUsersByCompanyIdAsync(int companyId);

        /// <summary>
        /// Get users by role dalam company
        /// </summary>
        Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName, int companyId);

        /// <summary>
        /// Count users dalam company
        /// </summary>
        Task<int> CountUsersByCompanyIdAsync(int companyId);

        /// <summary>
        /// Search users dalam company
        /// </summary>
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int companyId);

        /// <summary>
        /// Get paginated users dalam company
        /// </summary>
        Task<PagedResult<User>> GetPagedByCompanyIdAsync(int companyId, int pageNumber, int pageSize, string? searchTerm = null);
    }
}