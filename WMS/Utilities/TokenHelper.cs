using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WMS.Configuration;
using WMS.Models;

namespace WMS.Utilities
{
    /// <summary>
    /// Helper class untuk JWT token operations
    /// </summary>
    public class TokenHelper
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<TokenHelper> _logger;

        public TokenHelper(JwtSettings jwtSettings, ILogger<TokenHelper> logger)
        {
            _jwtSettings = jwtSettings;
            _logger = logger;
        }

        /// <summary>
        /// Generate JWT token untuk user
        /// </summary>
        /// <param name="user">User object</param>
        /// <param name="roles">User roles</param>
        /// <returns>JWT token string</returns>
        public string GenerateJwtToken(User user, IEnumerable<string> roles)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("FullName", user.FullName),
                    new Claim("CompanyId", user.CompanyId.ToString()),
                    new Claim("UserId", user.Id.ToString())
                };

                // Add role claims
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
                    Issuer = _jwtSettings.Issuer,
                    Audience = _jwtSettings.Audience,
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogDebug("JWT token generated for user {Username} (ID: {UserId})", user.Username, user.Id);
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user {Username}", user.Username);
                throw new InvalidOperationException("Failed to generate JWT token", ex);
            }
        }

        /// <summary>
        /// Validate JWT token
        /// </summary>
        /// <param name="token">JWT token string</param>
        /// <returns>ClaimsPrincipal jika valid, null jika tidak</returns>
        public ClaimsPrincipal? ValidateJwtToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = _jwtSettings.ValidateIssuer,
                    ValidateAudience = _jwtSettings.ValidateAudience,
                    ValidateLifetime = _jwtSettings.ValidateLifetime,
                    ValidateIssuerSigningKey = _jwtSettings.ValidateIssuerSigningKey,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidAudience = _jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = _jwtSettings.ClockSkew
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "JWT token validation failed");
                return null;
            }
        }

        /// <summary>
        /// Extract claims dari JWT token
        /// </summary>
        /// <param name="token">JWT token string</param>
        /// <returns>Dictionary of claims</returns>
        public Dictionary<string, string> GetClaimsFromToken(string token)
        {
            var claims = new Dictionary<string, string>();

            try
            {
                var principal = ValidateJwtToken(token);
                if (principal?.Identity is ClaimsIdentity identity)
                {
                    foreach (var claim in identity.Claims)
                    {
                        claims[claim.Type] = claim.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error extracting claims from token");
            }

            return claims;
        }

        /// <summary>
        /// Check if token is expired
        /// </summary>
        /// <param name="token">JWT token string</param>
        /// <returns>True jika expired</returns>
        public bool IsTokenExpired(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                if (tokenHandler.ReadToken(token) is JwtSecurityToken jwtToken)
                {
                    return jwtToken.ValidTo < DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error checking token expiration");
            }

            return true; // Consider invalid tokens as expired
        }

        /// <summary>
        /// Generate refresh token
        /// </summary>
        /// <returns>Refresh token string</returns>
        public string GenerateRefreshToken()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var randomBytes = new byte[64];
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Generate secure token untuk password reset, email verification, etc.
        /// </summary>
        /// <returns>Secure token string</returns>
        public string GenerateSecureToken()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var tokenBytes = new byte[32];
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}