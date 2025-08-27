using System.Security.Claims;

namespace WMS.Middleware
{
    /// <summary>
    /// Middleware untuk set company context dari authenticated user
    /// </summary>
    public class CompanyContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CompanyContextMiddleware> _logger;

        public CompanyContextMiddleware(RequestDelegate next, ILogger<CompanyContextMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Invoke middleware
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Only process authenticated users
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    SetCompanyContext(context);
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CompanyContextMiddleware");
                await _next(context);
            }
        }

        /// <summary>
        /// Set company context untuk current user
        /// </summary>
        private void SetCompanyContext(HttpContext context)
        {
            try
            {
                var user = context.User;

                // Get company information dari claims
                var companyId = user.FindFirst("CompanyId")?.Value;
                var companyCode = user.FindFirst("CompanyCode")?.Value;
                var companyName = user.FindFirst("CompanyName")?.Value;

                if (!string.IsNullOrEmpty(companyId))
                {
                    // Add company context to HttpContext items for easy access
                    context.Items["CompanyId"] = int.TryParse(companyId, out var id) ? id : (int?)null;
                    context.Items["CompanyCode"] = companyCode;
                    context.Items["CompanyName"] = companyName;

                    // Log company context (debug only)
                    _logger.LogDebug("Company context set for user {Username}: Company {CompanyCode} ({CompanyId})",
                        user.Identity?.Name, companyCode, companyId);
                }
                else
                {
                    _logger.LogWarning("No company context found for authenticated user: {Username}",
                        user.Identity?.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting company context for user: {Username}",
                    context.User?.Identity?.Name);
            }
        }
    }

    /// <summary>
    /// Extension method untuk register middleware
    /// </summary>
    public static class CompanyContextMiddlewareExtensions
    {
        public static IApplicationBuilder UseCompanyContext(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CompanyContextMiddleware>();
        }
    }
}