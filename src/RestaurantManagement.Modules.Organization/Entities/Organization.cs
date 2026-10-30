using RestaurantManagement.Shared.Data.Entities;
using RestaurantManagement.Shared.Data.Versioning;
using RestaurantManagement.Modules.Organization.Entities;

namespace RestaurantManagement.Modules.Organization.Entities
{
    public class Organization : BaseEntity, IVersionedEntity
    {
        public uint Version { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? AccentColor { get; set; }
        public bool IsActive { get; set; } = true;

        public Currency Currency { get; set; }

        // Navigation Properties
        public OrganizationSettings? Settings { get; set; }
        public ICollection<Location> Locations { get; set; } = new List<Location>();
    }
}