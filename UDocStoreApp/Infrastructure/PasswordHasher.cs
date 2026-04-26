using System.Security.Cryptography;
using System.Text;

namespace UDocStoreApp.Infrastructure
{
    public static class PasswordHasher
    {
        public static string GetMD5Hash(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            using (MD5 md5 = MD5.Create())
            {
                // Используем UTF8 вместо ASCII
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("X2")); // Формат 21232F29...

                return sb.ToString();
            }
        }
    }
}