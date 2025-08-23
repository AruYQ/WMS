using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using WMS.Services;

namespace WMS.Attributes
{
    /// <summary>
    /// Attribute untuk allow anonymous access (override authentication requirements)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class WMSAllowAnonymousAttribute : Attribute, IAllowAnonymous
    {
        // This interface is used by ASP.NET Core to identify actions that don't require authentication
    }
}

