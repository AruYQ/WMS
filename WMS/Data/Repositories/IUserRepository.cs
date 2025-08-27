using WMS.Models;

namespace WMS.Data.Repositories
{
    /// <summary>
    /// Interface untuk User repository dengan company filtering
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Get all users dalam company yang sama
        /// </summary>
        /// <returns>List of users</returns>
        Task<IEnumerable<User>> GetAllAsync();

        /// <summary>
        /// Get user by ID dengan company filtering
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User atau null</returns>
        Task<User?> GetByIdAsync(int id);

        /// <summary>
        /// Get user by username dalam company yang sama
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>User atau null</returns>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// Get user by email (global search)
        /// </summary>
        /// <param name="email">Email</param>
        /// <returns>User atau null</returns>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Get user by username or email untuk login
        /// </summary>
        /// <param name="usernameOrEmail">Username atau email</param>
        /// <returns>User dengan company dan roles</returns>
        Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);

        /// <summary>
        /// Create user baru
        /// </summary>
        /// <param name="user">User data</param>
        /// <returns>Created user</returns>
        Task<User> CreateAsync(User user);

        /// <summary>
        /// Update user data
        /// </summary>
        /// <param name="user">User data</param>
        /// <returns>Updated user</returns>
        Task<User> UpdateAsync(User user);

        /// <summary>
        /// Delete user (soft delete)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success status</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Check username exists dalam company
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="excludeUserId">User ID to exclude</param>
        /// <returns>True jika exists</returns>
        Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null);

        /// <summary>
        /// Check email exists (global)
        /// </summary>
        /// <param name="email">Email</param>
        /// <param name="excludeUserId">User ID to exclude</param>
        /// <returns>True jika exists</returns>
        Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);

        /// <summary>
        /// Get users by role dalam company
        /// </summary>
        /// <param name="roleName">Role name</param>
        /// <returns>List of users</returns>
        Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName);

        /// <summary>
        /// Get user count dalam company
        /// </summary>
        /// <returns>User count</returns>
        Task<int> GetUserCountAsync();

        /// <summary>
        /// Get active user count dalam company
        /// </summary>
        /// <returns>Active user count</returns>
        Task<int> GetActiveUserCountAsync();
    }
}