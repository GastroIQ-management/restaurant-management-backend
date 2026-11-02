using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagement.Shared.Data.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public Guid? UpdatedByUserId { get; set; }

        // Stub: [NotMapped] keeps the model unchanged until the migrations exist. Remove it in T2.
        [NotMapped] public DateTime? ClientActionTime { get; set; }
        [NotMapped] public int? ClientActionOffsetMinutes { get; set; }
        [NotMapped] public DateTime? CorrectedActionTime { get; set; }
        [NotMapped] public TimeSpan? ClockError { get; set; }
        [NotMapped] public DateTime? ReceivedAt { get; set; }
    }
}
