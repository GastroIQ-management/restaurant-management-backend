namespace RestaurantManagement.Shared.Models
{
    /// <summary>Query of a "changes since" read. There is no organization or location field: both come from the token and header.</summary>
    public class ChangesQuery
    {
        public const int DefaultPageSize = 100;
        public const int MaxPageSize = 500;

        public string? Cursor { get; set; }
        public int? PageSize { get; set; }

        public int EffectivePageSize => throw new NotImplementedException();
    }
}
