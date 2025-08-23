using System.Security.Claims;

namespace WMS.Services
{
    /// <summary>
    /// Service untuk mendapatkan informasi user yang sedang login
    /// Digunakan oleh repository untuk company filtering
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// User ID yang sedang login
        /// </summary>
        int? UserId { get; }

        /// <summary>
        /// Username yang sedang login
        /// </summary>
        string? Username { get; }

        /// <summary>
        /// Company ID dari user yang sedang login
        /// </summary>
        int? CompanyId { get; }

        /// <summary>
        /// Email user yang sedang login
        /// </summary>
        string? Email { get; }

        /// <summary>
        /// Full name user yang sedang login
        /// </summary>
        string? FullName { get; }

        /// <summary>
        /// Roles user yang sedang login
        /// </summary>
        IEnumerable<string> Roles { get; }

        /// <summary>
        /// Apakah user sudah login
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Check apakah user memiliki role tertentu
        /// </summary>
        bool IsInRole(string role);

        /// <summary>
        /// Check apakah user memiliki salah satu dari roles yang disebutkan
        /// </summary>
        bool IsInAnyRole(params string[] roles);

        /// <summary>
        /// Get all claims dari current user
        /// </summary>
        IEnumerable<Claim> GetClaims();
    }
}