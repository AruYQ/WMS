using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Attributes;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller untuk user management dalam company
    /// </summary>
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            ICurrentUserService currentUserService,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// List all users dalam company
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var userStats = await _userService.GetUserStatisticsAsync();

                var viewModel = new UserListViewModel
                {
                    Users = users.ToList(),
                    Statistics = userStats
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user list");
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat data user";
                return View(new UserListViewModel());
            }
        }

        /// <summary>
        /// User detail page
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User tidak ditemukan";
                    return RedirectToAction("Index");
                }

                var viewModel = new UserDetailsViewModel
                {
                    User = user,
                    Roles = user.RoleNames.ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID: {UserId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat detail user";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Create user page
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var roles = await _userService.GetAvailableRolesAsync();

                var viewModel = new CreateUserViewModel
                {
                    AvailableRoles = roles.Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = r.Name,
                        Text = $"{r.Name} - {r.Description}"
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create user page");
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat halaman";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Process create user
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload roles
                var roles = await _userService.GetAvailableRolesAsync();
                model.AvailableRoles = roles.Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = r.Name,
                    Text = $"{r.Name} - {r.Description}"
                }).ToList();

                return View(model);
            }

            try
            {
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    IsActive = true,
                    EmailVerified = true
                };

                var selectedRoles = model.SelectedRoles ?? new List<string>();
                var result = await _userService.CreateUserAsync(user, model.Password, selectedRoles);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Gagal membuat user");

                    if (result.ValidationErrors.Any())
                    {
                        foreach (var error in result.ValidationErrors)
                        {
                            ModelState.AddModelError(string.Empty, error);
                        }
                    }

                    // Reload roles
                    var roles = await _userService.GetAvailableRolesAsync();
                    model.AvailableRoles = roles.Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = r.Name,
                        Text = $"{r.Name} - {r.Description}"
                    }).ToList();

                    return View(model);
                }

                TempData["SuccessMessage"] = "User berhasil dibuat";
                return RedirectToAction("Details", new { id = result.User!.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "Terjadi kesalahan sistem");
                return View(model);
            }
        }

        /// <summary>
        /// Edit user page
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User tidak ditemukan";
                    return RedirectToAction("Index");
                }

                var roles = await _userService.GetAvailableRolesAsync();

                var viewModel = new EditUserViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Phone = user.Phone,
                    IsActive = user.IsActive,
                    SelectedRoles = user.RoleNames.ToList(),
                    AvailableRoles = roles.Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = r.Name,
                        Text = $"{r.Name} - {r.Description}",
                        Selected = user.RoleNames.Contains(r.Name)
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit user page for ID: {UserId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat data user";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Process edit user
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload roles
                var roles = await _userService.GetAvailableRolesAsync();
                model.AvailableRoles = roles.Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = r.Name,
                    Text = $"{r.Name} - {r.Description}",
                    Selected = (model.SelectedRoles ?? new List<string>()).Contains(r.Name)
                }).ToList();

                return View(model);
            }

            try
            {
                var user = new User
                {
                    Id = model.Id,
                    Username = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    IsActive = model.IsActive
                };

                var result = await _userService.UpdateUserAsync(user);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Gagal update user");
                    return View(model);
                }

                // Update roles
                var selectedRoles = model.SelectedRoles ?? new List<string>();
                await _userService.AssignRolesToUserAsync(model.Id, selectedRoles);

                TempData["SuccessMessage"] = "User berhasil diupdate";
                return RedirectToAction("Details", new { id = model.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", model.Id);
                ModelState.AddModelError(string.Empty, "Terjadi kesalahan sistem");
                return View(model);
            }
        }

        /// <summary>
        /// Delete user confirmation
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User tidak ditemukan";
                    return RedirectToAction("Index");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete user page for ID: {UserId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat data user";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Process delete user
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);

                if (result)
                {
                    TempData["SuccessMessage"] = "User berhasil dihapus";
                }
                else
                {
                    TempData["ErrorMessage"] = "Gagal menghapus user";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan sistem";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// User profile page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var user = await _userService.GetUserByIdAsync(userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User tidak ditemukan";
                    return RedirectToAction("Index", "Home");
                }

                var viewModel = new UserProfileViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Phone = user.Phone,
                    LastLoginDate = user.LastLoginDate,
                    CompanyName = user.Company?.Name ?? "",
                    Roles = user.RoleNames.ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile");
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat profil";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Edit profile page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var user = await _userService.GetUserByIdAsync(userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User tidak ditemukan";
                    return RedirectToAction("Profile");
                }

                var viewModel = new EditUserViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Phone = user.Phone,
                    IsActive = user.IsActive
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit profile page");
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat halaman";
                return RedirectToAction("Profile");
            }
        }

        /// <summary>
        /// Process edit profile
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = new User
                {
                    Id = model.Id,
                    Username = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    IsActive = model.IsActive
                };

                var result = await _userService.UpdateUserAsync(user);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Gagal update profil");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Profil berhasil diupdate";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile: {UserId}", model.Id);
                ModelState.AddModelError(string.Empty, "Terjadi kesalahan sistem");
                return View(model);
            }
        }

        /// <summary>
        /// Reset user password (admin function)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User tidak ditemukan";
                    return RedirectToAction("Index");
                }

                var viewModel = new ResetUserPasswordViewModel
                {
                    UserId = user.Id,
                    Username = user.Username,
                    FullName = user.FullName
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reset password page for user: {UserId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat halaman";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Process reset user password
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ResetPassword(ResetUserPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _userService.ResetUserPasswordAsync(model.UserId, model.NewPassword);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Gagal reset password");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Password user berhasil direset";
                return RedirectToAction("Details", new { id = model.UserId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user: {UserId}", model.UserId);
                ModelState.AddModelError(string.Empty, "Terjadi kesalahan sistem");
                return View(model);
            }
        }

        /// <summary>
        /// Toggle user active status (AJAX)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User tidak ditemukan" });
                }

                var newStatus = !user.IsActive;
                var result = await _userService.SetUserActiveStatusAsync(id, newStatus);

                if (result)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Status user berhasil diubah menjadi {(newStatus ? "Aktif" : "Nonaktif")}",
                        newStatus = newStatus
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Gagal mengubah status user" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status: {UserId}", id);
                return Json(new { success = false, message = "Terjadi kesalahan sistem" });
            }
        }

        /// <summary>
        /// Check username availability (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckUsername(string username, int? excludeId)
        {
            try
            {
                var isAvailable = await _userService.IsUsernameAvailableAsync(username, excludeId);
                return Json(new { available = isAvailable });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username availability: {Username}", username);
                return Json(new { available = false });
            }
        }

        /// <summary>
        /// Check email availability (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email, int? excludeId)
        {
            try
            {
                var isAvailable = await _userService.IsEmailAvailableAsync(email, excludeId);
                return Json(new { available = isAvailable });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email availability: {Email}", email);
                return Json(new { available = false });
            }
        }
    }
}