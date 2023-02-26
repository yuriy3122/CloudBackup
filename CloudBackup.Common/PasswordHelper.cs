using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace CloudBackup.Common
{
    public struct PasswordHashSalt
    {
        public string Hash { get; }

        public string Salt { get; }

        public PasswordHashSalt(string hash, string salt)
        {
            Hash = hash;
            Salt = salt;
        }
    }

    public static class PasswordHelper
    {
        public static PasswordHashSalt GetPasswordHashSalt(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return new PasswordHashSalt(string.Empty, string.Empty);
            }

            // generate a 128-bit salt using a secure PRNG
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // derive a 256-bit subkey (use HMACSHA1 with 10,000 iterations)
            string hashedPassword = HashPassword(password, salt);

            return new PasswordHashSalt(hashedPassword, Convert.ToBase64String(salt));
        }

        public static string GetPasswordHash(string password, string salt)
        {
            return HashPassword(password, Convert.FromBase64String(salt));
        }

        private static string HashPassword(string password, byte[] salt)
        {
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
        }
    }
}
