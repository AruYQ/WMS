using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WMS.Services;

namespace WMS.Attributes
{
    /// <summary>
    /// Attribute untuk memastikan user memiliki company context yang valid
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireCompanyAttribute : Attribute, IAuthorizationFilter
    {
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

            // Get current user service
            var currentUserService = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

            // Check company context
            if (!currentUserService.CompanyId.HasValue || !currentUserService.UserId.HasValue)
            {
                HandleUnauthorized(context, "Tidak memiliki akses ke perusahaan");
                return;
            }

            // Log successful authorization
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequireCompanyAttribute>>();
            logger.LogDebug("Company authorization successful for user {Username} (ID: {UserId}) in company {CompanyId}",
                currentUserService.Username, currentUserService.UserId, currentUserService.CompanyId);
        }

        /// <summary>
        /// Handle unauthorized access
        /// </summary>
        private void HandleUnauthorized(AuthorizationFilterContext context, string message)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequireCompanyAttribute>>();
            logger.LogWarning("Company access denied: {Message}", message);

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
                // Redirect to login page for web requests
                context.Result = new RedirectToActionResult("Login", "Account", null);
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