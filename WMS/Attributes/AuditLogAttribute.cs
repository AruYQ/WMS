using Microsoft.AspNetCore.Mvc.Filters;
using WMS.Services;

namespace WMS.Attributes
{
    /// <summary>
    /// Attribute untuk automatic audit logging
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuditLogAttribute : Attribute, IActionFilter
    {
        private readonly string _action;
        private readonly string? _description;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="action">Action being performed</param>
        /// <param name="description">Optional description</param>
        public AuditLogAttribute(string action, string? description = null)
        {
            _action = action;
            _description = description;
        }

        /// <summary>
        /// Called before action execution
        /// </summary>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Store start time for performance tracking
            context.HttpContext.Items["AuditStartTime"] = DateTime.UtcNow;
        }

        /// <summary>
        /// Called after action execution
        /// </summary>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<AuditLogAttribute>>();
            var currentUserService = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

            try
            {
                var startTime = (DateTime?)context.HttpContext.Items["AuditStartTime"];
                var duration = startTime.HasValue ? DateTime.UtcNow - startTime.Value : TimeSpan.Zero;

                var controllerName = context.RouteData.Values["controller"]?.ToString();
                var actionName = context.RouteData.Values["action"]?.ToString();
                var success = context.Exception == null;

                // Log audit information
                if (success)
                {
                    logger.LogInformation(
                        "Audit: {Action} by {Username} (ID: {UserId}, Company: {CompanyId}) " +
                        "on {Controller}/{ActionName} completed in {Duration}ms. {Description}",
                        _action,
                        currentUserService.Username ?? "Anonymous",
                        currentUserService.UserId ?? 0,
                        currentUserService.CompanyId ?? 0,
                        controllerName,
                        actionName,
                        duration.TotalMilliseconds,
                        _description ?? "");
                }
                else
                {
                    logger.LogWarning(
                        "Audit: {Action} by {Username} (ID: {UserId}, Company: {CompanyId}) " +
                        "on {Controller}/{ActionName} failed after {Duration}ms. Error: {Error}",
                        _action,
                        currentUserService.Username ?? "Anonymous",
                        currentUserService.UserId ?? 0,
                        currentUserService.CompanyId ?? 0,
                        controllerName,
                        actionName,
                        duration.TotalMilliseconds,
                        context.Exception?.Message);
                }
            }
            catch (Exception ex)
            {
                // Don't let audit logging break the application
                logger.LogError(ex, "Error in audit logging for action: {Action}", _action);
            }
        }
    }
}