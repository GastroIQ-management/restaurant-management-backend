namespace RestaurantManagement.Shared.Data.Idempotency
{
    /// <summary>What a reserve found for a key: it is new (reserved now), still running, used by another request, or finished (replay).</summary>
    public enum IdempotencyOutcome
    {
        Reserved,
        InProgress,
        Reused,
        Replay
    }
}
