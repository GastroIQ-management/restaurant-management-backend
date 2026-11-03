namespace RestaurantManagement.Shared.Data.Changes
{
    /// <summary>The SQL texts module migrations run to stamp rows and leave tombstones.</summary>
    public static class ChangeTrackingSql
    {
        /// <summary>A trigger that sets <c>change_stamp</c> on insert and update of <paramref name="table"/>; names carry the module <paramref name="prefix"/>.</summary>
        public static string StampTrigger(string prefix, string table) => throw new NotImplementedException();
    }
}
