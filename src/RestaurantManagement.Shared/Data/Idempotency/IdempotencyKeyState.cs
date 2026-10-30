namespace RestaurantManagement.Shared.Data.Idempotency
{
    /// <summary>Where a stored idempotency key stands: its first request is still running, or it finished and its answer is saved.</summary>
    public enum IdempotencyKeyState
    {
        InProgress,
        Completed
    }
}
