using System.ComponentModel.DataAnnotations;
using RestaurantManagement.Shared.Data.Entities;
using RestaurantManagement.Shared.Data.Versioning;

namespace RestaurantManagement.Modules.Menu.Entities
{
    public class Menu : TenantEntity, IVersionedEntity
    {
        public uint Version { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property (within same module)
        public ICollection<LocationMenu> LocationMenus { get; set; } = new List<LocationMenu>();
    }
}
