using System.Security.Claims;

namespace WMS.Services
{
    /// <summary>
    /// Service untuk mendapatkan context user yang sedang login
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Current HTTP context
        /// </summary>
        private HttpContext? Context => _httpContextAccessor.HttpContext;

        /// <summary>
        /// Current user claims principal
        /// </summary>
        private ClaimsPrincipal? User => Context?.User;

        /// <summary>
        /// User ID yang sedang login
        /// </summary>
        public int? UserId
        {
            get
            {
                var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User?.FindFirst("UserId")?.Value;

                return int.TryParse(userIdClaim, out var userId) ? userId : null;
            }
        }

        /// <summary>
        /// Username yang sedang login
        /// </summary>
        public string Username
        {
            get
            {
                return User?.FindFirst(ClaimTypes.Name)?.Value
                    ?? User?.Identity?.Name
                    ?? "System";
            }
        }

        /// <summary>
        /// Company ID dari user yang sedang login
        /// </summary>
        public int? CompanyId
        {
            get
            {
                var companyIdClaim = User?.FindFirst("CompanyId")?.Value;
                return int.TryParse(companyIdClaim, out var companyId) ? companyId : null;
            }
        }

        /// <summary>
        /// Full name user yang sedang login
        /// </summary>
        public string FullName
        {
            get
            {
                return User?.FindFirst("FullName")?.Value
                    ?? User?.FindFirst(ClaimTypes.GivenName)?.Value
                    ?? Username;
            }
        }

        /// <summary>
        /// Email user yang sedang login
        /// </summary>
        public string Email
        {
            get
            {
                return User?.FindFirst(ClaimTypes.Email)?.Value ?? "";
            }
        }

        /// <summary>
        /// Roles dari user yang sedang login
        /// </summary>
        public IEnumerable<string> Roles
        {
            get
            {
                return User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value) ?? new List<string>();
            }
        }

        /// <summary>
        /// Check apakah user sedang login
        /// </summary>
        public bool IsAuthenticated
        {
            get
            {
                return User?.Identity?.IsAuthenticated == true;
            }
        }

        /// <summary>
        /// Check apakah user memiliki role tertentu
        /// </summary>
        /// <param name="roleName">Nama role</param>
        /// <returns>True jika user memiliki role</returns>
        public bool IsInRole(string roleName)
        {
            return User?.IsInRole(roleName) == true;
        }

        /// <summary>
        /// Check apakah user adalah admin
        /// </summary>
        public bool IsAdmin
        {
            get
            {
                return IsInRole("Admin") || IsInRole("SuperAdmin");
            }
        }

        /// <summary>
        /// Check apakah user adalah manager atau admin
        /// </summary>
        public bool IsManagerOrAdmin
        {
            get
            {
                return IsInRole("Manager") || IsAdmin;
            }
        }

        /// <summary>
        /// Get claim value by type
        /// </summary>
        /// <param name="claimType">Type of claim</param>
        /// <returns>Claim value atau null</returns>
        public string? GetClaimValue(string claimType)
        {
            return User?.FindFirst(claimType)?.Value;
        }

        /// <summary>
        /// Get company code dari claims
        /// </summary>
        public string? CompanyCode
        {
            get
            {
                return GetClaimValue("CompanyCode");
            }
        }

        /// <summary>
        /// Check apakah user dari company tertentu
        /// </summary>
        /// <param name="companyId">Company ID to check</param>
        /// <returns>True jika user dari company tersebut</returns>
        public bool IsFromCompany(int companyId)
        {
            return CompanyId == companyId;
        }

        /// <summary>
        /// Get all claims sebagai dictionary
        /// </summary>
        /// <returns>Dictionary of all claims</returns>
        public Dictionary<string, string> GetAllClaims()
        {
            return User?.Claims?.ToDictionary(c => c.Type, c => c.Value)
                ?? new Dictionary<string, string>();
        }
    }
}