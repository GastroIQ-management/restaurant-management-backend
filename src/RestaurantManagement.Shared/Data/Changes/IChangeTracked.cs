namespace RestaurantManagement.Shared.Data.Changes
{
    /// <summary>A row stamped by the database with the transaction that last wrote it.</summary>
    public interface IChangeTracked
    {
        long ChangeStamp { get; set; }
    }
}
