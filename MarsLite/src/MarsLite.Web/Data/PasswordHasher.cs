using System;
using System.Security.Cryptography;
using System.Text;

namespace MarsLite.Web.Data
{
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("marslite-salt:" + password));
                return BitConverter.ToString(bytes).Replace("-", string.Empty).ToUpperInvariant();
            }
        }

        public static bool Verify(string password, string hash) =>
            string.Equals(Hash(password), hash, StringComparison.OrdinalIgnoreCase);
    }
}
