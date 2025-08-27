namespace WMS.Configuration
{
    /// <summary>
    /// JWT configuration settings
    /// </summary>
    public class JwtSettings
    {
        /// <summary>
        /// JWT Secret Key for signing tokens
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// JWT Token Issuer
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// JWT Token Audience
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration time in hours
        /// </summary>
        public int ExpirationHours { get; set; } = 8;

        /// <summary>
        /// Whether to validate the issuer
        /// </summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>
        /// Whether to validate the audience
        /// </summary>
        public bool ValidateAudience { get; set; } = true;

        /// <summary>
        /// Whether to validate token lifetime
        /// </summary>
        public bool ValidateLifetime { get; set; } = true;

        /// <summary>
        /// Whether to validate issuer signing key
        /// </summary>
        public bool ValidateIssuerSigningKey { get; set; } = true;

        /// <summary>
        /// Clock skew tolerance
        /// </summary>
        public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
    }
}