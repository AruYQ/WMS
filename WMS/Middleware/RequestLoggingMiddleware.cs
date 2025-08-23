using System.Diagnostics;
using System.Text;

namespace WMS.Middleware
{
    /// <summary>
    /// Middleware untuk logging requests dan responses
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip logging for certain paths
            if (ShouldSkipLogging(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N")[..8];

            // Add request ID to response headers
            context.Response.Headers.Add("X-Request-Id", requestId);

            try
            {
                // Log request
                await LogRequestAsync(context, requestId);

                // Continue pipeline
                await _next(context);

                // Log response
                stopwatch.Stop();
                LogResponse(context, requestId, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Request {RequestId} failed after {ElapsedMs}ms",
                    requestId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Check if logging should be skipped for this path
        /// </summary>
        private bool ShouldSkipLogging(PathString path)
        {
            var skipPaths = new[]
            {
                "/css/",
                "/js/",
                "/images/",
                "/lib/",
                "/favicon.ico",
                "/health"
            };

            return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath));
        }

        /// <summary>
        /// Log incoming request
        /// </summary>
        private async Task LogRequestAsync(HttpContext context, string requestId)
        {
            var request = context.Request;
            var username = context.User?.Identity?.Name ?? "Anonymous";
            var companyId = context.User?.FindFirst("CompanyId")?.Value ?? "Unknown";

            _logger.LogInformation(
                "Request {RequestId} started: {Method} {Path} by {Username} (Company: {CompanyId})",
                requestId, request.Method, request.Path, username, companyId);

            // Log request body for POST/PUT requests (be careful with sensitive data)
            if (request.Method == "POST" || request.Method == "PUT")
            {
                if (request.ContentLength > 0 && request.ContentLength < 1024) // Only log small payloads
                {
                    request.EnableBuffering();
                    var body = await new StreamReader(request.Body, Encoding.UTF8).ReadToEndAsync();
                    request.Body.Position = 0;

                    // Mask sensitive fields
                    var maskedBody = MaskSensitiveData(body);
                    _logger.LogDebug("Request {RequestId} body: {Body}", requestId, maskedBody);
                }
            }
        }

        /// <summary>
        /// Log response
        /// </summary>
        private void LogResponse(HttpContext context, string requestId, long elapsedMs)
        {
            var response = context.Response;
            var level = response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(level,
                "Request {RequestId} completed: {StatusCode} in {ElapsedMs}ms",
                requestId, response.StatusCode, elapsedMs);
        }

        /// <summary>
        /// Mask sensitive data in request body
        /// </summary>
        private string MaskSensitiveData(string body)
        {
            var sensitiveFields = new[] { "password", "token", "secret", "key" };

            foreach (var field in sensitiveFields)
            {
                // Simple regex to mask JSON fields (this is basic, consider using proper JSON parsing)
                var pattern = $"\"{field}\"\\s*:\\s*\"[^\"]*\"";
                body = System.Text.RegularExpressions.Regex.Replace(
                    body, pattern, $"\"{field}\": \"***\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return body;
        }
    }

    /// <summary>
    /// Extension method untuk register request logging middleware
    /// </summary>
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}