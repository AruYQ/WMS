namespace WMS.Services
{
    /// <summary>
    /// Service untuk mendapatkan context user yang sedang login
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
        string Username { get; }

        /// <summary>
        /// Company ID dari user yang sedang login
        /// </summary>
        int? CompanyId { get; }

        /// <summary>
        /// Full name user yang sedang login
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Email user yang sedang login
        /// </summary>
        string Email { get; }

        /// <summary>
        /// Roles dari user yang sedang login
        /// </summary>
        IEnumerable<string> Roles { get; }

        /// <summary>
        /// Check apakah user sedang login
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Check apakah user memiliki role tertentu
        /// </summary>
        /// <param name="roleName">Nama role</param>
        /// <returns>True jika user memiliki role</returns>
        bool IsInRole(string roleName);

        /// <summary>
        /// Check apakah user adalah admin
        /// </summary>
        bool IsAdmin { get; }

        /// <summary>
        /// Check apakah user adalah manager atau admin
        /// </summary>
        bool IsManagerOrAdmin { get; }

        /// <summary>
        /// Get claim value by type
        /// </summary>
        /// <param name="claimType">Type of claim</param>
        /// <returns>Claim value atau null</returns>
        string? GetClaimValue(string claimType);
    }
}