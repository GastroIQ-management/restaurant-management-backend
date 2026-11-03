namespace RestaurantManagement.Shared.Models
{
    /// <summary>One page of a "changes since" read: changed records, IDs of deleted records, the next cursor and whether more remain.</summary>
    public class ChangesPageResponse<T>
    {
        public List<T> Changed { get; set; } = new();
        public List<Guid> Deleted { get; set; } = new();
        public string Cursor { get; set; } = string.Empty;
        public bool HasMore { get; set; }
    }
}
