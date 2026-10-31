namespace RestaurantManagement.Shared.Services.Idempotency
{
    /// <summary>Marks an action that ignores the <c>Idempotency-Key</c> header, for example one whose answer carries a token or cookie.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class IgnoreIdempotencyKeyAttribute : Attribute
    {
    }
}
