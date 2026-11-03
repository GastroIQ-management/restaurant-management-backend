using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Menu.Data;
using Shouldly;
using static RestaurantManagement.Web.Tests.MenuChangesWorld;

namespace RestaurantManagement.Web.Tests;

/// <summary>The tombstone the Menu database leaves when a menu with no location links is deleted, read straight from PostgreSQL.</summary>
public class MenuUnlinkedDeleteTrackingTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _postgres;

    public MenuUnlinkedDeleteTrackingTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private sealed record TombstoneRow(Guid EntityId, Guid? LocationId, Guid OrganizationId);

    [Fact]
    [Trait("Rule", "MD5")]
    public async Task DeleteMenu_WithNoLocationLinks_LeavesOneTombstoneWithNoLocationCarryingTheMenusOrganization()
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        await factory.SeedMenuAsync(TrattoriaId, MenuId(2), "Pizzas");

        // Act
        await factory.DeleteMenuAsync(TrattoriaId, MenuId(2));
        var tombstones = await factory.InOrganizationAsync<MenuDbContext, List<TombstoneRow>>(TrattoriaId, db =>
            db.Database.SqlQueryRaw<TombstoneRow>(
                "SELECT entity_id, location_id, organization_id FROM menu_tombstones").ToListAsync());

        // Assert
        tombstones.ShouldHaveSingleItem("an unlinked menu leaves exactly one tombstone");
        tombstones[0].EntityId.ShouldBe(MenuId(2));
        tombstones[0].LocationId.ShouldBeNull("the menu was at no location");
        tombstones[0].OrganizationId.ShouldBe(TrattoriaId, "the tombstone carries the organization of the deleted menu");
    }
}
