using WMS.Models;

namespace WMS.Services
{
    /// <summary>
    /// Service untuk authentication operations
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Login user dengan username/email dan password
        /// </summary>
        Task<AuthenticationResult> LoginAsync(string usernameOrEmail, string password, bool rememberMe = false);

        /// <summary>
        /// Logout current user
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Validate user credentials tanpa login
        /// </summary>
        Task<User?> ValidateUserCredentialsAsync(string usernameOrEmail, string password);

        /// <summary>
        /// Change password untuk current user
        /// </summary>
        Task<AuthenticationResult> ChangePasswordAsync(string currentPassword, string newPassword);

        /// <summary>
        /// Reset password dengan token
        /// </summary>
        Task<AuthenticationResult> ResetPasswordAsync(string token, string newPassword);

        /// <summary>
        /// Generate password reset token
        /// </summary>
        Task<string?> GeneratePasswordResetTokenAsync(string email);

        /// <summary>
        /// Validate password reset token
        /// </summary>
        Task<bool> ValidatePasswordResetTokenAsync(string token);

        /// <summary>
        /// Check if account is locked
        /// </summary>
        Task<bool> IsAccountLockedAsync(string usernameOrEmail);

        /// <summary>
        /// Unlock account (admin function)
        /// </summary>
        Task<bool> UnlockAccountAsync(int userId);
    }

    /// <summary>
    /// Result dari authentication operations
    /// </summary>
    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public User? User { get; set; }
        public string? Token { get; set; }
        public DateTime? TokenExpiration { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public bool RequiresTwoFactor { get; set; }
        public bool IsLockedOut { get; set; }
    }
}