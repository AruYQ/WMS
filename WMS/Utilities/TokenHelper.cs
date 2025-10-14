using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WMS.Configuration;
using WMS.Models;

namespace WMS.Utilities
{
    /// <summary>
    /// Helper untuk JWT token operations
    /// </summary>
    public class TokenHelper
    {
        private readonly JwtSettings _jwtSettings;

        public TokenHelper(JwtSettings jwtSettings)
        {
            _jwtSettings = jwtSettings;
        }

        /// <summary>
        /// Generate JWT token untuk user (with permissions)
        /// </summary>
        /// <param name="user">User object</param>
        /// <param name="roles">User roles</param>
        /// <param name="permissions">User permissions (optional - for optimization)</param>
        /// <returns>JWT token string</returns>
        public string GenerateJwtToken(User user, IEnumerable<string> roles, IEnumerable<string>? permissions = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName),
                new Claim("CompanyId", user.CompanyId.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            // Add company information (if available)
            if (user.Company != null)
            {
                claims.Add(new Claim("CompanyCode", user.Company.Code ?? ""));
                claims.Add(new Claim("CompanyName", user.Company.Name ?? ""));
            }

            // Add roles sebagai claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add permissions sebagai claims (OPTIMIZATION for permission checking)
            if (permissions != null)
            {
                foreach (var permission in permissions.Distinct())
                {
                    claims.Add(new Claim("Permission", permission));
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Validate JWT token
        /// </summary>
        /// <param name="token">JWT token string</param>
        /// <returns>ClaimsPrincipal jika valid, null jika tidak</returns>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = _jwtSettings.ValidateIssuerSigningKey,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = _jwtSettings.ValidateIssuer,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = _jwtSettings.ValidateAudience,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = _jwtSettings.ValidateLifetime,
                    ClockSkew = _jwtSettings.ClockSkew
                };

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extract claims dari JWT token
        /// </summary>
        /// <param name="token">JWT token string</param>
        /// <returns>Dictionary of claims</returns>
        public Dictionary<string, string> GetTokenClaims(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);

                return jsonToken.Claims.ToDictionary(
                    claim => claim.Type,
                    claim => claim.Value
                );
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Check jika token sudah expired
        /// </summary>
        /// <param name="token">JWT token string</param>
        /// <returns>True jika expired</returns>
        public bool IsTokenExpired(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);

                return jsonToken.ValidTo < DateTime.UtcNow;
            }
            catch
            {
                return true;
            }
        }
    }
}