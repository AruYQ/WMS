using Microsoft.EntityFrameworkCore;
using WMS.Configuration;
using WMS.Data;
using WMS.Data.Repositories;
using WMS.Models;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Implementation dari IUserService
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly AuthenticationSettings _authSettings;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository,
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            AuthenticationSettings authSettings,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _context = context;
            _currentUserService = currentUserService;
            _authSettings = authSettings;
            _logger = logger;
        }

        /// <summary>
        /// Get all users dalam current company
        /// </summary>
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue)
            {
                throw new InvalidOperationException("No company context found");
            }

            return await _userRepository.GetAllByCompanyIdAsync(companyId.Value);
        }

        /// <summary>
        /// Get user by ID (with company validation)
        /// </summary>
        public async Task<User?> GetUserByIdAsync(int id)
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue)
            {
                return null;
            }

            var user = await _userRepository.GetByIdAsync(id);

            // Validate user belongs to current company
            if (user != null && user.CompanyId != companyId.Value)
            {
                return null;
            }

            return user;
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        public async Task<User?> GetCurrentUserProfileAsync()
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return null;
            }

            return await _userRepository.GetWithRolesAsync(userId.Value);
        }

        /// <summary>
        /// Create user baru dalam company
        /// </summary>
        public async Task<UserOperationResult> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "No company context found"
                    };
                }

                // Validate input
                var validationResult = ValidateCreateUserRequest(request);
                if (!validationResult.Success)
                {
                    return validationResult;
                }

                // Check username availability
                if (await _userRepository.ExistsByUsernameAsync(request.Username, companyId.Value))
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Username sudah digunakan dalam perusahaan ini"
                    };
                }

                // Check email availability
                if (await _userRepository.ExistsByEmailAsync(request.Email, companyId.Value))
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Email sudah digunakan dalam perusahaan ini"
                    };
                }

                // Check company user limit
                var currentUserCount = await _userRepository.CountUsersByCompanyIdAsync(companyId.Value);
                var company = await _context.Companies.FindAsync(companyId.Value);
                if (company != null && currentUserCount >= company.MaxUsers)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = $"Sudah mencapai batas maksimum user ({company.MaxUsers})"
                    };
                }

                // Validate password
                var passwordValidation = PasswordHelper.ValidatePassword(request.Password, _authSettings.PasswordRequirements);
                if (!passwordValidation.IsValid)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Password tidak memenuhi persyaratan",
                        Errors = passwordValidation.ErrorMessages
                    };
                }

                // Create user
                var salt = PasswordHelper.GenerateSalt();
                var hashedPassword = PasswordHelper.HashPassword(request.Password, salt);

                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    FullName = request.FullName,
                    Phone = request.Phone,
                    PasswordHash = hashedPassword,
                    PasswordSalt = salt,
                    CompanyId = companyId.Value,
                    IsActive = request.IsActive,
                    EmailVerified = !_authSettings.RequireEmailVerification,
                    CreatedDate = DateTime.Now,
                    CreatedBy = _currentUserService.Username
                };

                var createdUser = await _userRepository.AddAsync(user);

                // Assign roles
                if (request.RoleIds.Any())
                {
                    await AssignRolesToUserAsync(createdUser.Id, request.RoleIds);
                }

                _logger.LogInformation("User created successfully: {Username} (ID: {UserId}) by {CreatedBy}",
                    createdUser.Username, createdUser.Id, _currentUserService.Username);

                return new UserOperationResult
                {
                    Success = true,
                    Message = "User berhasil dibuat",
                    User = createdUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Username}", request.Username);
                return new UserOperationResult
                {
                    Success = false,
                    Message = "Terjadi kesalahan saat membuat user"
                };
            }
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        public async Task<UserOperationResult> UpdateUserAsync(int userId, UpdateUserRequest request)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "No company context found"
                    };
                }

                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "User tidak ditemukan"
                    };
                }

                // Validate input
                var validationResult = ValidateUpdateUserRequest(request);
                if (!validationResult.Success)
                {
                    return validationResult;
                }

                // Check username availability (exclude current user)
                if (user.Username != request.Username &&
                    await _userRepository.ExistsByUsernameAsync(request.Username, companyId.Value, userId))
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Username sudah digunakan"
                    };
                }

                // Check email availability (exclude current user)
                if (user.Email != request.Email &&
                    await _userRepository.ExistsByEmailAsync(request.Email, companyId.Value, userId))
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Email sudah digunakan"
                    };
                }

                // Update user
                user.Username = request.Username;
                user.Email = request.Email;
                user.FullName = request.FullName;
                user.Phone = request.Phone;
                user.IsActive = request.IsActive;
                user.ModifiedBy = _currentUserService.Username;

                var updatedUser = await _userRepository.UpdateAsync(user);

                // Update roles
                await UpdateUserRolesAsync(userId, request.RoleIds);

                _logger.LogInformation("User updated successfully: {Username} (ID: {UserId}) by {ModifiedBy}",
                    updatedUser.Username, updatedUser.Id, _currentUserService.Username);

                return new UserOperationResult
                {
                    Success = true,
                    Message = "User berhasil diperbarui",
                    User = updatedUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return new UserOperationResult
                {
                    Success = false,
                    Message = "Terjadi kesalahan saat memperbarui user"
                };
            }
        }

        /// <summary>
        /// Update current user profile
        /// </summary>
        public async Task<UserOperationResult> UpdateCurrentUserProfileAsync(UpdateProfileRequest request)
        {
            try
            {
                var userId = _currentUserService.UserId;
                if (!userId.HasValue)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "User tidak ditemukan"
                    };
                }

                var user = await _userRepository.GetByIdAsync(userId.Value);
                if (user == null)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "User tidak ditemukan"
                    };
                }

                // Basic validation
                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Nama lengkap wajib diisi"
                    };
                }

                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Email wajib diisi"
                    };
                }

                // Check email availability (exclude current user)
                if (user.Email != request.Email &&
                    await _userRepository.ExistsByEmailAsync(request.Email, user.CompanyId, userId.Value))
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Email sudah digunakan"
                    };
                }

                // Update profile
                user.FullName = request.FullName;
                user.Email = request.Email;
                user.Phone = request.Phone;
                user.ModifiedBy = _currentUserService.Username;

                var updatedUser = await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Profile updated successfully for user: {Username} (ID: {UserId})",
                    updatedUser.Username, updatedUser.Id);

                return new UserOperationResult
                {
                    Success = true,
                    Message = "Profile berhasil diperbarui",
                    User = updatedUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating current user profile");
                return new UserOperationResult
                {
                    Success = false,
                    Message = "Terjadi kesalahan saat memperbarui profile"
                };
            }
        }

        /// <summary>
        /// Change password untuk current user
        /// </summary>
        public async Task<UserOperationResult> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                var userId = _currentUserService.UserId;
                if (!userId.HasValue)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "User tidak ditemukan"
                    };
                }

                // Validation
                if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Password lama wajib diisi"
                    };
                }

                if (request.NewPassword != request.ConfirmPassword)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Konfirmasi password tidak cocok"
                    };
                }

                var user = await _userRepository.GetByIdAsync(userId.Value);
                if (user == null)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "User tidak ditemukan"
                    };
                }

                // Verify current password
                if (!PasswordHelper.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Password lama tidak benar"
                    };
                }

                // Validate new password
                var passwordValidation = PasswordHelper.ValidatePassword(request.NewPassword, _authSettings.PasswordRequirements);
                if (!passwordValidation.IsValid)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Password baru tidak memenuhi persyaratan",
                        Errors = passwordValidation.ErrorMessages
                    };
                }

                // Update password
                var salt = PasswordHelper.GenerateSalt();
                user.PasswordHash = PasswordHelper.HashPassword(request.NewPassword, salt);
                user.PasswordSalt = salt;
                user.ModifiedBy = _currentUserService.Username;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Password changed successfully for user: {Username} (ID: {UserId})",
                    user.Username, user.Id);

                return new UserOperationResult
                {
                    Success = true,
                    Message = "Password berhasil diubah"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for current user");
                return new UserOperationResult
                {
                    Success = false,
                    Message = "Terjadi kesalahan saat mengubah password"
                };
            }
        }

        /// <summary>
        /// Reset password untuk user (admin function)
        /// </summary>
        public async Task<UserOperationResult> ResetUserPasswordAsync(int userId, string newPassword)
        {
            try
            {
                // Check admin permission
                if (!_currentUserService.IsInRole("Admin"))
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Tidak memiliki akses untuk reset password"
                    };
                }

                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "User tidak ditemukan"
                    };
                }

                // Validate new password
                var passwordValidation = PasswordHelper.ValidatePassword(newPassword, _authSettings.PasswordRequirements);
                if (!passwordValidation.IsValid)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Password tidak memenuhi persyaratan",
                        Errors = passwordValidation.ErrorMessages
                    };
                }

                // Reset password
                var salt = PasswordHelper.GenerateSalt();
                user.PasswordHash = PasswordHelper.HashPassword(newPassword, salt);
                user.PasswordSalt = salt;
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;
                user.ModifiedBy = _currentUserService.Username;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Password reset successfully for user: {Username} (ID: {UserId}) by admin: {AdminUsername}",
                    user.Username, user.Id, _currentUserService.Username);

                return new UserOperationResult
                {
                    Success = true,
                    Message = "Password berhasil direset"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
                return new UserOperationResult
                {
                    Success = false,
                    Message = "Terjadi kesalahan saat reset password"
                };
            }
        }

        /// <summary>
        /// Delete/deactivate user
        /// </summary>
        public async Task<UserOperationResult> DeleteUserAsync(int userId)
        {
            try
            {
                // Check admin permission
                if (!_currentUserService.IsInRole("Admin"))
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Tidak memiliki akses untuk menghapus user"
                    };
                }

                // Cannot delete self
                if (userId == _currentUserService.UserId)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Tidak dapat menghapus akun sendiri"
                    };
                }

                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "User tidak ditemukan"
                    };
                }

                var result = await _userRepository.DeleteAsync(userId);
                if (!result)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Gagal menghapus user"
                    };
                }

                _logger.LogInformation("User deleted successfully: {Username} (ID: {UserId}) by admin: {AdminUsername}",
                    user.Username, user.Id, _currentUserService.Username);

                return new UserOperationResult
                {
                    Success = true,
                    Message = "User berhasil dihapus"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return new UserOperationResult
                {
                    Success = false,
                    Message = "Terjadi kesalahan saat menghapus user"
                };
            }
        }

        /// <summary>
        /// Activate/deactivate user
        /// </summary>
        public async Task<UserOperationResult> SetUserActiveStatusAsync(int userId, bool isActive)
        {
            try
            {
                // Check admin permission
                if (!_currentUserService.IsInRole("Admin"))
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Tidak memiliki akses untuk mengubah status user"
                    };
                }

                // Cannot deactivate self
                if (userId == _currentUserService.UserId && !isActive)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Tidak dapat menonaktifkan akun sendiri"
                    };
                }

                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "User tidak ditemukan"
                    };
                }

                user.IsActive = isActive;
                user.ModifiedBy = _currentUserService.Username;
                await _userRepository.UpdateAsync(user);

                var statusText = isActive ? "diaktifkan" : "dinonaktifkan";
                _logger.LogInformation("User status changed: {Username} (ID: {UserId}) {StatusText} by admin: {AdminUsername}",
                    user.Username, user.Id, statusText, _currentUserService.Username);

                return new UserOperationResult
                {
                    Success = true,
                    Message = $"User berhasil {statusText}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing user status for user {UserId}", userId);
                return new UserOperationResult
                {
                    Success = false,
                    Message = "Terjadi kesalahan saat mengubah status user"
                };
            }
        }

        /// <summary>
        /// Assign role to user
        /// </summary>
        public async Task<UserOperationResult> AssignRoleToUserAsync(int userId, int roleId)
        {
            try
            {
                // Check admin permission
                if (!_currentUserService.IsInRole("Admin"))
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Tidak memiliki akses untuk mengatur role"
                    };
                }

                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "User tidak ditemukan"
                    };
                }

                // Check if role exists
                var role = await _context.Roles.FindAsync(roleId);
                if (role == null || !role.IsActive)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Role tidak ditemukan atau tidak aktif"
                    };
                }

                // Check if user already has this role
                var existingUserRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

                if (existingUserRole != null)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "User sudah memiliki role ini"
                    };
                }

                // Assign role
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    AssignedDate = DateTime.Now,
                    AssignedBy = _currentUserService.Username,
                    CreatedDate = DateTime.Now,
                    CreatedBy = _currentUserService.Username
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Role assigned: {RoleName} to user {Username} (ID: {UserId}) by admin: {AdminUsername}",
                    role.Name, user.Username, user.Id, _currentUserService.Username);

                return new UserOperationResult
                {
                    Success = true,
                    Message = $"Role {role.Name} berhasil diberikan ke user"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
                return new UserOperationResult
                {
                    Success = false,
                    Message = "Terjadi kesalahan saat memberikan role"
                };
            }
        }

        /// <summary>
        /// Remove role from user
        /// </summary>
        public async Task<UserOperationResult> RemoveRoleFromUserAsync(int userId, int roleId)
        {
            try
            {
                // Check admin permission
                if (!_currentUserService.IsInRole("Admin"))
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "Tidak memiliki akses untuk mengatur role"
                    };
                }

                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "User tidak ditemukan"
                    };
                }

                var userRole = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

                if (userRole == null)
                {
                    return new UserOperationResult
                    {
                        Success = false,
                        Message = "User tidak memiliki role ini"
                    };
                }

                // Cannot remove last admin role from user if they're the only admin
                if (userRole.Role?.Name == "Admin")
                {
                    var companyId = _currentUserService.CompanyId;
                    if (companyId.HasValue)
                    {
                        var adminCount = await _context.UserRoles
                            .Include(ur => ur.User)
                            .Include(ur => ur.Role)
                            .CountAsync(ur => ur.User!.CompanyId == companyId.Value &&
                                            ur.User.IsActive &&
                                            ur.Role!.Name == "Admin");

                        if (adminCount <= 1)
                        {
                            return new UserOperationResult
                            {
                                Success = false,
                                Message = "Tidak dapat menghapus role Admin terakhir dalam perusahaan"
                            };
                        }
                    }
                }

                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Role removed: {RoleName} from user {Username} (ID: {UserId}) by admin: {AdminUsername}",
                    userRole.Role?.Name, user.Username, user.Id, _currentUserService.Username);

                return new UserOperationResult
                {
                    Success = true,
                    Message = $"Role {userRole.Role?.Name} berhasil dihapus dari user"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
                return new UserOperationResult
                {
                    Success = false,
                    Message = "Terjadi kesalahan saat menghapus role"
                };
            }
        }

        /// <summary>
        /// Get users by role dalam current company
        /// </summary>
        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string roleName)
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue)
            {
                return new List<User>();
            }

            return await _userRepository.GetUsersByRoleAsync(roleName, companyId.Value);
        }

        /// <summary>
        /// Search users dalam current company
        /// </summary>
        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue)
            {
                return new List<User>();
            }

            return await _userRepository.SearchUsersAsync(searchTerm, companyId.Value);
        }

        /// <summary>
        /// Get paginated users
        /// </summary>
        public async Task<PagedResult<User>> GetPagedUsersAsync(int pageNumber, int pageSize, string? searchTerm = null)
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue)
            {
                return new PagedResult<User>();
            }

            return await _userRepository.GetPagedByCompanyIdAsync(companyId.Value, pageNumber, pageSize, searchTerm);
        }

        /// <summary>
        /// Validate username availability
        /// </summary>
        public async Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null)
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue)
            {
                return false;
            }

            return !await _userRepository.ExistsByUsernameAsync(username, companyId.Value, excludeUserId);
        }

        /// <summary>
        /// Validate email availability
        /// </summary>
        public async Task<bool> IsEmailAvailableAsync(string email, int? excludeUserId = null)
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue)
            {
                return false;
            }

            return !await _userRepository.ExistsByEmailAsync(email, companyId.Value, excludeUserId);
        }

        #region Private Helper Methods

        /// <summary>
        /// Validate create user request
        /// </summary>
        private UserOperationResult ValidateCreateUserRequest(CreateUserRequest request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Username))
                errors.Add("Username wajib diisi");

            if (string.IsNullOrWhiteSpace(request.Email))
                errors.Add("Email wajib diisi");

            if (string.IsNullOrWhiteSpace(request.FullName))
                errors.Add("Nama lengkap wajib diisi");

            if (string.IsNullOrWhiteSpace(request.Password))
                errors.Add("Password wajib diisi");

            if (errors.Any())
            {
                return new UserOperationResult
                {
                    Success = false,
                    Message = "Data tidak valid",
                    Errors = errors
                };
            }

            return new UserOperationResult { Success = true };
        }

        /// <summary>
        /// Validate update user request
        /// </summary>
        private UserOperationResult ValidateUpdateUserRequest(UpdateUserRequest request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Username))
                errors.Add("Username wajib diisi");

            if (string.IsNullOrWhiteSpace(request.Email))
                errors.Add("Email wajib diisi");

            if (string.IsNullOrWhiteSpace(request.FullName))
                errors.Add("Nama lengkap wajib diisi");

            if (errors.Any())
            {
                return new UserOperationResult
                {
                    Success = false,
                    Message = "Data tidak valid",
                    Errors = errors
                };
            }

            return new UserOperationResult { Success = true };
        }

        /// <summary>
        /// Assign roles to user
        /// </summary>
        private async Task AssignRolesToUserAsync(int userId, List<int> roleIds)
        {
            if (!roleIds.Any()) return;

            var currentUsername = _currentUserService.Username;
            var userRoles = roleIds.Select(roleId => new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedDate = DateTime.Now,
                AssignedBy = currentUsername,
                CreatedDate = DateTime.Now,
                CreatedBy = currentUsername
            });

            _context.UserRoles.AddRange(userRoles);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Update user roles
        /// </summary>
        private async Task UpdateUserRolesAsync(int userId, List<int> newRoleIds)
        {
            // Get current roles
            var currentUserRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            var currentRoleIds = currentUserRoles.Select(ur => ur.RoleId).ToList();

            // Remove roles that are no longer assigned
            var rolesToRemove = currentUserRoles.Where(ur => !newRoleIds.Contains(ur.RoleId)).ToList();
            if (rolesToRemove.Any())
            {
                _context.UserRoles.RemoveRange(rolesToRemove);
            }

            // Add new roles
            var rolesToAdd = newRoleIds.Where(roleId => !currentRoleIds.Contains(roleId)).ToList();
            if (rolesToAdd.Any())
            {
                await AssignRolesToUserAsync(userId, rolesToAdd);
            }

            if (rolesToRemove.Any() || rolesToAdd.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        #endregion
    }
}