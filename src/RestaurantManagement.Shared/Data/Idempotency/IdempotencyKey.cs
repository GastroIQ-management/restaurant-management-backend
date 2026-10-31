namespace RestaurantManagement.Shared.Data.Idempotency
{
    /// <summary>
    /// One idempotency key a signed-in user sent with a write, and the answer saved for it.
    /// A plain entity (not a tenant entity): creating an organization has no organization in the token yet.
    /// Only a hash of the request is stored, never its body.
    /// </summary>
    public class IdempotencyKey
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid Key { get; set; }
        public string RequestHash { get; set; } = null!;
        public IdempotencyKeyState State { get; set; }
        public int? ResponseStatus { get; set; }
        public string? ResponseContentType { get; set; }
        public byte[]? ResponseBody { get; set; }
        public string? ResponseLocation { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
