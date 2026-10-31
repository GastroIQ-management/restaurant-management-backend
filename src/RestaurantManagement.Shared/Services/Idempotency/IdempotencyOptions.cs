namespace RestaurantManagement.Shared.Services.Idempotency
{
    /// <summary>Settings section <c>Idempotency</c>.</summary>
    public class IdempotencyOptions
    {
        public const string SectionName = "Idempotency";

        public int RetentionDays { get; set; } = 8;
        public int InProgressTimeoutSeconds { get; set; } = 120;
        public bool CleanupEnabled { get; set; } = true;
        public int CleanupIntervalMinutes { get; set; } = 60;
    }
}
