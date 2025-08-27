using System.Security.Cryptography;
using System.Text;

namespace WMS.Utilities
{
    /// <summary>
    /// Password helper untuk enkripsi password tanpa salt
    /// Menggunakan SHA256 dengan pepper untuk keamanan tambahan
    /// </summary>
    public static class PasswordHelper
    {
        // Static pepper untuk semua password - lebih aman dari no salt
        private const string PEPPER = "WMS_PASSWORD_PEPPER_2024_SECURE_KEY";

        /// <summary>
        /// Hash password menggunakan SHA256 dengan pepper
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <returns>Hashed password</returns>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            // Combine password dengan pepper
            var passwordWithPepper = password + PEPPER;

            // Convert ke bytes
            var inputBytes = Encoding.UTF8.GetBytes(passwordWithPepper);

            // Hash dengan SHA256
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(inputBytes);

            // Convert ke Base64 string
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Verify password dengan hash yang ada
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <param name="hashedPassword">Hashed password dari database</param>
        /// <returns>True jika password cocok</returns>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            try
            {
                var hashOfInput = HashPassword(password);
                return hashOfInput.Equals(hashedPassword, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generate random password untuk user baru
        /// </summary>
        /// <param name="length">Panjang password (default 8)</param>
        /// <returns>Random password</returns>
        public static string GenerateRandomPassword(int length = 8)
        {
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            var password = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                password.Append(chars[random.Next(chars.Length)]);
            }

            return password.ToString();
        }

        /// <summary>
        /// Generate reset password token
        /// </summary>
        /// <returns>Reset token</returns>
        public static string GenerateResetToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        /// <summary>
        /// Validate password strength
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <returns>Validation result</returns>
        public static PasswordValidationResult ValidatePassword(string password)
        {
            var result = new PasswordValidationResult();

            if (string.IsNullOrEmpty(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password tidak boleh kosong");
                return result;
            }

            if (password.Length < 6)
            {
                result.IsValid = false;
                result.Errors.Add("Password minimal 6 karakter");
            }

            if (!password.Any(char.IsDigit))
            {
                result.IsValid = false;
                result.Errors.Add("Password harus mengandung minimal 1 angka");
            }

            if (!password.Any(char.IsLower))
            {
                result.IsValid = false;
                result.Errors.Add("Password harus mengandung minimal 1 huruf kecil");
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }
    }

    /// <summary>
    /// Result dari validasi password
    /// </summary>
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
    }
}