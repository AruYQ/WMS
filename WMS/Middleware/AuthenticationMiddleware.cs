using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WMS.Configuration;
using WMS.Utilities;

namespace WMS.Middleware
{
    /// <summary>
    /// Middleware untuk handle JWT authentication
    /// </summary>
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        public AuthenticationMiddleware(
            RequestDelegate next,
            JwtSettings jwtSettings,
            ILogger<AuthenticationMiddleware> logger)
        {
            _next = next;
            _jwtSettings = jwtSettings;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, TokenHelper tokenHelper)
        {
            // Skip authentication for certain paths
            var path = context.Request.Path.Value?.ToLower() ?? "";

            if (ShouldSkipAuthentication(path))
            {
                await _next(context);
                return;
            }

            try
            {
                // Get token from Authorization header
                var token = GetTokenFromRequest(context);

                if (!string.IsNullOrEmpty(token))
                {
                    // Validate token and set user principal
                    var principal = tokenHelper.ValidateToken(token);
                    if (principal != null)
                    {
                        context.User = principal;
                        _logger.LogDebug("User authenticated: {Username}", principal.Identity?.Name);
                    }
                    else
                    {
                        _logger.LogDebug("Invalid JWT token provided");
                    }
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in authentication middleware");
                await _next(context);
            }
        }

        /// <summary>
        /// Check if authentication should be skipped for this path
        /// </summary>
        private bool ShouldSkipAuthentication(string path)
        {
            var publicPaths = new[]
            {
                "/account/login",
                "/account/logout",
                "/account/forgotpassword",
                "/account/resetpassword",
                "/css/",
                "/js/",
                "/images/",
                "/lib/",
                "/favicon.ico",
                "/health",
                "/api/health"
            };

            return publicPaths.Any(publicPath => path.StartsWith(publicPath));
        }

        /// <summary>
        /// Extract JWT token from request
        /// </summary>
        private string? GetTokenFromRequest(HttpContext context)
        {
            // Check Authorization header first
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }

            // Check cookie as fallback
            if (context.Request.Cookies.TryGetValue("AuthToken", out var cookieToken))
            {
                return cookieToken;
            }

            return null;
        }
    }

    /// <summary>
    /// Extension method untuk register authentication middleware
    /// </summary>
    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}