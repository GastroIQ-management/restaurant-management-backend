namespace RestaurantManagement.Modules.Menu.Models
{
    /// <summary>A link between a menu and a location, as a device syncs it.</summary>
    public class LocationMenuResponse
    {
        public Guid Id { get; set; }
        public Guid MenuId { get; set; }
        public Guid LocationId { get; set; }
        public bool IsActive { get; set; }
    }
}
