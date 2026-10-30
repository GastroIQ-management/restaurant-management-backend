namespace RestaurantManagement.Shared.Services.Idempotency
{
    public static class IdempotencyRequestHash
    {
        /// <summary>SHA-256 (hex) of the method, path with query, <c>X-Location-Id</c> value and raw body.</summary>
        public static string Compute(string method, string pathAndQuery, string? locationHeader, byte[] body)
            => throw new NotImplementedException();
    }
}
