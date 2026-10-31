namespace RestaurantManagement.Shared.Data.Idempotency
{
    /// <summary>The result of a reserve. <see cref="Response"/> is set only for <see cref="IdempotencyOutcome.Replay"/>.</summary>
    public sealed record IdempotencyReservation(IdempotencyOutcome Outcome, StoredResponse? Response);
}
