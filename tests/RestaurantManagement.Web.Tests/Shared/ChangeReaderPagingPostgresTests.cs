using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Shared.Data.Changes;
using RestaurantManagement.Shared.Models;
using RestaurantManagement.Shared.Services.Changes;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>The change reader's paging on a real PostgreSQL database, with records and tombstones given as fixed (ID, stamp) rows.</summary>
public class ChangeReaderPagingPostgresTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Feed = "test-feed";

    private static readonly Guid OrganizationId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid M1 = Guid.Parse("00000000-0000-4000-8000-0000000000a1");
    private static readonly Guid M2 = Guid.Parse("00000000-0000-4000-8000-0000000000a2");
    private static readonly Guid Z = Guid.Parse("00000000-0000-4000-8000-0000000000ff");

    private readonly PostgresDatabaseFixture _postgres;

    public ChangeReaderPagingPostgresTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private sealed class EmptyContext(DbContextOptions<EmptyContext> options) : DbContext(options);

    private static IQueryable<ChangeRow> Rows(EmptyContext db, params (Guid Id, long Stamp)[] rows)
    {
        var values = string.Join(", ", rows.Select(r => $"('{r.Id}'::uuid, {r.Stamp}::bigint)"));
#pragma warning disable EF1002 // fixed IDs and numbers from this test, no input
        return db.Database.SqlQueryRaw<ChangeRow>($"SELECT * FROM (VALUES {values}) AS v(\"Id\", \"Stamp\")");
#pragma warning restore EF1002
    }

    // Failure first
    [Fact]
    [Trait("Rule", "SH152")]
    public async Task ReadAsync_PageEndsBeforeAnUnreadTombstoneWhoseNeighboursWereReattached_NeverSkipsThatTombstone()
    {
        // Arrange: M1 and M2 were detached (tombstones 10, 11) and reattached (records 20, 21); Z was deleted at 12.
        var options = new DbContextOptionsBuilder<EmptyContext>().UseNpgsql(await _postgres.CreateDatabaseAsync()).Options;
        await using var db = new EmptyContext(options);
        var reader = new ChangeReader(new ChangeCursorCodec(TimeProvider.System), TimeProvider.System);
        var records = Rows(db, (M1, 20), (M2, 21));
        var tombstones = Rows(db, (M1, 10), (M2, 11), (Z, 12));

        // Act: read page by page, one entry each, until the feed says nothing is left
        var deleted = new List<Guid>();
        var changed = new List<Guid>();
        string? cursor = null;
        for (var page = 0; page < 10; page++)
        {
            var read = new ChangeFeedRead<Guid>(Feed, OrganizationId, null, new ChangesQuery { Cursor = cursor, PageSize = 1 },
                records, tombstones,
                (ids, _) => Task.FromResult<IReadOnlyDictionary<Guid, Guid>>(ids.ToDictionary(id => id)));
            var response = await reader.ReadAsync(db, read, CancellationToken.None);
            deleted.AddRange(response.Deleted);
            changed.AddRange(response.Changed);
            cursor = response.Cursor;
            if (!response.HasMore)
                break;
        }

        // Assert
        deleted.ShouldContain(Z, "a deleted record that was never handed out is not skipped because later records reuse other IDs");
        changed.ShouldBe(new[] { M1, M2 }, ignoreOrder: true, customMessage: "the reattached menus still come back as changes");
    }
}
