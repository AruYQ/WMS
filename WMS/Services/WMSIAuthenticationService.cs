using WMS.Models;

namespace WMS.Services
{
    /// <summary>
    /// Interface untuk authentication service
    /// </summary>
    public interface WMSIAuthenticationService
    {
        /// <summary>
        /// Login user dengan username/email dan password
        /// </summary>
        /// <param name="usernameOrEmail">Username atau email</param>
        /// <param name="password">Password</param>
        /// <returns>Login result</returns>
        Task<LoginResult> LoginAsync(string usernameOrEmail, string password);

        /// <summary>
        /// Logout user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Success status</returns>
        Task<bool> LogoutAsync(int userId);

        /// <summary>
        /// Validate user credentials
        /// </summary>
        /// <param name="usernameOrEmail">Username atau email</param>
        /// <param name="password">Password</param>
        /// <returns>User jika valid, null jika tidak</returns>
        Task<User?> ValidateCredentialsAsync(string usernameOrEmail, string password);

        /// <summary>
        /// Check apakah user account locked
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True jika locked</returns>
        Task<bool> IsAccountLockedAsync(int userId);

        /// <summary>
        /// Record failed login attempt
        /// </summary>
        /// <param name="usernameOrEmail">Username atau email</param>
        /// <returns>Remaining attempts before lockout</returns>
        Task<int> RecordFailedLoginAttemptAsync(string usernameOrEmail);

        /// <summary>
        /// Reset failed login attempts
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Success status</returns>
        Task<bool> ResetFailedLoginAttemptsAsync(int userId);

        /// <summary>
        /// Generate reset password token
        /// </summary>
        /// <param name="email">User email</param>
        /// <returns>Reset result</returns>
        Task<ResetPasswordResult> GenerateResetPasswordTokenAsync(string email);

        /// <summary>
        /// Reset password menggunakan token
        /// </summary>
        /// <param name="token">Reset token</param>
        /// <param name="newPassword">New password</param>
        /// <returns>Reset result</returns>
        Task<ResetPasswordResult> ResetPasswordWithTokenAsync(string token, string newPassword);

        /// <summary>
        /// Change password untuk user yang sudah login
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="currentPassword">Current password</param>
        /// <param name="newPassword">New password</param>
        /// <returns>Change result</returns>
        Task<ChangePasswordResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }

    /// <summary>
    /// Result dari login attempt
    /// </summary>
    public class LoginResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
        public string? Token { get; set; }
        public bool RequiresPasswordChange { get; set; }
        public bool AccountLocked { get; set; }
        public int RemainingAttempts { get; set; }
    }

    /// <summary>
    /// Result dari reset password
    /// </summary>
    public class ResetPasswordResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Token { get; set; }
        public DateTime? TokenExpiry { get; set; }
    }

    /// <summary>
    /// Result dari change password
    /// </summary>
    public class ChangePasswordResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }
}