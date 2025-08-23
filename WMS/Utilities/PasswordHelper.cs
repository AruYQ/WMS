using System.Security.Cryptography;
using System.Text;
using WMS.Configuration;

namespace WMS.Utilities
{
    /// <summary>
    /// Helper class untuk password hashing dan validation
    /// </summary>
    public static class PasswordHelper
    {
        private const int SaltSize = 128 / 8; // 128 bits
        private const int KeySize = 256 / 8; // 256 bits
        private const int Iterations = 10000;

        /// <summary>
        /// Generate random salt untuk password hashing
        /// </summary>
        /// <returns>Base64 encoded salt</returns>
        public static string GenerateSalt()
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        /// <summary>
        /// Hash password menggunakan PBKDF2 dengan salt
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <param name="salt">Salt dalam format Base64</param>
        /// <returns>Hashed password dalam format Base64</returns>
        public static string HashPassword(string password, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(KeySize);

            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Verify password terhadap hash yang tersimpan
        /// </summary>
        /// <param name="password">Plain text password yang akan diverifikasi</param>
        /// <param name="hash">Stored password hash</param>
        /// <param name="salt">Salt yang digunakan untuk hashing</param>
        /// <returns>True jika password cocok</returns>
        public static bool VerifyPassword(string password, string hash, string salt)
        {
            var hashToCompare = HashPassword(password, salt);
            return hashToCompare == hash;
        }

        /// <summary>
        /// Validate password berdasarkan requirements
        /// </summary>
        /// <param name="password">Password yang akan divalidasi</param>
        /// <param name="requirements">Password requirements</param>
        /// <returns>Validation result dengan error messages jika ada</returns>
        public static PasswordValidationResult ValidatePassword(string password, PasswordRequirements requirements)
        {
            var result = new PasswordValidationResult { IsValid = true };
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password tidak boleh kosong");
                result.IsValid = false;
            }
            else
            {
                if (password.Length < requirements.MinLength)
                {
                    errors.Add($"Password minimal {requirements.MinLength} karakter");
                    result.IsValid = false;
                }

                if (password.Length > requirements.MaxLength)
                {
                    errors.Add($"Password maksimal {requirements.MaxLength} karakter");
                    result.IsValid = false;
                }

                if (requirements.RequireDigit && !password.Any(char.IsDigit))
                {
                    errors.Add("Password harus mengandung minimal 1 angka");
                    result.IsValid = false;
                }

                if (requirements.RequireLowercase && !password.Any(char.IsLower))
                {
                    errors.Add("Password harus mengandung minimal 1 huruf kecil");
                    result.IsValid = false;
                }

                if (requirements.RequireUppercase && !password.Any(char.IsUpper))
                {
                    errors.Add("Password harus mengandung minimal 1 huruf besar");
                    result.IsValid = false;
                }

                if (requirements.RequireSpecialCharacter && !password.Any(ch => !char.IsLetterOrDigit(ch)))
                {
                    errors.Add("Password harus mengandung minimal 1 karakter khusus");
                    result.IsValid = false;
                }
            }

            result.ErrorMessages = errors;
            return result;
        }

        /// <summary>
        /// Generate secure random password
        /// </summary>
        /// <param name="length">Panjang password</param>
        /// <param name="includeSpecialChars">Include karakter khusus</param>
        /// <returns>Random password</returns>
        public static string GenerateRandomPassword(int length = 12, bool includeSpecialChars = true)
        {
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var chars = lowercase + uppercase + digits;
            if (includeSpecialChars)
                chars += specialChars;

            using var rng = RandomNumberGenerator.Create();
            var password = new StringBuilder();
            var bytes = new byte[4];

            // Ensure at least one character from each required set
            password.Append(lowercase[GetRandomIndex(rng, bytes, lowercase.Length)]);
            password.Append(uppercase[GetRandomIndex(rng, bytes, uppercase.Length)]);
            password.Append(digits[GetRandomIndex(rng, bytes, digits.Length)]);

            if (includeSpecialChars)
                password.Append(specialChars[GetRandomIndex(rng, bytes, specialChars.Length)]);

            // Fill the rest randomly
            var remainingLength = length - password.Length;
            for (int i = 0; i < remainingLength; i++)
            {
                password.Append(chars[GetRandomIndex(rng, bytes, chars.Length)]);
            }

            // Shuffle the password
            return new string(password.ToString().ToCharArray().OrderBy(x => Guid.NewGuid()).ToArray());
        }

        private static int GetRandomIndex(RandomNumberGenerator rng, byte[] bytes, int maxValue)
        {
            rng.GetBytes(bytes);
            var value = BitConverter.ToUInt32(bytes, 0);
            return (int)(value % maxValue);
        }
    }

    /// <summary>
    /// Result dari password validation
    /// </summary>
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
    }
}