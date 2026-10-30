using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Shared.Data.Versioning;
using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Data;

/// <summary>The version check on a module-like database (SQLite): a stale version is refused, a current one saves, a lost race ends as the same conflict.</summary>
public class VersionCheckTests
{
    private static readonly Guid RisottoId = Guid.Parse("0b3f6c1e-5d2a-4e8b-9a47-1c6d8e2f7a10");

    private static async Task SeedRisottoAsync(VersionedTestDatabase database)
    {
        await using var context = database.CreateContext();
        context.Dishes.Add(new TestVersionedDish { Id = RisottoId, Name = "Risotto de cogumelos", PriceInCents = 5800 });
        await context.SaveChangesAsync();
    }

    [Fact]
    [Trait("Rule", "SH73")]
    public async Task Require_SentVersionDiffersFromStored_ThrowsVersionConflictWithCurrentRecord()
    {
        // Arrange
        using var database = new VersionedTestDatabase();
        await SeedRisottoAsync(database);
        await using var context = database.CreateContext();
        var dish = await context.Dishes.SingleAsync();
        var current = new { Name = dish.Name, Version = dish.Version };

        // Act
        var act = () => VersionCheck.Require(context, dish, dish.Version + 1, () => current);

        // Assert
        var conflict = Should.Throw<VersionConflictException>(act, "a version different from the stored one is a stale read and must be refused");
        conflict.Current.ShouldBeSameAs(current, "the conflict carries the record as stored now, so the caller can show it");
    }

    [Fact]
    [Trait("Rule", "SH74")]
    public async Task Require_SentVersionEqualsStored_PassesAndTheChangeIsSaved()
    {
        // Arrange
        using var database = new VersionedTestDatabase();
        await SeedRisottoAsync(database);
        await using var context = database.CreateContext();
        var dish = await context.Dishes.SingleAsync();

        // Act
        VersionCheck.Require(context, dish, dish.Version, () => new object());
        dish.PriceInCents = 6200;
        await VersionCheck.SaveAsync(context, () => Task.FromResult<object>(new object()));

        // Assert
        await using var reader = database.CreateContext();
        (await reader.Dishes.SingleAsync()).PriceInCents.ShouldBe(6200, "the current version may change the record");
    }

    [Fact]
    [Trait("Rule", "SH75")]
    public async Task SaveAsync_RowChangedAfterItWasRead_ThrowsVersionConflictWithTheFreshRecord()
    {
        // Arrange
        using var database = new VersionedTestDatabase();
        await SeedRisottoAsync(database);
        await using var context = database.CreateContext();
        var dish = await context.Dishes.SingleAsync();
        VersionCheck.Require(context, dish, dish.Version, () => new object());
        await using (var other = database.CreateContext())
        {
            await other.Database.ExecuteSqlRawAsync("UPDATE \"Dishes\" SET \"Version\" = 7");
        }

        dish.PriceInCents = 6200;
        var fresh = new { PriceInCents = 5800, Version = 7u };

        // Act
        var act = () => VersionCheck.SaveAsync(context, () => Task.FromResult<object>(fresh));

        // Assert
        var conflict = await Should.ThrowAsync<VersionConflictException>(act, "a save that loses a race ends as the same version conflict");
        conflict.Current.ShouldBeSameAs(fresh, "the conflict carries the record as stored after the race");
        await using var reader = database.CreateContext();
        (await reader.Dishes.SingleAsync()).PriceInCents.ShouldBe(5800, "the losing change is never applied");
    }
}
