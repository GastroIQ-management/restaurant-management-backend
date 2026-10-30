namespace RestaurantManagement.Shared.Data.Versioning
{
    /// <summary>
    /// An entity whose rows carry a version (PostgreSQL `xmin`, the hidden per-row number that changes on every update).
    /// </summary>
    public interface IVersionedEntity
    {
        uint Version { get; set; }
    }
}
