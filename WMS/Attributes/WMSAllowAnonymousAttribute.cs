using Microsoft.AspNetCore.Authorization;

namespace WMS.Attributes
{
    /// <summary>
    /// Custom AllowAnonymous attribute untuk WMS
    /// Sama seperti AllowAnonymous tapi lebih specific untuk WMS
    /// </summary>
    public class WMSAllowAnonymousAttribute : AllowAnonymousAttribute
    {
        /// <summary>
        /// Reason kenapa endpoint ini allow anonymous
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Constructor
        /// </summary>
        public WMSAllowAnonymousAttribute()
        {
        }

        /// <summary>
        /// Constructor dengan reason
        /// </summary>
        /// <param name="reason">Reason untuk allow anonymous</param>
        public WMSAllowAnonymousAttribute(string reason)
        {
            Reason = reason;
        }
    }
}