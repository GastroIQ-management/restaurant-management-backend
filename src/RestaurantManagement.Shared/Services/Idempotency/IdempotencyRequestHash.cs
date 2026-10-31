using System.Security.Cryptography;
using System.Text;

namespace RestaurantManagement.Shared.Services.Idempotency
{
    public static class IdempotencyRequestHash
    {
        /// <summary>SHA-256 (hex) of the method, path with query, <c>X-Location-Id</c> value and raw body.</summary>
        public static string Compute(string method, string pathAndQuery, string? locationHeader, byte[] body)
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            AppendPart(hash, method);
            AppendPart(hash, pathAndQuery);
            AppendPart(hash, locationHeader ?? string.Empty);
            hash.AppendData(body);
            return Convert.ToHexString(hash.GetHashAndReset());
        }

        // A length prefix keeps "ab"+"c" and "a"+"bc" from hashing the same.
        private static void AppendPart(IncrementalHash hash, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            hash.AppendData(BitConverter.GetBytes(bytes.Length));
            hash.AppendData(bytes);
        }
    }
}
