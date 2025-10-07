using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Attributes;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller untuk authentication (Login/Logout)
    /// </summary>
    public class AccountController : Controller
    {
        private readonly WMSIAuthenticationService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            WMSIAuthenticationService authService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Login page
        /// </summary>
        [HttpGet]
        [WMSAllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Redirect jika sudah login
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        /// <summary>
        /// Process login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [WMSAllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _authService.LoginAsync(model.UsernameOrEmail, model.Password);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Login failed");

                    if (result.AccountLocked)
                    {
                        ModelState.AddModelError(string.Empty, "Akun terkunci karena terlalu banyak percobaan login yang gagal");
                    }

                    return View(model);
                }

                if (result.User == null)
                {
                    ModelState.AddModelError(string.Empty, "User data tidak ditemukan");
                    return View(model);
                }

                // Create claims untuk cookie authentication
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
                    new Claim(ClaimTypes.Name, result.User.Username),
                    new Claim(ClaimTypes.Email, result.User.Email),
                    new Claim("FullName", result.User.FullName),
                    new Claim("CompanyId", result.User.CompanyId.ToString()),
                    new Claim("CompanyCode", result.User.Company?.Code ?? ""),
                    new Claim("CompanyName", result.User.Company?.Name ?? ""),
                    new Claim("UserId", result.User.Id.ToString())
                };

                // Add roles and permissions to claims
                foreach (var userRole in result.User.UserRoles.Where(ur => ur.Role?.IsActive == true))
                {
                    if (userRole.Role != null)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                        
                        // Add permissions from role to claims
                        if (!string.IsNullOrEmpty(userRole.Role.Permissions))
                        {
                            try
                            {
                                var permissions = System.Text.Json.JsonSerializer.Deserialize<string[]>(userRole.Role.Permissions);
                                if (permissions != null)
                                {
                                    foreach (var permission in permissions)
                                    {
                                        claims.Add(new Claim("Permission", permission));
                                    }
                                }
                            }
                            catch (System.Text.Json.JsonException ex)
                            {
                                _logger.LogWarning(ex, "Failed to deserialize permissions for role {RoleName}: {Permissions}", 
                                    userRole.Role.Name, userRole.Role.Permissions);
                            }
                        }
                    }
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User {Username} logged in successfully", result.User.Username);

                // Redirect ke return URL atau home
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {UsernameOrEmail}", model.UsernameOrEmail);
                ModelState.AddModelError(string.Empty, "Terjadi kesalahan sistem. Silakan coba lagi.");
                return View(model);
            }
        }

        /// <summary>
        /// Logout
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var username = User.Identity?.Name;

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                _logger.LogInformation("User {Username} logged out", username);

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return RedirectToAction("Login");
            }
        }

        /// <summary>
        /// Access denied page
        /// </summary>
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Forgot password page
        /// </summary>
        [HttpGet]
        [WMSAllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        /// <summary>
        /// Process forgot password
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [WMSAllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _authService.GenerateResetPasswordTokenAsync(model.Email);

                // Always show success message untuk security (don't reveal if email exists)
                TempData["SuccessMessage"] = "Jika email terdaftar, link reset password akan dikirim ke email Anda";

                _logger.LogInformation("Password reset requested for email: {Email}", model.Email);

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password for email: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Terjadi kesalahan sistem");
                return View(model);
            }
        }

        /// <summary>
        /// Reset password with token
        /// </summary>
        [HttpGet]
        [WMSAllowAnonymous]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            return View(new ResetPasswordViewModel { Token = token });
        }

        /// <summary>
        /// Process reset password
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [WMSAllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _authService.ResetPasswordWithTokenAsync(model.Token, model.NewPassword);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Reset password gagal");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Password berhasil direset. Silakan login dengan password baru";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password with token");
                ModelState.AddModelError(string.Empty, "Terjadi kesalahan sistem");
                return View(model);
            }
        }

        /// <summary>
        /// Change password page
        /// </summary>
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        /// <summary>
        /// Process change password
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    ModelState.AddModelError(string.Empty, "User context tidak ditemukan");
                    return View(model);
                }

                var result = await _authService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Ganti password gagal");

                    if (result.ValidationErrors.Any())
                    {
                        foreach (var error in result.ValidationErrors)
                        {
                            ModelState.AddModelError(string.Empty, error);
                        }
                    }

                    return View(model);
                }

                TempData["SuccessMessage"] = "Password berhasil diubah";
                return RedirectToAction("Profile", "User");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                ModelState.AddModelError(string.Empty, "Terjadi kesalahan sistem");
                return View(model);
            }
        }
    }
}