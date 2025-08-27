using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WMS.Configuration;
using WMS.Data;
using WMS.Models;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Service untuk authentication operations
    /// </summary>
    public class WMSAuthenticationService : WMSIAuthenticationService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthenticationSettings _authSettings;
        private readonly TokenHelper _tokenHelper;
        private readonly ILogger<WMSAuthenticationService> _logger; // FIXED LOGGER TYPE

        public WMSAuthenticationService(
            ApplicationDbContext context,
            IOptions<AuthenticationSettings> authSettings,
            TokenHelper tokenHelper,
            ILogger<WMSAuthenticationService> logger) // FIXED LOGGER TYPE
        {
            _context = context;
            _authSettings = authSettings.Value;
            _tokenHelper = tokenHelper;
            _logger = logger;
        }

        /// <summary>
        /// Login user dengan username/email dan password
        /// </summary>
        public async Task<LoginResult> LoginAsync(string usernameOrEmail, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(password))
                {
                    return new LoginResult
                    {
                        Success = false,
                        ErrorMessage = "Username/Email dan password wajib diisi"
                    };
                }

                // Find user by username atau email
                var user = await _context.Users
                    .Include(u => u.Company)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u =>
                        (u.Username == usernameOrEmail || u.Email == usernameOrEmail) &&
                        u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("Login attempt with invalid username/email: {UsernameOrEmail}", usernameOrEmail);
                    return new LoginResult
                    {
                        Success = false,
                        ErrorMessage = "Username/Email atau password salah"
                    };
                }

                // Check company active
                if (user.Company == null || !user.Company.IsActive)
                {
                    _logger.LogWarning("Login attempt for inactive company. User: {Username}, Company: {CompanyId}",
                        user.Username, user.CompanyId);
                    return new LoginResult
                    {
                        Success = false,
                        ErrorMessage = "Company tidak aktif"
                    };
                }

                // Verify password
                if (!PasswordHelper.VerifyPassword(password, user.HashedPassword))
                {
                    _logger.LogWarning("Failed login attempt for user: {Username}", user.Username);
                    await RecordFailedLoginAttemptAsync(usernameOrEmail);

                    return new LoginResult
                    {
                        Success = false,
                        ErrorMessage = "Username/Email atau password salah"
                    };
                }

                // Reset failed attempts on successful login
                await ResetFailedLoginAttemptsAsync(user.Id);

                // Update last login date
                user.LastLoginDate = DateTime.Now;
                await _context.SaveChangesAsync();

                // Get user roles
                var roles = user.UserRoles.Where(ur => ur.Role != null && ur.Role.IsActive)
                                        .Select(ur => ur.Role!.Name)
                                        .ToList();

                // Generate JWT token
                var token = _tokenHelper.GenerateJwtToken(user, roles);

                _logger.LogInformation("Successful login for user: {Username}, Company: {CompanyCode}",
                    user.Username, user.Company.Code);

                return new LoginResult
                {
                    Success = true,
                    User = user,
                    Token = token
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for: {UsernameOrEmail}", usernameOrEmail);
                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = "Terjadi kesalahan sistem"
                };
            }
        }

        /// <summary>
        /// Logout user
        /// </summary>
        public async Task<bool> LogoutAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    _logger.LogInformation("User logged out: {Username}", user.Username);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Validate user credentials
        /// </summary>
        public async Task<User?> ValidateCredentialsAsync(string usernameOrEmail, string password)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Company)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u =>
                        (u.Username == usernameOrEmail || u.Email == usernameOrEmail) &&
                        u.IsActive);

                if (user == null || user.Company == null || !user.Company.IsActive)
                    return null;

                if (!PasswordHelper.VerifyPassword(password, user.HashedPassword))
                    return null;

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for: {UsernameOrEmail}", usernameOrEmail);
                return null;
            }
        }

        /// <summary>
        /// Check apakah user account locked (for future implementation)
        /// </summary>
        public async Task<bool> IsAccountLockedAsync(int userId)
        {
            // For now, always return false since we don't have lockout fields
            // Can be enhanced later with FailedLoginAttempts and LockoutEndDate fields
            return await Task.FromResult(false);
        }

        /// <summary>
        /// Record failed login attempt (for future implementation)
        /// </summary>
        public async Task<int> RecordFailedLoginAttemptAsync(string usernameOrEmail)
        {
            // For now, just log the attempt
            _logger.LogWarning("Failed login attempt recorded for: {UsernameOrEmail}", usernameOrEmail);
            return await Task.FromResult(_authSettings.MaxFailedAccessAttempts);
        }

        /// <summary>
        /// Reset failed login attempts
        /// </summary>
        public async Task<bool> ResetFailedLoginAttemptsAsync(int userId)
        {
            // For now, just return success
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Generate reset password token
        /// </summary>
        public async Task<ResetPasswordResult> GenerateResetPasswordTokenAsync(string email)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

                if (user == null)
                {
                    // Don't reveal if email exists or not
                    return new ResetPasswordResult
                    {
                        Success = true,
                        ErrorMessage = "Jika email terdaftar, link reset password akan dikirim"
                    };
                }

                // Generate reset token
                var token = PasswordHelper.GenerateResetToken();
                var expiry = DateTime.Now.AddHours(2); // 2 jam expired

                user.ResetPasswordToken = token;
                user.ResetPasswordTokenExpiry = expiry;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Reset password token generated for user: {Email}", email);

                return new ResetPasswordResult
                {
                    Success = true,
                    Token = token,
                    TokenExpiry = expiry
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating reset password token for: {Email}", email);
                return new ResetPasswordResult
                {
                    Success = false,
                    ErrorMessage = "Terjadi kesalahan sistem"
                };
            }
        }

        /// <summary>
        /// Reset password menggunakan token
        /// </summary>
        public async Task<ResetPasswordResult> ResetPasswordWithTokenAsync(string token, string newPassword)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.ResetPasswordToken == token &&
                        u.ResetPasswordTokenExpiry > DateTime.Now &&
                        u.IsActive);

                if (user == null)
                {
                    return new ResetPasswordResult
                    {
                        Success = false,
                        ErrorMessage = "Token reset password tidak valid atau sudah expired"
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
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset successful for user: {Username}", user.Username);

                return new ResetPasswordResult
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password with token: {Token}", token);
                return new ResetPasswordResult
                {
                    Success = false,
                    ErrorMessage = "Terjadi kesalahan sistem"
                };
            }
        }

        /// <summary>
        /// Change password untuk user yang sudah login
        /// </summary>
        public async Task<ChangePasswordResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return new ChangePasswordResult
                    {
                        Success = false,
                        ErrorMessage = "User tidak ditemukan"
                    };
                }

                // Verify current password
                if (!PasswordHelper.VerifyPassword(currentPassword, user.HashedPassword))
                {
                    return new ChangePasswordResult
                    {
                        Success = false,
                        ErrorMessage = "Password lama tidak sesuai"
                    };
                }

                // Validate new password
                var validation = PasswordHelper.ValidatePassword(newPassword);
                if (!validation.IsValid)
                {
                    return new ChangePasswordResult
                    {
                        Success = false,
                        ErrorMessage = "Password baru tidak memenuhi kriteria",
                        ValidationErrors = validation.Errors
                    };
                }

                // Update password
                user.HashedPassword = PasswordHelper.HashPassword(newPassword);
                user.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed for user: {Username}", user.Username);

                return new ChangePasswordResult
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                return new ChangePasswordResult
                {
                    Success = false,
                    ErrorMessage = "Terjadi kesalahan sistem"
                };
            }
        }
    }
}