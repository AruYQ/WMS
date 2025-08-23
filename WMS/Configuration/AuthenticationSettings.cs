namespace WMS.Configuration
{
    /// <summary>
    /// Configuration settings untuk authentication behavior
    /// </summary>
    public class AuthenticationSettings
    {
        /// <summary>
        /// JWT settings
        /// </summary>
        public string JwtSecretKey { get; set; } = string.Empty;
        public string JwtIssuer { get; set; } = string.Empty;
        public string JwtAudience { get; set; } = string.Empty;
        public int JwtExpirationHours { get; set; } = 8;

        /// <summary>
        /// Email verification required
        /// </summary>
        public bool RequireEmailVerification { get; set; } = false;

        /// <summary>
        /// Account lockout settings
        /// </summary>
        public bool LockoutEnabled { get; set; } = true;
        public int MaxFailedAccessAttempts { get; set; } = 5;
        public int LockoutTimeSpanMinutes { get; set; } = 30;

        /// <summary>
        /// Password requirements
        /// </summary>
        public PasswordRequirements PasswordRequirements { get; set; } = new PasswordRequirements();

        /// <summary>
        /// Session settings
        /// </summary>
        public int SessionTimeoutMinutes { get; set; } = 480; // 8 hours
        public bool RequireHttps { get; set; } = false;
        public bool RequireUniqueEmail { get; set; } = true;

        /// <summary>
        /// Remember me settings
        /// </summary>
        public bool AllowRememberMe { get; set; } = true;
        public int RememberMeDays { get; set; } = 30;
    }

    /// <summary>
    /// Password requirements configuration
    /// </summary>
    public class PasswordRequirements
    {
        public int MinLength { get; set; } = 6;
        public bool RequireDigit { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireUppercase { get; set; } = false;
        public bool RequireSpecialCharacter { get; set; } = false;
        public int MaxLength { get; set; } = 100;
    }
}