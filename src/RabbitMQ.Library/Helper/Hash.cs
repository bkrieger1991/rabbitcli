using System.Security.Cryptography;
using System.Text;

namespace RabbitMQ.Library.Helper
{
    public class Hash
    {
        public static string GetShortHash(string input)
        {
            return GetHash(input).Substring(0, 12);
        }

        public static string GetHash(string input)
        {
            using var shaHash = SHA256.Create();
            var bytes = shaHash.ComputeHash(Encoding.UTF8.GetBytes(input));
            // Convert byte array to a string   
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}