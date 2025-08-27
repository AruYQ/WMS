using WMS.Models;

namespace WMS.Services
{
    /// <summary>
    /// Interface untuk user management service
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Get all users dalam company yang sama
        /// </summary>
        /// <returns>List of users</returns>
        Task<IEnumerable<User>> GetAllUsersAsync();

        /// <summary>
        /// Get user by ID (dengan company filtering)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User atau null</returns>
        Task<User?> GetUserByIdAsync(int userId);

        /// <summary>
        /// Get user by username (dengan company filtering)
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>User atau null</returns>
        Task<User?> GetUserByUsernameAsync(string username);

        /// <summary>
        /// Get user by email
        /// </summary>
        /// <param name="email">Email</param>
        /// <returns>User atau null</returns>
        Task<User?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Create user baru dalam company yang sama
        /// </summary>
        /// <param name="user">User data</param>
        /// <param name="password">Password</param>
        /// <param name="roleNames">Role names to assign</param>
        /// <returns>Create result</returns>
        Task<CreateUserResult> CreateUserAsync(User user, string password, IEnumerable<string> roleNames);

        /// <summary>
        /// Update user data
        /// </summary>
        /// <param name="user">Updated user data</param>
        /// <returns>Update result</returns>
        Task<UpdateUserResult> UpdateUserAsync(User user);

        /// <summary>
        /// Delete/deactivate user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Success status</returns>
        Task<bool> DeleteUserAsync(int userId);

        /// <summary>
        /// Reset password user (admin function)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="newPassword">New password</param>
        /// <returns>Reset result</returns>
        Task<ResetPasswordResult> ResetUserPasswordAsync(int userId, string newPassword);

        /// <summary>
        /// Activate/deactivate user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="isActive">Active status</param>
        /// <returns>Success status</returns>
        Task<bool> SetUserActiveStatusAsync(int userId, bool isActive);

        /// <summary>
        /// Assign roles to user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="roleNames">Role names</param>
        /// <returns>Success status</returns>
        Task<bool> AssignRolesToUserAsync(int userId, IEnumerable<string> roleNames);

        /// <summary>
        /// Remove roles from user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="roleNames">Role names to remove</param>
        /// <returns>Success status</returns>
        Task<bool> RemoveRolesFromUserAsync(int userId, IEnumerable<string> roleNames);

        /// <summary>
        /// Get available roles untuk assignment
        /// </summary>
        /// <returns>List of roles</returns>
        Task<IEnumerable<Role>> GetAvailableRolesAsync();

        /// <summary>
        /// Check username availability dalam company
        /// </summary>
        /// <param name="username">Username to check</param>
        /// <param name="excludeUserId">User ID to exclude (untuk edit)</param>
        /// <returns>True jika available</returns>
        Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null);

        /// <summary>
        /// Check email availability (global)
        /// </summary>
        /// <param name="email">Email to check</param>
        /// <param name="excludeUserId">User ID to exclude (untuk edit)</param>
        /// <returns>True jika available</returns>
        Task<bool> IsEmailAvailableAsync(string email, int? excludeUserId = null);

        /// <summary>
        /// Get user statistics untuk dashboard
        /// </summary>
        /// <returns>User statistics</returns>
        Task<UserStatistics> GetUserStatisticsAsync();
    }

    /// <summary>
    /// Result dari create user operation
    /// </summary>
    public class CreateUserResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result dari update user operation
    /// </summary>
    public class UpdateUserResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }

    /// <summary>
    /// User statistics untuk dashboard
    /// </summary>
    public class UserStatistics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int AdminUsers { get; set; }
        public int ManagerUsers { get; set; }
        public int RegularUsers { get; set; }
        public DateTime LastLogin { get; set; }
        public string? LastLoginUser { get; set; }
    }
}