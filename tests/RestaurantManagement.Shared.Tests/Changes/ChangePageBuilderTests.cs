using RestaurantManagement.Shared.Data.Changes;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Changes;

/// <summary>Merging changed records and tombstones into pages ordered by (stamp, ID).</summary>
public class ChangePageBuilderTests
{
    private static Guid Id(int n) => Guid.Parse($"{n:x8}-0000-4000-8000-000000000000");

    [Fact]
    [Trait("Rule", "SH144")]
    public void Build_RecordsAndTombstones_AreMergedInStampOrderAndCutAtThePageSize()
    {
        // Arrange
        var records = new[] { new ChangeEntry(Id(1), 30, false), new ChangeEntry(Id(2), 10, false) };
        var tombstones = new[] { new ChangeEntry(Id(3), 20, true), new ChangeEntry(Id(4), 40, true) };

        // Act
        var page = ChangePageBuilder.Build(records, tombstones, after: null, pageSize: 3);

        // Assert
        page.Entries.Select(e => e.Id).ShouldBe(new[] { Id(2), Id(3), Id(1) }, "records and tombstones come together, oldest stamp first");
        page.HasMore.ShouldBeTrue("one entry is left past the page size, so the caller must continue");
    }

    [Theory]
    [InlineData(10, 20, true)]
    [InlineData(30, 20, false)]
    [Trait("Rule", "SH145")]
    public void Build_SameIdChangedAndDeleted_OnlyTheLaterEntryIsReturned(long recordStamp, long tombstoneStamp, bool expectedDeleted)
    {
        // Arrange
        var records = new[] { new ChangeEntry(Id(7), recordStamp, false) };
        var tombstones = new[] { new ChangeEntry(Id(7), tombstoneStamp, true) };

        // Act
        var page = ChangePageBuilder.Build(records, tombstones, after: null, pageSize: 10);

        // Assert
        page.Entries.Single().IsDeleted.ShouldBe(expectedDeleted, "a record changed then deleted comes back only as deleted, and one deleted then recreated only as changed");
        page.HasMore.ShouldBeFalse("nothing is left");
    }

    [Fact]
    [Trait("Rule", "SH146")]
    public void Build_ContinuingFromAPosition_ReturnsOnlyEntriesAfterIt()
    {
        // Arrange
        var records = new[]
        {
            new ChangeEntry(Id(1), 10, false), new ChangeEntry(Id(2), 20, false),
            new ChangeEntry(Id(3), 20, false), new ChangeEntry(Id(4), 30, false)
        };

        // Act
        var page = ChangePageBuilder.Build(records, Array.Empty<ChangeEntry>(), new ChangePosition(20, Id(2)), pageSize: 10);

        // Assert
        page.Entries.Select(e => e.Id).ShouldBe(new[] { Id(3), Id(4) }, "a page continues strictly after the last (stamp, ID) handed out, also inside a run of equal stamps");
    }
}
