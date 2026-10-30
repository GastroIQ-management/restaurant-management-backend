using RestaurantManagement.Shared.Data.Entities;
using RestaurantManagement.Shared.Data.Versioning;

namespace RestaurantManagement.Modules.Identity.Entities.Roles
{
    /// <summary>
    /// A role stored per organization (Admin, Manager, Cashier, Waiter, Kitchen at first). It only
    /// holds default levels; a member's actual levels are stored per location.
    /// </summary>
    public class OrganizationRole : TenantEntity, IVersionedEntity
    {
        public uint Version { get; set; }

        public const int NameMaxLength = 100;

        public string Name { get; set; } = null!;
    }
}
