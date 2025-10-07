using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WMS.Data;
using WMS.Services;

namespace WMS.Attributes
{
    /// <summary>
    /// Attribute untuk check specific permissions
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _permissions;
        private readonly bool _requireAllPermissions;

        /// <summary>
        /// Constructor untuk single permission
        /// </summary>
        /// <param name="permission">Required permission</param>
        public RequirePermissionAttribute(string permission)
        {
            _permissions = new[] { permission };
            _requireAllPermissions = false;
        }

        /// <summary>
        /// Constructor untuk multiple permissions
        /// </summary>
        /// <param name="permissions">Required permissions</param>
        /// <param name="requireAllPermissions">True jika user harus memiliki semua permissions</param>
        public RequirePermissionAttribute(string[] permissions, bool requireAllPermissions = false)
        {
            _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
            _requireAllPermissions = requireAllPermissions;
        }

        /// <summary>
        /// Async authorization filter implementation
        /// </summary>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                HandleUnauthorized(context, "User tidak terautentikasi");
                return;
            }

            var currentUserService = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequirePermissionAttribute>>();

            // Check company context
            if (!currentUserService.UserId.HasValue)
            {
                HandleUnauthorized(context, "Context user tidak valid");
                return;
            }

            try
            {
                // Get user permissions from claims or database
                var userPermissions = await GetUserPermissionsAsync(dbContext, currentUserService.UserId.Value, context.HttpContext);

                // Check if user has "all" permission (super admin)
                if (userPermissions.Contains("all"))
                {
                    logger.LogDebug("User {Username} has 'all' permission, access granted",
                        currentUserService.Username);
                    return;
                }

                // Check specific permissions
                bool hasAccess;
                if (_requireAllPermissions)
                {
                    // User must have ALL required permissions
                    hasAccess = _permissions.All(perm => userPermissions.Contains(perm, StringComparer.OrdinalIgnoreCase));
                }
                else
                {
                    // User must have AT LEAST ONE of the required permissions
                    hasAccess = _permissions.Any(perm => userPermissions.Contains(perm, StringComparer.OrdinalIgnoreCase));
                }

                if (!hasAccess)
                {
                    var requiredPermissionsText = string.Join(", ", _permissions);
                    var message = _requireAllPermissions
                        ? $"Memerlukan semua permission: {requiredPermissionsText}"
                        : $"Memerlukan salah satu permission: {requiredPermissionsText}";

                    HandleUnauthorized(context, message);
                    return;
                }

                // Log successful authorization
                logger.LogDebug("Permission authorization successful for user {Username}. Required: [{RequiredPermissions}]",
                    currentUserService.Username, string.Join(", ", _permissions));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking permissions for user {Username}", currentUserService.Username);
                HandleUnauthorized(context, "Terjadi kesalahan saat memeriksa permission");
            }
        }

        /// <summary>
        /// Get user permissions from claims (optimized) or database (fallback)
        /// </summary>
        private async Task<List<string>> GetUserPermissionsAsync(ApplicationDbContext dbContext, int userId, HttpContext httpContext)
        {
            // First try to get permissions from claims (faster)
            var user = httpContext.User;
            if (user != null)
            {
                var claimPermissions = user.FindAll("Permission").Select(c => c.Value).ToList();
                if (claimPermissions.Any())
                {
                    return claimPermissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                }
            }

            // Fallback to database query if claims not available
            var userRoles = await dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role!.Permissions)
                .ToListAsync();

            var allPermissions = new List<string>();

            foreach (var rolePermissions in userRoles)
            {
                if (!string.IsNullOrEmpty(rolePermissions))
                {
                    try
                    {
                        var permissions = JsonSerializer.Deserialize<string[]>(rolePermissions);
                        if (permissions != null)
                        {
                            allPermissions.AddRange(permissions);
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip invalid permission JSON
                    }
                }
            }

            return allPermissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Handle unauthorized access
        /// </summary>
        private void HandleUnauthorized(AuthorizationFilterContext context, string message)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequirePermissionAttribute>>();
            logger.LogWarning("Permission denied for user {Username}: {Message}",
                context.HttpContext.User.Identity?.Name ?? "Unknown", message);

            if (IsApiRequest(context.HttpContext))
            {
                // Return JSON response for API requests
                context.Result = new JsonResult(new
                {
                    error = "Access denied",
                    message = message
                })
                {
                    StatusCode = 403
                };
            }
            else
            {
                // Redirect to access denied page for web requests
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }

        /// <summary>
        /// Check if this is an API request
        /// </summary>
        private bool IsApiRequest(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            return path.StartsWith("/api/") ||
                   context.Request.Headers.ContainsKey("X-Requested-With") ||
                   context.Request.Headers["Accept"].ToString().Contains("application/json");
        }
    }
}