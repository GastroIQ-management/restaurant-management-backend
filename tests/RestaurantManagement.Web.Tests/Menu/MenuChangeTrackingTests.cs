using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Menu.Data;
using Shouldly;
using static RestaurantManagement.Web.Tests.MenuChangesWorld;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The change stamps and tombstones the Menu database leaves on writes, read straight from PostgreSQL
/// (database triggers do this, so it cannot be proven on SQLite). Cantina da Nonna and Trattoria do Sol each have menus.
/// </summary>
public class MenuChangeTrackingTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _postgres;

    public MenuChangeTrackingTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private sealed record TombstoneRow(Guid EntityId, Guid? LocationId, Guid OrganizationId);

    private static readonly Guid CategoryId = Guid.Parse("66666666-0000-4000-8000-000000000001");
    private static readonly Guid BatelLinkId = Guid.Parse("77777777-0000-4000-8000-000000000001");
    private static readonly Guid MercadoLinkId = Guid.Parse("77777777-0000-4000-8000-000000000002");

    private Task<TenantApiFactory> CreateWithJantarAsync() => CreateAsync(_postgres);

    private static async Task SeedJantarAsync(TenantApiFactory factory)
    {
        await factory.SeedMenuAsync(CantinaId, MenuId(1), "Jantar");
        await factory.LinkAsync(CantinaId, MenuId(1), BatelId, BatelLinkId);
        await factory.LinkAsync(CantinaId, MenuId(1), MercadoId, MercadoLinkId);
        await factory.SeedCategoryAtAsync(CantinaId, MenuId(1), CategoryId, "Massas", BatelId);
    }

#pragma warning disable EF1002 // fixed table names from this test, no input
    private static Task<long> ReadStampAsync(TenantApiFactory factory, string table)
        => factory.InOrganizationAsync<MenuDbContext, long>(CantinaId, async db =>
            await db.Database.SqlQueryRaw<long>("SELECT change_stamp AS \"Value\" FROM " + table + " LIMIT 1").SingleAsync());

    private static Task<List<TombstoneRow>> ReadTombstonesAsync(TenantApiFactory factory)
        => factory.InOrganizationAsync<MenuDbContext, List<TombstoneRow>>(CantinaId, db =>
            db.Database.SqlQueryRaw<TombstoneRow>(
                "SELECT entity_id, location_id, organization_id FROM menu_tombstones").ToListAsync());
#pragma warning restore EF1002

    [Theory]
    [InlineData("menus")]
    [InlineData("menu_categories")]
    [InlineData("location_menus")]
    [Trait("Rule", "MD1")]
    public async Task Write_AnyStampedMenuTable_InsertSetsAndEditRaisesTheChangeStamp(string table)
    {
        // Arrange
        await using var factory = await CreateWithJantarAsync();
        await SeedJantarAsync(factory);
        var afterInsert = await ReadStampAsync(factory, table);

        // Act
        await factory.InOrganizationAsync<MenuDbContext, int>(CantinaId, db =>
            db.Database.ExecuteSqlRawAsync("UPDATE " + table + " SET updated_at = updated_at"));
        var afterEdit = await ReadStampAsync(factory, table);

        // Assert
        afterInsert.ShouldBeGreaterThan(0, "a new row is stamped by the database with its transaction");
        afterEdit.ShouldBeGreaterThan(afterInsert, "an edit stamps the row again with a later transaction");
    }

    [Fact]
    [Trait("Rule", "MD2")]
    public async Task DeleteMenu_LinkedToTwoLocationsWithOneCategory_LeavesTombstonesAtEachLocationItLeft()
    {
        // Arrange
        await using var factory = await CreateWithJantarAsync();
        await SeedJantarAsync(factory);

        // Act
        await factory.DeleteMenuAsync(CantinaId, MenuId(1));
        var tombstones = await ReadTombstonesAsync(factory);

        // Assert
        tombstones.Select(t => (t.EntityId, t.LocationId)).ToList().ShouldBe(new (Guid, Guid?)[]
        {
            (MenuId(1), BatelId), (MenuId(1), MercadoId), (BatelLinkId, BatelId), (MercadoLinkId, MercadoId), (CategoryId, BatelId)
        }, ignoreOrder: true, customMessage: "the menu and its link are reported at both locations and the cascaded category at its own location");
    }

    [Fact]
    [Trait("Rule", "MD3")]
    public async Task DetachMenu_FromOneLocation_LeavesLinkAndMenuTombstonesAtThatLocationOnly()
    {
        // Arrange
        await using var factory = await CreateWithJantarAsync();
        await SeedJantarAsync(factory);

        // Act
        await factory.DetachAsync(CantinaId, MenuId(1), BatelId);
        var tombstones = await ReadTombstonesAsync(factory);

        // Assert
        tombstones.Select(t => (t.EntityId, t.LocationId)).ToList().ShouldBe(new (Guid, Guid?)[] { (BatelLinkId, BatelId), (MenuId(1), BatelId) },
            ignoreOrder: true, customMessage: "detaching reports the link and the menu at Batel and nothing at Mercado");
    }

    [Fact]
    [Trait("Rule", "MD4")]
    public async Task DeleteMenu_OfTrattoria_TombstonesCarryTrattoriasOrganizationNeverCantinas()
    {
        // Arrange
        await using var factory = await CreateWithJantarAsync();
        await factory.SeedMenuAsync(TrattoriaId, MenuId(2), "Pizzas");
        await factory.LinkAsync(TrattoriaId, MenuId(2), CentroId);

        // Act
        await factory.DeleteMenuAsync(TrattoriaId, MenuId(2));
        var tombstones = await factory.InOrganizationAsync<MenuDbContext, List<TombstoneRow>>(TrattoriaId, db =>
            db.Database.SqlQueryRaw<TombstoneRow>(
                "SELECT entity_id, location_id, organization_id FROM menu_tombstones").ToListAsync());

        // Assert
        tombstones.ShouldNotBeEmpty("the delete left tombstones");
        tombstones.ShouldAllBe(t => t.OrganizationId == TrattoriaId, "every tombstone carries the organization of the row that was deleted");
    }
}
