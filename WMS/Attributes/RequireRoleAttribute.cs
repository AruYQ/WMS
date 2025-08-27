using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WMS.Attributes
{
    /// <summary>
    /// Custom role-based authorization attribute untuk WMS
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireRoleAttribute : ActionFilterAttribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="roles">Required roles</param>
        public RequireRoleAttribute(params string[] roles)
        {
            _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        }

        /// <summary>
        /// Authorization check
        /// </summary>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Check if user is authenticated
            if (!user.Identity?.IsAuthenticated == true)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Check if user has any of the required roles
            if (!_roles.Any(role => user.IsInRole(role)))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }
        }

        /// <summary>
        /// Get roles as string for logging
        /// </summary>
        public string RolesAsString => string.Join(", ", _roles);
    }

    /// <summary>
    /// Require Admin role
    /// </summary>
    public class RequireAdminAttribute : RequireRoleAttribute
    {
        public RequireAdminAttribute() : base("Admin", "SuperAdmin") { }
    }

    /// <summary>
    /// Require Manager atau Admin role
    /// </summary>
    public class RequireManagerOrAdminAttribute : RequireRoleAttribute
    {
        public RequireManagerOrAdminAttribute() : base("Manager", "Admin", "SuperAdmin") { }
    }

    /// <summary>
    /// Require any user role (excluding viewer)
    /// </summary>
    public class RequireUserAccessAttribute : RequireRoleAttribute
    {
        public RequireUserAccessAttribute() : base("User", "Operator", "Supervisor", "Manager", "Admin", "SuperAdmin") { }
    }
}