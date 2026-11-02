using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagement.Shared.Data.Entities
{
    public abstract class LocationEntity : TenantEntity
    {
        public Guid LocationId { get; set; }

        // Stub: [NotMapped] keeps the model unchanged until the migrations exist. Remove it in T2.
        [NotMapped] public DateOnly? BusinessDay { get; set; }
    }
}
