using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace WMS.Attributes
{
    /// <summary>
    /// Attribute untuk require specific roles
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;
        private readonly bool _requireAllRoles;

        /// <summary>
        /// Constructor untuk single role
        /// </summary>
        /// <param name="role">Required role</param>
        public RequireRoleAttribute(string role)
        {
            _roles = new[] { role };
            _requireAllRoles = false;
        }

        /// <summary>
        /// Constructor untuk multiple roles
        /// </summary>
        /// <param name="roles">Required roles</param>
        /// <param name="requireAllRoles">True jika user harus memiliki semua roles, false jika salah satu saja cukup</param>
        public RequireRoleAttribute(string[] roles, bool requireAllRoles = false)
        {
            _roles = roles ?? throw new ArgumentNullException(nameof(roles));
            _requireAllRoles = requireAllRoles;
        }

        /// <summary>
        /// Authorization filter implementation
        /// </summary>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                HandleUnauthorized(context, "User tidak terautentikasi");
                return;
            }

            // Get user roles
            var userRoles = context.HttpContext.User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            // Check role requirements
            bool hasAccess;
            if (_requireAllRoles)
            {
                // User must have ALL required roles
                hasAccess = _roles.All(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
            }
            else
            {
                // User must have AT LEAST ONE of the required roles
                hasAccess = _roles.Any(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
            }

            if (!hasAccess)
            {
                var requiredRolesText = string.Join(", ", _roles);
                var message = _requireAllRoles
                    ? $"Memerlukan semua role: {requiredRolesText}"
                    : $"Memerlukan salah satu role: {requiredRolesText}";

                HandleUnauthorized(context, message);
                return;
            }

            // Log successful authorization
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequireRoleAttribute>>();
            logger.LogDebug("Role authorization successful for user {Username} with roles [{UserRoles}]. Required: [{RequiredRoles}]",
                context.HttpContext.User.Identity?.Name,
                string.Join(", ", userRoles),
                string.Join(", ", _roles));
        }

        /// <summary>
        /// Handle unauthorized access
        /// </summary>
        private void HandleUnauthorized(AuthorizationFilterContext context, string message)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequireRoleAttribute>>();
            logger.LogWarning("Access denied for user {Username}: {Message}",
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