namespace RestaurantManagement.Shared.Services.Idempotency
{
    /// <summary>Settings section <c>Idempotency</c>.</summary>
    public class IdempotencyOptions
    {
        public const string SectionName = "Idempotency";

        public int RetentionDays { get; set; }
        public int InProgressTimeoutSeconds { get; set; }
        public bool CleanupEnabled { get; set; }
        public int CleanupIntervalMinutes { get; set; }
    }
}
