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
        {
            // One ID keeps only its latest entry; on an equal stamp the record wins (deleted and recreated in one transaction).
            var latest = records.Concat(tombstones)
                .GroupBy(e => e.Id)
                .Select(g => g.OrderBy(e => e.Stamp).ThenBy(e => e.IsDeleted ? 0 : 1).Last());

            var ordered = latest
                .Where(e => after is null || Compare(e.Stamp, e.Id, after.Value.Stamp, after.Value.Id) > 0)
                .OrderBy(e => e.Stamp)
                .ThenBy(e => e.Id, GuidByteOrder.Instance)
                .ToList();

            return new ChangePage(ordered.Take(pageSize).ToList(), ordered.Count > pageSize);
        }

        private static int Compare(long stamp, Guid id, long otherStamp, Guid otherId)
            => stamp != otherStamp ? stamp.CompareTo(otherStamp) : GuidByteOrder.Instance.Compare(id, otherId);

        /// <summary>The order PostgreSQL sorts a uuid in (byte by byte, first byte of the text first), so memory and database agree.</summary>
        private sealed class GuidByteOrder : IComparer<Guid>
        {
            public static readonly GuidByteOrder Instance = new();

            public int Compare(Guid x, Guid y)
                => x.ToByteArray(bigEndian: true).AsSpan().SequenceCompareTo(y.ToByteArray(bigEndian: true));
        }
    }
}
