namespace WMS.Configuration
{
    /// <summary>
    /// Authentication configuration settings
    /// </summary>
    public class AuthenticationSettings
    {
        /// <summary>
        /// JWT Secret Key
        /// </summary>
        public string JwtSecretKey { get; set; } = string.Empty;

        /// <summary>
        /// JWT Issuer
        /// </summary>
        public string JwtIssuer { get; set; } = string.Empty;

        /// <summary>
        /// JWT Audience
        /// </summary>
        public string JwtAudience { get; set; } = string.Empty;

        /// <summary>
        /// JWT expiration dalam jam
        /// </summary>
        public int JwtExpirationHours { get; set; } = 8;

        /// <summary>
        /// Cookie expiration dalam jam
        /// </summary>
        public int CookieExpirationHours { get; set; } = 8;

        /// <summary>
        /// Sliding expiration untuk cookie
        /// </summary>
        public bool SlidingExpiration { get; set; } = true;

        /// <summary>
        /// Nama cookie
        /// </summary>
        public string CookieName { get; set; } = "WMS.Auth";

        /// <summary>
        /// Session timeout dalam jam
        /// </summary>
        public int SessionTimeoutHours { get; set; } = 2;

        /// <summary>
        /// Require email verification
        /// </summary>
        public bool RequireEmailVerification { get; set; } = false;

        /// <summary>
        /// Enable account lockout
        /// </summary>
        public bool LockoutEnabled { get; set; } = true;

        /// <summary>
        /// Maximum failed login attempts
        /// </summary>
        public int MaxFailedAccessAttempts { get; set; } = 5;

        /// <summary>
        /// Lockout duration dalam menit
        /// </summary>
        public int LockoutTimeSpanMinutes { get; set; } = 30;

        /// <summary>
        /// Password requirements
        /// </summary>
        public PasswordRequirements PasswordRequirements { get; set; } = new PasswordRequirements();
    }

    /// <summary>
    /// Password requirements configuration
    /// </summary>
    public class PasswordRequirements
    {
        /// <summary>
        /// Minimum password length
        /// </summary>
        public int MinLength { get; set; } = 6;

        /// <summary>
        /// Require digit
        /// </summary>
        public bool RequireDigit { get; set; } = true;

        /// <summary>
        /// Require lowercase letter
        /// </summary>
        public bool RequireLowercase { get; set; } = true;

        /// <summary>
        /// Require uppercase letter
        /// </summary>
        public bool RequireUppercase { get; set; } = false;

        /// <summary>
        /// Require special character
        /// </summary>
        public bool RequireSpecialCharacter { get; set; } = false;
    }
}