using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Attributes;
using WMS.Models.ViewModels;
using WMS.Services;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller untuk authentication operations
    /// </summary>
    public class AccountController : Controller
    {
        private readonly IAuthenticationService _authService;
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthenticationService authService,
            IUserService userService,
            ICurrentUserService currentUserService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _userService = userService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Display login page
        /// </summary>
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // If already logged in, redirect to home
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        /// <summary>
        /// Process login form submission
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [AuditLog("User Login Attempt")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = await _authService.LoginAsync(
                    model.UsernameOrEmail,
                    model.Password,
                    model.RememberMe);

                if (result.Success)
                {
                    // Set authentication cookie
                    Response.Cookies.Append("AuthToken", result.Token!, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Expires = model.RememberMe ? DateTimeOffset.Now.AddDays(30) : null
                    });

                    _logger.LogInformation("User login successful: {Username}", result.User?.Username);

                    // Redirect to return URL or home
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    if (result.IsLockedOut)
                    {
                        ModelState.AddModelError("", result.Message);
                        return View("Lockout");
                    }

                    ModelState.AddModelError("", result.Message);

                    // Add individual errors if any
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt for: {UsernameOrEmail}", model.UsernameOrEmail);
                ModelState.AddModelError("", "Terjadi kesalahan saat login. Silakan coba lagi.");
            }

            return View(model);
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog("User Logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _authService.LogoutAsync();

                // Remove authentication cookie
                Response.Cookies.Delete("AuthToken");

                _logger.LogInformation("User logged out: {Username}", User.Identity?.Name);

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user: {Username}", User.Identity?.Name);
                return RedirectToAction("Login");
            }
        }

        /// <summary>
        /// Display access denied page
        /// </summary>
        public IActionResult AccessDenied()
        {
            ViewBag.Message = "Anda tidak memiliki akses untuk mengakses halaman ini.";
            ViewBag.UserRoles = _currentUserService.Roles.ToList();
            ViewBag.CompanyName = User.FindFirst("CompanyName")?.Value ?? "Unknown";
            return View();
        }

        /// <summary>
        /// Display lockout page
        /// </summary>
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        /// <summary>
        /// Display forgot password page
        /// </summary>
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        /// <summary>
        /// Process forgot password form
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [AuditLog("Forgot Password Request")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var token = await _authService.GeneratePasswordResetTokenAsync(model.Email);

                if (!string.IsNullOrEmpty(token))
                {
                    // In a real application, send email with reset link
                    // For demo purposes, we'll show the token (don't do this in production!)
                    TempData["ResetToken"] = token;
                    TempData["ResetEmail"] = model.Email;

                    _logger.LogInformation("Password reset token generated for email: {Email}", model.Email);

                    return RedirectToAction("ForgotPasswordConfirmation");
                }

                // Don't reveal whether email exists or not for security
                return RedirectToAction("ForgotPasswordConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for email: {Email}", model.Email);
                ModelState.AddModelError("", "Terjadi kesalahan. Silakan coba lagi.");
                return View(model);
            }
        }

        /// <summary>
        /// Display forgot password confirmation
        /// </summary>
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            // In development, show the token for testing
            ViewBag.ResetToken = TempData["ResetToken"];
            ViewBag.ResetEmail = TempData["ResetEmail"];

            return View();
        }

        /// <summary>
        /// Display reset password page
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Link reset password tidak valid.";
                return RedirectToAction("Login");
            }

            // Validate token
            var isValidToken = await _authService.ValidatePasswordResetTokenAsync(token);
            if (!isValidToken)
            {
                TempData["Error"] = "Link reset password tidak valid atau sudah kadaluwarsa.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        /// <summary>
        /// Process reset password form
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [AuditLog("Password Reset")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = await _authService.ResetPasswordAsync(model.Token, model.NewPassword);

                if (result.Success)
                {
                    _logger.LogInformation("Password reset successful for email: {Email}", model.Email);
                    TempData["Success"] = "Password berhasil direset. Silakan login dengan password baru.";
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("", result.Message);
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset for email: {Email}", model.Email);
                ModelState.AddModelError("", "Terjadi kesalahan saat reset password.");
            }

            return View(model);
        }

        /// <summary>
        /// API endpoint to check if user is authenticated
        /// </summary>
        [HttpGet]
        public IActionResult CheckAuth()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

            if (isAuthenticated)
            {
                return Json(new
                {
                    authenticated = true,
                    username = _currentUserService.Username,
                    fullName = _currentUserService.FullName,
                    roles = _currentUserService.Roles.ToArray(),
                    companyId = _currentUserService.CompanyId
                });
            }

            return Json(new { authenticated = false });
        }

        /// <summary>
        /// Check username availability (AJAX)
        /// </summary>
        [HttpPost]
        [RequireCompany]
        public async Task<IActionResult> CheckUsernameAvailability(string username, int? excludeUserId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Json(new { available = false, message = "Username tidak boleh kosong" });
                }

                var available = await _userService.IsUsernameAvailableAsync(username, excludeUserId);

                return Json(new
                {
                    available = available,
                    message = available ? "Username tersedia" : "Username sudah digunakan dalam perusahaan ini"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username availability: {Username}", username);
                return Json(new { available = false, message = "Terjadi kesalahan" });
            }
        }

        /// <summary>
        /// Check email availability (AJAX)
        /// </summary>
        [HttpPost]
        [RequireCompany]
        public async Task<IActionResult> CheckEmailAvailability(string email, int? excludeUserId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Json(new { available = false, message = "Email tidak boleh kosong" });
                }

                var available = await _userService.IsEmailAvailableAsync(email, excludeUserId);

                return Json(new
                {
                    available = available,
                    message = available ? "Email tersedia" : "Email sudah digunakan dalam perusahaan ini"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email availability: {Email}", email);
                return Json(new { available = false, message = "Terjadi kesalahan" });
            }
        }

        /// <summary>
        /// Get current user info (API)
        /// </summary>
        [HttpGet]
        [RequireCompany]
        public async Task<IActionResult> GetCurrentUserInfo()
        {
            try
            {
                var user = await _userService.GetCurrentUserProfileAsync();
                if (user == null)
                {
                    return Json(new { success = false, message = "User tidak ditemukan" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = user.Id,
                        username = user.Username,
                        email = user.Email,
                        fullName = user.FullName,
                        phone = user.Phone,
                        companyName = user.Company?.Name,
                        roles = user.UserRoles.Select(ur => ur.Role?.Name).Where(r => !string.IsNullOrEmpty(r)),
                        lastLoginDate = user.LastLoginDate,
                        isActive = user.IsActive,
                        emailVerified = user.EmailVerified
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user info");
                return Json(new { success = false, message = "Terjadi kesalahan" });
            }
        }

        /// <summary>
        /// Update current user profile (API)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireCompany]
        [AuditLog("Update Profile via API")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new { success = false, message = "Data tidak valid", errors = errors });
                }

                var result = await _userService.UpdateCurrentUserProfileAsync(request);

                if (result.Success)
                {
                    return Json(new { success = true, message = result.Message });
                }

                return Json(new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile via API");
                return Json(new { success = false, message = "Terjadi kesalahan saat memperbarui profile" });
            }
        }

        /// <summary>
        /// Change password (API)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireCompany]
        [AuditLog("Change Password via API")]
        public async Task<IActionResult> ChangePasswordApi([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                {
                    return Json(new { success = false, message = "Password lama wajib diisi" });
                }

                if (string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return Json(new { success = false, message = "Password baru wajib diisi" });
                }

                if (request.NewPassword != request.ConfirmPassword)
                {
                    return Json(new { success = false, message = "Konfirmasi password tidak cocok" });
                }

                var result = await _userService.ChangePasswordAsync(request);

                if (result.Success)
                {
                    return Json(new { success = true, message = result.Message });
                }

                return Json(new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password via API");
                return Json(new { success = false, message = "Terjadi kesalahan saat mengubah password" });
            }
        }

        /// <summary>
        /// Validate password strength (AJAX)
        /// </summary>
        [HttpPost]
        public IActionResult ValidatePasswordStrength(string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    return Json(new
                    {
                        valid = false,
                        message = "Password tidak boleh kosong",
                        strength = "none",
                        score = 0
                    });
                }

                // Simple password strength calculation
                var score = 0;
                var messages = new List<string>();

                // Length check
                if (password.Length >= 8)
                {
                    score += 2;
                    messages.Add("Panjang memadai");
                }
                else
                {
                    messages.Add("Minimal 8 karakter");
                }

                // Has lowercase
                if (password.Any(char.IsLower))
                {
                    score += 1;
                    messages.Add("Mengandung huruf kecil");
                }

                // Has uppercase
                if (password.Any(char.IsUpper))
                {
                    score += 1;
                    messages.Add("Mengandung huruf besar");
                }

                // Has digits
                if (password.Any(char.IsDigit))
                {
                    score += 1;
                    messages.Add("Mengandung angka");
                }

                // Has special characters
                if (password.Any(ch => !char.IsLetterOrDigit(ch)))
                {
                    score += 1;
                    messages.Add("Mengandung karakter khusus");
                }

                var strength = score switch
                {
                    <= 2 => "weak",
                    <= 4 => "medium",
                    _ => "strong"
                };

                var strengthText = strength switch
                {
                    "weak" => "Lemah",
                    "medium" => "Sedang",
                    "strong" => "Kuat",
                    _ => "Tidak diketahui"
                };

                return Json(new
                {
                    valid = score >= 3,
                    message = $"Kekuatan password: {strengthText}",
                    strength = strength,
                    score = score,
                    requirements = messages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating password strength");
                return Json(new
                {
                    valid = false,
                    message = "Terjadi kesalahan saat validasi password",
                    strength = "error",
                    score = 0
                });
            }
        }

        /// <summary>
        /// Session keepalive endpoint
        /// </summary>
        [HttpPost]
        [RequireCompany]
        public IActionResult KeepAlive()
        {
            return Json(new
            {
                success = true,
                timestamp = DateTime.UtcNow,
                user = _currentUserService.Username,
                company = _currentUserService.CompanyId
            });
        }

        /// <summary>
        /// Get user session info
        /// </summary>
        [HttpGet]
        [RequireCompany]
        public IActionResult GetSessionInfo()
        {
            try
            {
                var sessionInfo = new
                {
                    isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                    username = _currentUserService.Username,
                    fullName = _currentUserService.FullName,
                    email = _currentUserService.Email,
                    companyId = _currentUserService.CompanyId,
                    roles = _currentUserService.Roles.ToArray(),
                    sessionStart = DateTime.UtcNow.AddHours(-8), // Approximate session start
                    lastActivity = DateTime.UtcNow,
                    expiresAt = DateTime.UtcNow.AddHours(8) // JWT expiration
                };

                return Json(new { success = true, data = sessionInfo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session info");
                return Json(new { success = false, message = "Terjadi kesalahan saat mengambil info session" });
            }
        }

        /// <summary>
        /// Health check endpoint for authentication
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Health()
        {
            return Json(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "Authentication Service",
                version = "1.0.0"
            });
        }

        #region Private Helper Methods

        /// <summary>
        /// Set authentication cookie with proper options
        /// </summary>
        private void SetAuthenticationCookie(string token, bool rememberMe)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = rememberMe ? DateTimeOffset.Now.AddDays(30) : null
            };

            Response.Cookies.Append("AuthToken", token, cookieOptions);
        }

        /// <summary>
        /// Clear authentication cookie
        /// </summary>
        private void ClearAuthenticationCookie()
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTimeOffset.Now.AddDays(-1)
            };

            Response.Cookies.Append("AuthToken", "", cookieOptions);
        }

        /// <summary>
        /// Log security event
        /// </summary>
        private void LogSecurityEvent(string eventType, string details = "")
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";

            _logger.LogInformation("Security Event: {EventType} | User: {Username} | IP: {ClientIp} | UserAgent: {UserAgent} | Details: {Details}",
                eventType,
                User.Identity?.Name ?? "Anonymous",
                clientIp,
                userAgent,
                details);
        }

        #endregion
    }
}
