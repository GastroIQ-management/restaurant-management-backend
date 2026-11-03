namespace RestaurantManagement.Shared.Data.Changes
{
    /// <summary>A small note left when a record is removed, so a device can learn it is gone.</summary>
    public class ChangeTombstone
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? LocationId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public long ChangeStamp { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
