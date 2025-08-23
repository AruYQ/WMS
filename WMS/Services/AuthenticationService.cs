using Microsoft.EntityFrameworkCore;
using WMS.Configuration;
using WMS.Data;
using WMS.Models;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Implementation dari IAuthenticationService
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthenticationSettings _authSettings;
        private readonly TokenHelper _tokenHelper;
        private readonly ILogger<AuthenticationService> _logger;

        // Account lockout tracking (in-memory for simplicity, consider using Redis for production)
        private static readonly Dictionary<string, FailedLoginAttempt> _failedAttempts = new();

        public AuthenticationService(
            ApplicationDbContext context,
            AuthenticationSettings authSettings,
            TokenHelper tokenHelper,
            ILogger<AuthenticationService> logger)
        {
            _context = context;
            _authSettings = authSettings;
            _tokenHelper = tokenHelper;
            _logger = logger;
        }

        /// <summary>
        /// Login user dengan username/email dan password
        /// </summary>
        public async Task<AuthenticationResult> LoginAsync(string usernameOrEmail, string password, bool rememberMe = false)
        {
            try
            {
                _logger.LogInformation("Login attempt for user: {UsernameOrEmail}", usernameOrEmail);

                // Check if account is locked
                if (await IsAccountLockedAsync(usernameOrEmail))
                {
                    _logger.LogWarning("Login attempt blocked - account locked: {UsernameOrEmail}", usernameOrEmail);
                    return new AuthenticationResult
                    {
                        Success = false,
                        IsLockedOut = true,
                        Message = $"Account terkunci karena terlalu banyak percobaan login gagal. Coba lagi dalam {_authSettings.LockoutTimeSpanMinutes} menit."
                    };
                }

                // Find user by username or email
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u =>
                        (u.Username == usernameOrEmail || u.Email == usernameOrEmail) &&
                        u.IsActive);

                if (user == null)
                {
                    await HandleFailedLoginAsync(usernameOrEmail);
                    _logger.LogWarning("Login failed - user not found: {UsernameOrEmail}", usernameOrEmail);
                    return new AuthenticationResult
                    {
                        Success = false,
                        Message = "Username/Email atau password tidak valid."
                    };
                }

                // Check company is active
                if (user.Company == null || !user.Company.IsActive)
                {
                    _logger.LogWarning("Login failed - company inactive: {UsernameOrEmail}, Company: {CompanyName}",
                        usernameOrEmail, user.Company?.Name);
                    return new AuthenticationResult
                    {
                        Success = false,
                        Message = "Akun perusahaan tidak aktif. Hubungi administrator."
                    };
                }

                // Verify password
                if (!PasswordHelper.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
                {
                    await HandleFailedLoginAsync(usernameOrEmail);
                    _logger.LogWarning("Login failed - invalid password: {UsernameOrEmail}", usernameOrEmail);
                    return new AuthenticationResult
                    {
                        Success = false,
                        Message = "Username/Email atau password tidak valid."
                    };
                }

                // Check email verification if required
                if (_authSettings.RequireEmailVerification && !user.EmailVerified)
                {
                    _logger.LogWarning("Login failed - email not verified: {UsernameOrEmail}", usernameOrEmail);
                    return new AuthenticationResult
                    {
                        Success = false,
                        Message = "Email belum diverifikasi. Silakan verifikasi email terlebih dahulu."
                    };
                }

                // Clear failed attempts on successful login
                ClearFailedLoginAttempts(usernameOrEmail);

                // Generate JWT token
                var roles = user.UserRoles.Select(ur => ur.Role?.Name ?? "").Where(r => !string.IsNullOrEmpty(r));
                var token = _tokenHelper.GenerateJwtToken(user, roles);

                // Update last login
                user.LastLoginDate = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Login successful for user: {Username} (ID: {UserId})", user.Username, user.Id);

                return new AuthenticationResult
                {
                    Success = true,
                    Message = "Login berhasil.",
                    User = user,
                    Token = token,
                    TokenExpiration = DateTime.UtcNow.AddHours(_authSettings.JwtExpirationHours)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {UsernameOrEmail}", usernameOrEmail);
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "Terjadi kesalahan saat login. Silakan coba lagi."
                };
            }
        }

        /// <summary>
        /// Logout current user (mainly for logging purposes in stateless JWT)
        /// </summary>
        public async Task LogoutAsync()
        {
            // In stateless JWT, logout is mainly client-side (remove token)
            // But we can log the event for audit purposes
            _logger.LogInformation("User logged out");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Validate user credentials tanpa login
        /// </summary>
        public async Task<User?> ValidateUserCredentialsAsync(string usernameOrEmail, string password)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u =>
                        (u.Username == usernameOrEmail || u.Email == usernameOrEmail) &&
                        u.IsActive);

                if (user != null && PasswordHelper.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
                {
                    return user;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for user: {UsernameOrEmail}", usernameOrEmail);
                return null;
            }
        }

        /// <summary>
        /// Change password untuk current user
        /// </summary>
        public async Task<AuthenticationResult> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                // This would typically get current user from HttpContext
                // For now, we'll need to pass userId or get it from ICurrentUserService
                throw new NotImplementedException("This method needs ICurrentUserService integration");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "Terjadi kesalahan saat mengubah password."
                };
            }
        }

        /// <summary>
        /// Generate password reset token
        /// </summary>
        public async Task<string?> GeneratePasswordResetTokenAsync(string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
                if (user == null) return null;

                var token = _tokenHelper.GenerateSecureToken();
                user.ResetPasswordToken = token;
                user.ResetPasswordTokenExpiry = DateTime.Now.AddHours(24); // Token valid for 24 hours

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset token generated for user: {Email}", email);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset token for: {Email}", email);
                return null;
            }
        }

        /// <summary>
        /// Reset password dengan token
        /// </summary>
        public async Task<AuthenticationResult> ResetPasswordAsync(string token, string newPassword)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.ResetPasswordToken == token &&
                    u.ResetPasswordTokenExpiry > DateTime.Now);

                if (user == null)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Message = "Token reset password tidak valid atau sudah kadaluwarsa."
                    };
                }

                // Validate new password
                var passwordValidation = PasswordHelper.ValidatePassword(newPassword, _authSettings.PasswordRequirements);
                if (!passwordValidation.IsValid)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Message = "Password tidak memenuhi persyaratan.",
                        Errors = passwordValidation.ErrorMessages
                    };
                }

                // Update password
                var salt = PasswordHelper.GenerateSalt();
                user.PasswordHash = PasswordHelper.HashPassword(newPassword, salt);
                user.PasswordSalt = salt;
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset successful for user: {Username}", user.Username);

                return new AuthenticationResult
                {
                    Success = true,
                    Message = "Password berhasil direset."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "Terjadi kesalahan saat reset password."
                };
            }
        }

        /// <summary>
        /// Validate password reset token
        /// </summary>
        public async Task<bool> ValidatePasswordResetTokenAsync(string token)
        {
            try
            {
                return await _context.Users.AnyAsync(u =>
                    u.ResetPasswordToken == token &&
                    u.ResetPasswordTokenExpiry > DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating password reset token");
                return false;
            }
        }

        /// <summary>
        /// Check if account is locked
        /// </summary>
        public async Task<bool> IsAccountLockedAsync(string usernameOrEmail)
        {
            if (!_authSettings.LockoutEnabled) return false;

            if (_failedAttempts.TryGetValue(usernameOrEmail, out var attempt))
            {
                if (attempt.Count >= _authSettings.MaxFailedAccessAttempts)
                {
                    var lockoutEnd = attempt.LastAttempt.AddMinutes(_authSettings.LockoutTimeSpanMinutes);
                    if (DateTime.Now < lockoutEnd)
                    {
                        return true;
                    }
                    else
                    {
                        // Lockout period expired, clear attempts
                        _failedAttempts.Remove(usernameOrEmail);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Unlock account (admin function)
        /// </summary>
        public async Task<bool> UnlockAccountAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    // Clear failed login attempts
                    var keysToRemove = _failedAttempts.Keys.Where(k =>
                        k == user.Username || k == user.Email).ToList();

                    foreach (var key in keysToRemove)
                    {
                        _failedAttempts.Remove(key);
                    }

                    _logger.LogInformation("Account unlocked for user: {Username} (ID: {UserId})", user.Username, userId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking account for user ID: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Handle failed login attempt
        /// </summary>
        private async Task HandleFailedLoginAsync(string usernameOrEmail)
        {
            if (!_authSettings.LockoutEnabled) return;

            if (_failedAttempts.ContainsKey(usernameOrEmail))
            {
                _failedAttempts[usernameOrEmail].Count++;
                _failedAttempts[usernameOrEmail].LastAttempt = DateTime.Now;
            }
            else
            {
                _failedAttempts[usernameOrEmail] = new FailedLoginAttempt
                {
                    Count = 1,
                    LastAttempt = DateTime.Now
                };
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Clear failed login attempts
        /// </summary>
        private void ClearFailedLoginAttempts(string usernameOrEmail)
        {
            _failedAttempts.Remove(usernameOrEmail);
        }
    }

    /// <summary>
    /// Helper class untuk tracking failed login attempts
    /// </summary>
    internal class FailedLoginAttempt
    {
        public int Count { get; set; }
        public DateTime LastAttempt { get; set; }
    }
}