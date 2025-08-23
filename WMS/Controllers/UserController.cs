using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WMS.Attributes;
using WMS.Data;
using WMS.Models.ViewModels;
using WMS.Services;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller untuk user management within company
    /// </summary>
    [RequireCompany]
    [RequireRole("Admin", "Manager")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Display user list
        /// </summary>
        [AuditLog("View User List")]
        public async Task<IActionResult> Index(string? searchTerm, string? roleFilter, string? statusFilter, int page = 1, int pageSize = 10)
        {
            try
            {
                var model = new UserListViewModel
                {
                    SearchTerm = searchTerm,
                    RoleFilter = roleFilter,
                    StatusFilter = statusFilter,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                // Get paginated users
                var users = await _userService.GetPagedUsersAsync(page, pageSize, searchTerm);

                // Convert to ViewModels
                model.Users = new PagedResult<UserViewModel>
                {
                    Items = users.Items.Select(MapUserToViewModel),
                    TotalItems = users.TotalItems,
                    PageNumber = users.PageNumber,
                    PageSize = users.PageSize,
                    TotalPages = users.TotalPages
                };

                // Get available roles for filter
                model.AvailableRoles = await GetAvailableRolesAsync();

                // Get company summary
                model.CompanySummary = await GetCompanySummaryAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user list");
                TempData["Error"] = "Terjadi kesalahan saat memuat daftar user.";
                return View(new UserListViewModel());
            }
        }

        /// <summary>
        /// Display user details
        /// </summary>
        [AuditLog("View User Details")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "User tidak ditemukan.";
                    return RedirectToAction("Index");
                }

                var model = MapUserToDetailsViewModel(user);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID: {UserId}", id);
                TempData["Error"] = "Terjadi kesalahan saat memuat detail user.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Display create user form
        /// </summary>
        [RequireRole("Admin")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new CreateUserViewModel
                {
                    AvailableRoles = await GetRoleSelectionViewModelsAsync()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create user form");
                TempData["Error"] = "Terjadi kesalahan saat memuat form.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Process create user form
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin")]
        [AuditLog("Create User")]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    model.AvailableRoles = await GetRoleSelectionViewModelsAsync();
                    return View(model);
                }

                // Check username availability
                if (!await _userService.IsUsernameAvailableAsync(model.Username))
                {
                    ModelState.AddModelError("Username", "Username sudah digunakan dalam perusahaan ini.");
                    model.AvailableRoles = await GetRoleSelectionViewModelsAsync();
                    return View(model);
                }

                // Check email availability
                if (!await _userService.IsEmailAvailableAsync(model.Email))
                {
                    ModelState.AddModelError("Email", "Email sudah digunakan dalam perusahaan ini.");
                    model.AvailableRoles = await GetRoleSelectionViewModelsAsync();
                    return View(model);
                }

                // Create user request
                var request = new CreateUserRequest
                {
                    Username = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Password = model.Password,
                    IsActive = model.IsActive,
                    RoleIds = model.SelectedRoleIds
                };

                var result = await _userService.CreateUserAsync(request);

                if (result.Success)
                {
                    TempData["Success"] = "User berhasil dibuat.";
                    return RedirectToAction("Details", new { id = result.User!.Id });
                }

                ModelState.AddModelError("", result.Message);
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }

                model.AvailableRoles = await GetRoleSelectionViewModelsAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", model.Username);
                ModelState.AddModelError("", "Terjadi kesalahan saat membuat user.");
                model.AvailableRoles = await GetRoleSelectionViewModelsAsync();
                return View(model);
            }
        }

        /// <summary>
        /// Display edit user form
        /// </summary>
        [RequireRole("Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "User tidak ditemukan.";
                    return RedirectToAction("Index");
                }

                var model = new EditUserViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Phone = user.Phone,
                    IsActive = user.IsActive,
                    EmailVerified = user.EmailVerified,
                    CreatedDate = user.CreatedDate,
                    CreatedBy = user.CreatedBy,
                    LastLoginDate = user.LastLoginDate,
                    AvailableRoles = await GetRoleSelectionViewModelsAsync(),
                    SelectedRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList(),
                    CurrentRoles = user.UserRoles.Select(ur => new UserRoleViewModel
                    {
                        Id = ur.Id,
                        UserId = ur.UserId,
                        RoleId = ur.RoleId,
                        RoleName = ur.Role?.Name ?? "",
                        RoleDescription = ur.Role?.Description,
                        AssignedDate = ur.AssignedDate,
                        AssignedBy = ur.AssignedBy
                    }).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit user form for ID: {UserId}", id);
                TempData["Error"] = "Terjadi kesalahan saat memuat form edit user.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Process edit user form
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin")]
        [AuditLog("Edit User")]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    model.AvailableRoles = await GetRoleSelectionViewModelsAsync();
                    return View(model);
                }

                // Check username availability (exclude current user)
                if (!await _userService.IsUsernameAvailableAsync(model.Username, model.Id))
                {
                    ModelState.AddModelError("Username", "Username sudah digunakan.");
                    model.AvailableRoles = await GetRoleSelectionViewModelsAsync();
                    return View(model);
                }

                // Check email availability (exclude current user)
                if (!await _userService.IsEmailAvailableAsync(model.Email, model.Id))
                {
                    ModelState.AddModelError("Email", "Email sudah digunakan.");
                    model.AvailableRoles = await GetRoleSelectionViewModelsAsync();
                    return View(model);
                }

                // Update user request
                var request = new UpdateUserRequest
                {
                    Username = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    IsActive = model.IsActive,
                    RoleIds = model.SelectedRoleIds
                };

                var result = await _userService.UpdateUserAsync(model.Id, request);

                if (result.Success)
                {
                    TempData["Success"] = "User berhasil diperbarui.";
                    return RedirectToAction("Details", new { id = model.Id });
                }

                ModelState.AddModelError("", result.Message);
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }

                model.AvailableRoles = await GetRoleSelectionViewModelsAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing user: {UserId}", model.Id);
                ModelState.AddModelError("", "Terjadi kesalahan saat memperbarui user.");
                model.AvailableRoles = await GetRoleSelectionViewModelsAsync();
                return View(model);
            }
        }

        /// <summary>
        /// Display delete confirmation
        /// </summary>
        [RequireRole("Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "User tidak ditemukan.";
                    return RedirectToAction("Index");
                }

                // Cannot delete self
                if (user.Id == _currentUserService.UserId)
                {
                    TempData["Error"] = "Tidak dapat menghapus akun sendiri.";
                    return RedirectToAction("Index");
                }

                var model = MapUserToViewModel(user);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete confirmation for user ID: {UserId}", id);
                TempData["Error"] = "Terjadi kesalahan saat memuat konfirmasi hapus user.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Process user deletion
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin")]
        [AuditLog("Delete User")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);

                if (result.Success)
                {
                    TempData["Success"] = "User berhasil dihapus.";
                }
                else
                {
                    TempData["Error"] = result.Message;
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                TempData["Error"] = "Terjadi kesalahan saat menghapus user.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Display current user profile
        /// </summary>
        public async Task<IActionResult> Profile()
        {
            try
            {
                var user = await _userService.GetCurrentUserProfileAsync();
                if (user == null)
                {
                    TempData["Error"] = "Profile tidak ditemukan.";
                    return RedirectToAction("Index", "Home");
                }

                var model = new UserProfileViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Phone = user.Phone,
                    CompanyName = user.Company?.Name ?? "",
                    RoleNames = user.UserRoles.Select(ur => ur.Role?.Name ?? "").Where(r => !string.IsNullOrEmpty(r)).ToList(),
                    LastLoginDate = user.LastLoginDate,
                    CreatedDate = user.CreatedDate
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile");
                TempData["Error"] = "Terjadi kesalahan saat memuat profile.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Display change password form
        /// </summary>
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        /// <summary>
        /// Process change password form
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog("Change Password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var request = new ChangePasswordRequest
                {
                    CurrentPassword = model.CurrentPassword,
                    NewPassword = model.NewPassword,
                    ConfirmPassword = model.ConfirmPassword
                };

                var result = await _userService.ChangePasswordAsync(request);

                if (result.Success)
                {
                    TempData["Success"] = "Password berhasil diubah.";
                    return RedirectToAction("Profile");
                }

                ModelState.AddModelError("", result.Message);
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {Username}", _currentUserService.Username);
                ModelState.AddModelError("", "Terjadi kesalahan saat mengubah password.");
                return View(model);
            }
        }

        /// <summary>
        /// Display reset user password form (Admin only)
        /// </summary>
        [RequireRole("Admin")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "User tidak ditemukan.";
                    return RedirectToAction("Index");
                }

                var model = new ResetUserPasswordViewModel
                {
                    UserId = user.Id,
                    Username = user.Username,
                    FullName = user.FullName
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reset password form for user ID: {UserId}", id);
                TempData["Error"] = "Terjadi kesalahan saat memuat form reset password.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Process reset user password (Admin only)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin")]
        [AuditLog("Reset User Password")]
        public async Task<IActionResult> ResetPassword(ResetUserPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = await _userService.ResetUserPasswordAsync(model.UserId, model.NewPassword);

                if (result.Success)
                {
                    TempData["Success"] = "Password user berhasil direset.";

                    // TODO: Send notification email if requested
                    if (model.NotifyUser)
                    {
                        // Implement email notification
                    }

                    return RedirectToAction("Details", new { id = model.UserId });
                }

                ModelState.AddModelError("", result.Message);
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user: {UserId}", model.UserId);
                ModelState.AddModelError("", "Terjadi kesalahan saat reset password.");
                return View(model);
            }
        }

        /// <summary>
        /// Toggle user active status (Admin only)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin")]
        [AuditLog("Toggle User Status")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User tidak ditemukan." });
                }

                var result = await _userService.SetUserActiveStatusAsync(id, !user.IsActive);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    newStatus = !user.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status: {UserId}", id);
                return Json(new { success = false, message = "Terjadi kesalahan saat mengubah status user." });
            }
        }

        #region Helper Methods

        /// <summary>
        /// Map User entity to UserViewModel
        /// </summary>
        private UserViewModel MapUserToViewModel(Models.User user)
        {
            return new UserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                IsActive = user.IsActive,
                EmailVerified = user.EmailVerified,
                LastLoginDate = user.LastLoginDate,
                CreatedDate = user.CreatedDate,
                CreatedBy = user.CreatedBy,
                ModifiedDate = user.ModifiedDate,
                ModifiedBy = user.ModifiedBy,
                UserRoles = user.UserRoles?.Select(ur => new UserRoleViewModel
                {
                    Id = ur.Id,
                    UserId = ur.UserId,
                    RoleId = ur.RoleId,
                    RoleName = ur.Role?.Name ?? "",
                    RoleDescription = ur.Role?.Description,
                    AssignedDate = ur.AssignedDate,
                    AssignedBy = ur.AssignedBy
                }).ToList() ?? new List<UserRoleViewModel>()
            };
        }

        /// <summary>
        /// Map User entity to UserDetailsViewModel
        /// </summary>
        private UserDetailsViewModel MapUserToDetailsViewModel(Models.User user)
        {
            return new UserDetailsViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                CompanyName = user.Company?.Name ?? "",
                IsActive = user.IsActive,
                EmailVerified = user.EmailVerified,
                LastLoginDate = user.LastLoginDate,
                CreatedDate = user.CreatedDate,
                CreatedBy = user.CreatedBy,
                ModifiedDate = user.ModifiedDate,
                ModifiedBy = user.ModifiedBy,
                UserRoles = user.UserRoles?.Select(ur => new UserRoleDetailViewModel
                {
                    Id = ur.Id,
                    RoleName = ur.Role?.Name ?? "",
                    RoleDescription = ur.Role?.Description,
                    AssignedDate = ur.AssignedDate,
                    AssignedBy = ur.AssignedBy,
                    IsActive = ur.Role?.IsActive ?? false,
                    Permissions = GetPermissionsFromRole(ur.Role)
                }).ToList() ?? new List<UserRoleDetailViewModel>(),
                ActivitySummary = new UserActivitySummaryViewModel
                {
                    LastLoginDate = user.LastLoginDate,
                    TotalLogins = 0 // This would come from audit log
                },
                AvailableActions = GetAvailableActionsForUser(user)
            };
        }

        /// <summary>
        /// Get permissions from role
        /// </summary>
        private List<string> GetPermissionsFromRole(Models.Role? role)
        {
            if (role == null || string.IsNullOrEmpty(role.Permissions))
                return new List<string>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<string[]>(role.Permissions)?.ToList() ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Get available actions for user
        /// </summary>
        private UserActionsViewModel GetAvailableActionsForUser(Models.User user)
        {
            var currentUserId = _currentUserService.UserId ?? 0;
            var isCurrentUser = user.Id == currentUserId;
            var isTargetUserAdmin = user.UserRoles?.Any(ur => ur.Role?.Name == "Admin") ?? false;
            var isCurrentUserAdmin = _currentUserService.IsInRole("Admin");

            return new UserActionsViewModel
            {
                CanEdit = isCurrentUserAdmin && !isCurrentUser,
                CanDelete = isCurrentUserAdmin && !isCurrentUser,
                CanResetPassword = isCurrentUserAdmin && !isCurrentUser,
                CanManageRoles = isCurrentUserAdmin && !isCurrentUser,
                CanActivateDeactivate = isCurrentUserAdmin && !isCurrentUser,
                CanViewAuditLog = isCurrentUserAdmin,
                CanUnlock = isCurrentUserAdmin && !isCurrentUser,
                IsCurrentUser = isCurrentUser,
                IsTargetUserAdmin = isTargetUserAdmin,
                IsLastAdminInCompany = false // This would require additional query
            };
        }

        /// <summary>
        /// Get available roles as ViewModels
        /// </summary>
        private async Task<List<RoleViewModel>> GetAvailableRolesAsync()
        {
            var roles = await _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            return roles.Select(r => new RoleViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsActive = r.IsActive
            }).ToList();
        }

        /// <summary>
        /// Get role selection ViewModels
        /// </summary>
        private async Task<List<RoleSelectionViewModel>> GetRoleSelectionViewModelsAsync()
        {
            var roles = await _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            return roles.Select(r => new RoleSelectionViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSelected = false
            }).ToList();
        }

        /// <summary>
        /// Get company summary
        /// </summary>
        private async Task<CompanySummaryViewModel> GetCompanySummaryAsync()
        {
            var companyId = _currentUserService.CompanyId ?? 0;
            var company = await _context.Companies.FindAsync(companyId);

            if (company == null)
                return new CompanySummaryViewModel();

            var totalUsers = await _context.Users.CountAsync(u => u.CompanyId == companyId);
            var activeUsers = await _context.Users.CountAsync(u => u.CompanyId == companyId && u.IsActive);

            return new CompanySummaryViewModel
            {
                CompanyName = company.Name,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                MaxUsers = company.MaxUsers,
                SubscriptionPlan = company.SubscriptionPlan,
                SubscriptionEndDate = company.SubscriptionEndDate
            };
        }

        #endregion
    }
}