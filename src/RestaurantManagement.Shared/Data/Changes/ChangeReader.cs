using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Shared.Models;
using RestaurantManagement.Shared.Services.Changes;

namespace RestaurantManagement.Shared.Data.Changes
{
    /// <summary>The ID and stamp of one record or tombstone, projected by the module from its own scoped query.</summary>
    /// <remarks>Initialized by member (not by constructor) so EF Core can filter and sort on the projected stamp in SQL.</remarks>
    public sealed record ChangeRow
    {
        public Guid Id { get; init; }
        public long Stamp { get; init; }
    }

    /// <summary>
    /// One "changes since" read. <see cref="Records"/> and <see cref="Tombstones"/> are the module's queries already limited to the
    /// token organization, header location and access; the reader only applies the cursor.
    /// </summary>
    public sealed record ChangeFeedRead<TItem>(
        string Feed,
        Guid OrganizationId,
        Guid? LocationId,
        ChangesQuery Query,
        IQueryable<ChangeRow> Records,
        IQueryable<ChangeRow> Tombstones,
        Func<IReadOnlyList<Guid>, CancellationToken, Task<IReadOnlyDictionary<Guid, TItem>>> LoadItems);

    /// <summary>
    /// Reads a page of changes. The next lower bound is the oldest transaction still running when the run began (read before any row),
    /// so a write that commits late is never missed; it may come back twice. Tombstones are only removed by cursor age, never here.
    /// </summary>
    public sealed class ChangeReader
    {
        private const string SnapshotXminSql = "SELECT CAST(CAST(pg_snapshot_xmin(pg_current_snapshot()) AS text) AS bigint) AS \"Value\"";

        private readonly ChangeCursorCodec _codec;
        private readonly TimeProvider _timeProvider;

        public ChangeReader(ChangeCursorCodec codec, TimeProvider timeProvider)
        {
            _codec = codec;
            _timeProvider = timeProvider;
        }

        public async Task<ChangesPageResponse<TItem>> ReadAsync<TItem>(DbContext context, ChangeFeedRead<TItem> read, CancellationToken cancellationToken)
        {
            var cursor = read.Query.Cursor is null
                ? null
                : _codec.Decode(read.Query.Cursor, read.Feed, read.OrganizationId, read.LocationId);

            var inRun = cursor?.LastStamp is not null && cursor.LastId is not null;
            var nextBound = inRun ? cursor!.NextBound : await ReadSnapshotXminAsync(context, cancellationToken);
            var lower = cursor?.LowerBound ?? 0;
            ChangePosition? after = inRun ? new ChangePosition(cursor!.LastStamp!.Value, cursor.LastId!.Value) : null;

            var pageSize = read.Query.EffectivePageSize;
            var records = await ReadRowsAsync(read.Records, lower, after, pageSize, false, cancellationToken);
            var tombstones = await ReadRowsAsync(read.Tombstones, lower, after, pageSize, true, cancellationToken);

            // A source cut at pageSize + 1 says nothing about rows past its last one, so no entry past the earliest cut may be
            // handed out: a later entry for the same ID would hide an unread one and the page would skip it.
            var limit = EarliestCut(records.CutAt, tombstones.CutAt);
            var page = ChangePageBuilder.Build(WithinLimit(records.Entries, limit), WithinLimit(tombstones.Entries, limit), after, pageSize);
            var hasMore = page.HasMore || limit is not null;
            return await BuildResponseAsync(read, page, hasMore, lower, nextBound, cancellationToken);
        }

        private static ChangePosition? EarliestCut(ChangePosition? first, ChangePosition? second)
        {
            if (first is null || second is null)
                return first ?? second;
            return ChangePageBuilder.IsAfter(first.Value.Stamp, first.Value.Id, second.Value) ? second : first;
        }

        private static IEnumerable<ChangeEntry> WithinLimit(IEnumerable<ChangeEntry> entries, ChangePosition? limit)
            => limit is { } position ? entries.Where(e => !ChangePageBuilder.IsAfter(e.Stamp, e.Id, position)) : entries;

        private static async Task<(List<ChangeEntry> Entries, ChangePosition? CutAt)> ReadRowsAsync(IQueryable<ChangeRow> source, long lower, ChangePosition? after, int pageSize, bool isDeleted, CancellationToken cancellationToken)
        {
            var floor = source.Where(r => r.Stamp >= lower);
            var rows = new List<ChangeRow>();

            // Rows sharing the last stamp are read whole: the page builder drops those at or before the last ID.
            if (after is { } position)
            {
                rows.AddRange(await floor.Where(r => r.Stamp == position.Stamp).ToListAsync(cancellationToken));
                floor = floor.Where(r => r.Stamp > position.Stamp);
            }

            var next = await floor.OrderBy(r => r.Stamp).ThenBy(r => r.Id).Take(pageSize + 1).ToListAsync(cancellationToken);
            rows.AddRange(next);

            ChangePosition? cutAt = next.Count > pageSize ? new ChangePosition(next[^1].Stamp, next[^1].Id) : null;
            return (rows.Select(r => new ChangeEntry(r.Id, r.Stamp, isDeleted)).ToList(), cutAt);
        }

        private async Task<ChangesPageResponse<TItem>> BuildResponseAsync<TItem>(
            ChangeFeedRead<TItem> read, ChangePage page, bool hasMore, long lower, long nextBound, CancellationToken cancellationToken)
        {
            var changedIds = page.Entries.Where(e => !e.IsDeleted).Select(e => e.Id).ToList();
            var items = changedIds.Count == 0
                ? new Dictionary<Guid, TItem>()
                : await read.LoadItems(changedIds, cancellationToken);

            var last = page.Entries.Count > 0 ? page.Entries[^1] : (ChangeEntry?)null;
            var cursor = hasMore && last is { } position
                ? new ChangeCursor(read.Feed, read.OrganizationId, read.LocationId, _timeProvider.GetUtcNow(), lower, nextBound, position.Stamp, position.Id)
                : new ChangeCursor(read.Feed, read.OrganizationId, read.LocationId, _timeProvider.GetUtcNow(), nextBound, nextBound, null, null);

            return new ChangesPageResponse<TItem>
            {
                Changed = changedIds.Where(items.ContainsKey).Select(id => items[id]).ToList(),
                Deleted = page.Entries.Where(e => e.IsDeleted).Select(e => e.Id).ToList(),
                Cursor = _codec.Encode(cursor),
                HasMore = hasMore,
            };
        }

        private static Task<long> ReadSnapshotXminAsync(DbContext context, CancellationToken cancellationToken)
            => context.Database.SqlQueryRaw<long>(SnapshotXminSql).SingleAsync(cancellationToken);
    }
}
