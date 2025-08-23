using System.Security.Claims;
using WMS.Services;

namespace WMS.Middleware
{
    /// <summary>
    /// Middleware untuk validate dan set company context
    /// Memastikan user memiliki akses ke company yang valid
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

        public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUserService)
        {
            // Skip for unauthenticated requests
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await _next(context);
                return;
            }

            try
            {
                // Validate company context
                var companyId = currentUserService.CompanyId;
                var userId = currentUserService.UserId;

                if (!companyId.HasValue || !userId.HasValue)
                {
                    _logger.LogWarning("Authenticated user missing company context. UserId: {UserId}, CompanyId: {CompanyId}",
                        userId, companyId);

                    // Redirect to login or access denied
                    if (IsApiRequest(context))
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Unauthorized: Missing company context");
                        return;
                    }
                    else
                    {
                        context.Response.Redirect("/Account/Login");
                        return;
                    }
                }

                // Add company context to response headers for debugging (in development only)
                if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                {
                    context.Response.Headers.Add("X-Company-Id", companyId.Value.ToString());
                    context.Response.Headers.Add("X-User-Id", userId.Value.ToString());
                }

                _logger.LogDebug("Company context validated. UserId: {UserId}, CompanyId: {CompanyId}",
                    userId, companyId);

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in company context middleware");

                // Continue with request but log the error
                await _next(context);
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

    /// <summary>
    /// Extension method untuk register company context middleware
    /// </summary>
    public static class CompanyContextMiddlewareExtensions
    {
        public static IApplicationBuilder UseCompanyContext(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CompanyContextMiddleware>();
        }
    }
}