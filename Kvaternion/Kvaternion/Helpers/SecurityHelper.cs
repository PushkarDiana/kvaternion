using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Kvaternion.Helpers
{
    public static class SecurityHelper
    {
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
