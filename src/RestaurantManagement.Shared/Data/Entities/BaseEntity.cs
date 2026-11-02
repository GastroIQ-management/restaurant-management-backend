namespace RestaurantManagement.Shared.Data.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public Guid? UpdatedByUserId { get; set; }

        public DateTime? ClientActionTime { get; set; }
        public int? ClientActionOffsetMinutes { get; set; }
        public DateTime? CorrectedActionTime { get; set; }
        public TimeSpan? ClockError { get; set; }
        public DateTime? ReceivedAt { get; set; }
    }
}
