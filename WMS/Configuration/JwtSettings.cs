namespace WMS.Configuration
{
    /// <summary>
    /// Configuration settings untuk JWT authentication
    /// </summary>
    public class JwtSettings
    {
        /// <summary>
        /// Secret key untuk signing JWT tokens
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Issuer untuk JWT token
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Audience untuk JWT token
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Expiration time dalam jam
        /// </summary>
        public int ExpirationHours { get; set; } = 8;

        /// <summary>
        /// Validate issuer
        /// </summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>
        /// Validate audience
        /// </summary>
        public bool ValidateAudience { get; set; } = true;

        /// <summary>
        /// Validate lifetime
        /// </summary>
        public bool ValidateLifetime { get; set; } = true;

        /// <summary>
        /// Validate issuer signing key
        /// </summary>
        public bool ValidateIssuerSigningKey { get; set; } = true;

        /// <summary>
        /// Clock skew untuk token validation
        /// </summary>
        public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
    }
}