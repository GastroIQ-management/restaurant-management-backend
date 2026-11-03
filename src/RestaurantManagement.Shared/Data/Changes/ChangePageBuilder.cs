namespace RestaurantManagement.Shared.Data.Changes
{
    /// <summary>One changed record (<see cref="IsDeleted"/> false) or one tombstone (true) with its stamp.</summary>
    public readonly record struct ChangeEntry(Guid Id, long Stamp, bool IsDeleted);

    /// <summary>The last (stamp, ID) position handed out; the next page starts strictly after it.</summary>
    public readonly record struct ChangePosition(long Stamp, Guid Id);

    public sealed record ChangePage(IReadOnlyList<ChangeEntry> Entries, bool HasMore);

    /// <summary>Pure merge and paging of record and tombstone entries.</summary>
    public static class ChangePageBuilder
    {
        public static ChangePage Build(IEnumerable<ChangeEntry> records, IEnumerable<ChangeEntry> tombstones, ChangePosition? after, int pageSize)
            => throw new NotImplementedException();
    }
}
