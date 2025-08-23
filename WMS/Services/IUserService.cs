using WMS.Data.Repositories;
using WMS.Models;

namespace WMS.Services
{
    /// <summary>
    /// Service interface untuk user management within company
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Get all users dalam current company
        /// </summary>
        Task<IEnumerable<User>> GetAllUsersAsync();

        /// <summary>
        /// Get user by ID (with company validation)
        /// </summary>
        Task<User?> GetUserByIdAsync(int id);

        /// <summary>
        /// Get current user profile
        /// </summary>
        Task<User?> GetCurrentUserProfileAsync();

        /// <summary>
        /// Create user baru dalam company
        /// </summary>
        Task<UserOperationResult> CreateUserAsync(CreateUserRequest request);

        /// <summary>
        /// Update user profile
        /// </summary>
        Task<UserOperationResult> UpdateUserAsync(int userId, UpdateUserRequest request);

        /// <summary>
        /// Update current user profile
        /// </summary>
        Task<UserOperationResult> UpdateCurrentUserProfileAsync(UpdateProfileRequest request);

        /// <summary>
        /// Change password untuk current user
        /// </summary>
        Task<UserOperationResult> ChangePasswordAsync(ChangePasswordRequest request);

        /// <summary>
        /// Reset password untuk user (admin function)
        /// </summary>
        Task<UserOperationResult> ResetUserPasswordAsync(int userId, string newPassword);

        /// <summary>
        /// Delete/deactivate user
        /// </summary>
        Task<UserOperationResult> DeleteUserAsync(int userId);

        /// <summary>
        /// Activate/deactivate user
        /// </summary>
        Task<UserOperationResult> SetUserActiveStatusAsync(int userId, bool isActive);

        /// <summary>
        /// Assign role to user
        /// </summary>
        Task<UserOperationResult> AssignRoleToUserAsync(int userId, int roleId);

        /// <summary>
        /// Remove role from user
        /// </summary>
        Task<UserOperationResult> RemoveRoleFromUserAsync(int userId, int roleId);

        /// <summary>
        /// Get users by role dalam current company
        /// </summary>
        Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName);

        /// <summary>
        /// Search users dalam current company
        /// </summary>
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);

        /// <summary>
        /// Get paginated users
        /// </summary>
        Task<PagedResult<User>> GetPagedUsersAsync(int pageNumber, int pageSize, string? searchTerm = null);

        /// <summary>
        /// Validate username availability
        /// </summary>
        Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null);

        /// <summary>
        /// Validate email availability
        /// </summary>
        Task<bool> IsEmailAvailableAsync(string email, int? excludeUserId = null);
    }

    /// <summary>
    /// Result dari user operations
    /// </summary>
    public class UserOperationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public User? User { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Request untuk create user
    /// </summary>
    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Password { get; set; } = string.Empty;
        public List<int> RoleIds { get; set; } = new List<int>();
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Request untuk update user
    /// </summary>
    public class UpdateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public List<int> RoleIds { get; set; } = new List<int>();
    }

    /// <summary>
    /// Request untuk update profile
    /// </summary>
    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    /// <summary>
    /// Request untuk change password
    /// </summary>
    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}