using System.Security.Claims;
using WMS.Services;

namespace WMS.Services
{
    /// <summary>
    /// Implementation dari ICurrentUserService
    /// Menggunakan HttpContext untuk mendapatkan informasi user
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CurrentUserService> _logger;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<CurrentUserService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// User ID yang sedang login
        /// </summary>
        public int? UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("UserId")?.Value;

                if (int.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }

                return null;
            }
        }

        /// <summary>
        /// Username yang sedang login
        /// </summary>
        public string? Username =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;

        /// <summary>
        /// Company ID dari user yang sedang login
        /// </summary>
        public int? CompanyId
        {
            get
            {
                var companyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("CompanyId")?.Value;

                if (int.TryParse(companyIdClaim, out var companyId))
                {
                    return companyId;
                }

                return null;
            }
        }

        /// <summary>
        /// Email user yang sedang login
        /// </summary>
        public string? Email =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

        /// <summary>
        /// Full name user yang sedang login
        /// </summary>
        public string? FullName =>
            _httpContextAccessor.HttpContext?.User?.FindFirst("FullName")?.Value;

        /// <summary>
        /// Roles user yang sedang login
        /// </summary>
        public IEnumerable<string> Roles =>
            _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value) ?? new List<string>();

        /// <summary>
        /// Apakah user sudah login
        /// </summary>
        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        /// <summary>
        /// Check apakah user memiliki role tertentu
        /// </summary>
        public bool IsInRole(string role)
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
        }

        /// <summary>
        /// Check apakah user memiliki salah satu dari roles yang disebutkan
        /// </summary>
        public bool IsInAnyRole(params string[] roles)
        {
            return roles.Any(role => IsInRole(role));
        }

        /// <summary>
        /// Get all claims dari current user
        /// </summary>
        public IEnumerable<Claim> GetClaims()
        {
            return _httpContextAccessor.HttpContext?.User?.Claims ?? new List<Claim>();
        }
    }
}