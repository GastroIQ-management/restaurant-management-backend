namespace RestaurantManagement.Shared.Data.Idempotency
{
    /// <summary>The answer saved for a finished key.</summary>
    public sealed record StoredResponse(int Status, string? ContentType, byte[] Body, string? Location);
}
