using System.Security.Cryptography;
using System.Text;

namespace FypTracker.Services
{
    // Title + description ka ek chhota "nishan" (64 characters) banata hai.
    // Do projects ka nishan same ho to wo ek hi project hai (jaise COMSATS ka
    // "Smart Attendance Management System", jo 2021 aur 2024 dono mein aaya).
    // Capital/small letters, extra spaces aur punctuation is mein farq nahi daalte.
    public static class ContentHasher
    {
        public static string Create(string? title, string? description)
        {
            string text = Normalize(title) + "|" + Normalize(description);
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(bytes);   // SHA-256 = 64 hex characters
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            bool lastWasSpace = false;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(char.ToLowerInvariant(c));
                    lastWasSpace = false;
                }
                else if (!lastWasSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                    lastWasSpace = true;
                }
            }

            return builder.ToString().Trim();
        }
    }
}