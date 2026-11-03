using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RestaurantManagement.Modules.Menu.Data;
using RestaurantManagement.Shared.Auth;
using RestaurantManagement.Shared.Services.Changes;
using Shouldly;
using static RestaurantManagement.Web.Tests.MenuChangesWorld;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The menus and menu-location links "changes since" feeds on a real PostgreSQL database. Cantina da Nonna (Batel, Mercado)
/// and Trattoria do Sol (Centro) each have menus; Ana owns Cantina, João owns Trattoria, Bia reads Menus at Batel.
/// Writes go through the database context, as the app's own writes do.
/// </summary>
public class MenuChangesEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _postgres;

    public MenuChangesEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    [Fact]
    [Trait("Rule", "M82")]
    public async Task GetMenuChanges_FirstSync_ReturnsOnlyMenusLinkedToTheHeaderLocationAndACursor()
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        await factory.SeedMenuAsync(CantinaId, MenuId(1), "Jantar");
        await factory.SeedMenuAsync(CantinaId, MenuId(2), "Almoço");
        await factory.LinkAsync(CantinaId, MenuId(1), BatelId);
        await factory.LinkAsync(CantinaId, MenuId(2), MercadoId);

        // Act
        var page = await factory.AnaAt(BatelId).ReadChangesAsync<MenuRow>(MenusRoute);

        // Assert
        page.Changed.Select(m => m.Id).ShouldBe(new[] { MenuId(1) }, ignoreOrder: false, customMessage: "the first sync returns the menus linked to Batel and never the menu linked only to Mercado");
        page.Changed.Single().Name.ShouldBe("Jantar");
        page.Deleted.ShouldBeEmpty("nothing was deleted");
        page.HasMore.ShouldBeFalse("one menu fits in a page");
        page.Cursor.ShouldNotBeNullOrWhiteSpace("the device needs a cursor for the next call");
    }

    [Fact]
    [Trait("Rule", "M83")]
    public async Task GetMenuChanges_OneOfThreeMenusEdited_ReturnsExactlyItAndThenNothing()
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        foreach (var n in new[] { 1, 2, 3 })
        {
            await factory.SeedMenuAsync(CantinaId, MenuId(n), $"Menu {n}");
            await factory.LinkAsync(CantinaId, MenuId(n), BatelId);
        }
        var client = factory.AnaAt(BatelId);
        var first = await client.ReadChangesAsync<MenuRow>(MenusRoute);
        await factory.RenameMenuAsync(CantinaId, MenuId(2), "Jantar de inverno");

        // Act
        var second = await client.ReadChangesAsync<MenuRow>(MenusRoute, first.Cursor);
        var third = await client.ReadChangesAsync<MenuRow>(MenusRoute, second.Cursor);

        // Assert
        second.Changed.Select(m => m.Id).ShouldBe(new[] { MenuId(2) }, ignoreOrder: false, customMessage: "only the edited menu comes back");
        second.Changed.Single().Name.ShouldBe("Jantar de inverno");
        third.Changed.ShouldBeEmpty("a change is returned once: with the new cursor and no writes nothing comes back");
        third.Deleted.ShouldBeEmpty("no deletes either");
        third.HasMore.ShouldBeFalse();
    }

    [Fact]
    [Trait("Rule", "M84")]
    public async Task GetMenuChanges_FiveMenusWithPageSizeTwo_FollowingTheCursorsReturnsEachOnce()
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        foreach (var n in new[] { 1, 2, 3, 4, 5 })
        {
            await factory.SeedMenuAsync(CantinaId, MenuId(n), $"Menu {n}");
            await factory.LinkAsync(CantinaId, MenuId(n), BatelId);
        }
        var client = factory.AnaAt(BatelId);

        // Act
        var pageOne = await client.ReadChangesAsync<MenuRow>(MenusRoute, pageSize: 2);
        var pageTwo = await client.ReadChangesAsync<MenuRow>(MenusRoute, pageOne.Cursor, 2);
        var pageThree = await client.ReadChangesAsync<MenuRow>(MenusRoute, pageTwo.Cursor, 2);

        // Assert
        pageOne.Changed.Count.ShouldBe(2);
        pageOne.HasMore.ShouldBeTrue("three menus remain after the first page");
        pageTwo.Changed.Count.ShouldBe(2);
        pageTwo.HasMore.ShouldBeTrue("one menu remains after the second page");
        pageThree.Changed.Count.ShouldBe(1);
        pageThree.HasMore.ShouldBeFalse("the last page says nothing more remains");
        pageOne.Changed.Concat(pageTwo.Changed).Concat(pageThree.Changed).Select(m => m.Id)
            .OrderBy(id => id).ShouldBe(Enumerable.Range(1, 5).Select(MenuId).OrderBy(id => id), ignoreOrder: false, customMessage: "following the cursors returns every menu exactly once");
    }

    [Fact]
    [Trait("Rule", "M85")]
    [Trait("Rule", "SH151")]
    public async Task GetMenuChanges_TwoOverlappingWritesCommitInReverseOrder_NeitherIsMissed()
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        foreach (var n in new[] { 1, 2 })
        {
            await factory.SeedMenuAsync(CantinaId, MenuId(n), $"Menu {n}");
            await factory.LinkAsync(CantinaId, MenuId(n), BatelId);
        }
        var client = factory.AnaAt(BatelId);
        var start = await client.ReadChangesAsync<MenuRow>(MenusRoute);
        var connectionString = factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
        await using var slowConnection = new NpgsqlConnection(connectionString);
        await slowConnection.OpenAsync();
        await using var slowTransaction = await slowConnection.BeginTransactionAsync();
        await using (var slowWrite = new NpgsqlCommand("UPDATE menus SET name = 'Jantar da casa' WHERE id = @id", slowConnection, slowTransaction))
        {
            slowWrite.Parameters.AddWithValue("id", MenuId(1));
            await slowWrite.ExecuteNonQueryAsync();
        }
        await using (var fastConnection = new NpgsqlConnection(connectionString))
        {
            await fastConnection.OpenAsync();
            await using var fastWrite = new NpgsqlCommand("UPDATE menus SET name = 'Almoço da casa' WHERE id = @id", fastConnection);
            fastWrite.Parameters.AddWithValue("id", MenuId(2));
            await fastWrite.ExecuteNonQueryAsync();
        }

        // Act
        var whileSlowIsOpen = await client.ReadChangesAsync<MenuRow>(MenusRoute, start.Cursor);
        await slowTransaction.CommitAsync();
        var afterSlowCommits = await client.ReadChangesAsync<MenuRow>(MenusRoute, whileSlowIsOpen.Cursor);

        // Assert
        whileSlowIsOpen.Changed.Select(m => m.Id).ShouldContain(MenuId(2), "the write that committed first is returned");
        afterSlowCommits.Changed.Select(m => m.Id).ShouldContain(MenuId(1), "the write that committed last is not missed even though a later write was already read");
    }

    [Fact]
    [Trait("Rule", "M86")]
    public async Task GetMenuChanges_AnotherOrganizationsMenusAndDeletes_NeverAppearForTheCaller()
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        await factory.SeedMenuAsync(CantinaId, MenuId(1), "Jantar");
        await factory.SeedMenuAsync(TrattoriaId, MenuId(2), "Pizzas");
        await factory.SeedMenuAsync(TrattoriaId, MenuId(3), "Sobremesas");
        await factory.LinkAsync(CantinaId, MenuId(1), BatelId);
        await factory.LinkAsync(TrattoriaId, MenuId(2), CentroId);
        await factory.LinkAsync(TrattoriaId, MenuId(3), CentroId);
        var client = factory.AnaAt(BatelId);
        var first = await client.ReadChangesAsync<MenuRow>(MenusRoute);
        await factory.RenameMenuAsync(TrattoriaId, MenuId(2), "Pizzas artesanais");
        await factory.DeleteMenuAsync(TrattoriaId, MenuId(3));

        // Act
        var second = await client.ReadChangesAsync<MenuRow>(MenusRoute, first.Cursor);

        // Assert
        first.Changed.Select(m => m.Id).ShouldBe(new[] { MenuId(1) }, ignoreOrder: false, customMessage: "Ana receives only Cantina da Nonna's menu");
        second.Changed.ShouldBeEmpty("Trattoria do Sol's edit is never returned to Cantina da Nonna");
        second.Deleted.ShouldBeEmpty("Trattoria do Sol's delete is never reported to Cantina da Nonna");
    }

    [Fact]
    [Trait("Rule", "M87")]
    public async Task GetMenuChanges_MenuDetachedFromOneOfTwoLocations_IsATombstoneOnlyAtThatLocation()
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        await factory.SeedMenuAsync(CantinaId, MenuId(1), "Jantar");
        await factory.LinkAsync(CantinaId, MenuId(1), BatelId);
        await factory.LinkAsync(CantinaId, MenuId(1), MercadoId);
        var batel = factory.AnaAt(BatelId);
        var mercado = factory.AnaAt(MercadoId);
        var batelStart = await batel.ReadChangesAsync<MenuRow>(MenusRoute);
        var mercadoStart = await mercado.ReadChangesAsync<MenuRow>(MenusRoute);
        await factory.DetachAsync(CantinaId, MenuId(1), BatelId);

        // Act
        var atBatel = await batel.ReadChangesAsync<MenuRow>(MenusRoute, batelStart.Cursor);
        var atMercado = await mercado.ReadChangesAsync<MenuRow>(MenusRoute, mercadoStart.Cursor);

        // Assert
        atBatel.Deleted.ShouldBe(new[] { MenuId(1) }, ignoreOrder: false, customMessage: "the menu left Batel, so the device drops it");
        atMercado.Deleted.ShouldBeEmpty("the menu stayed at Mercado, and tombstones of another location never appear");
        atMercado.Changed.ShouldBeEmpty("nothing changed at Mercado");
    }

    [Fact]
    [Trait("Rule", "M88")]
    public async Task GetMenuChanges_MemberLosesReadAtTheLocation_NextSyncIsRefusedWithoutRecordsOrTombstones()
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        await factory.SeedMenuAsync(CantinaId, MenuId(1), "Jantar");
        await factory.LinkAsync(CantinaId, MenuId(1), BatelId);
        var bia = factory.ClientFor(BiaId, CantinaId, BatelId.ToString());
        var first = await bia.ReadChangesAsync<MenuRow>(MenusRoute);
        await factory.SetLevelAsync(BiaId, CantinaId, BatelId, AccessModule.Menus, AccessLevel.None);

        // Act
        var response = await bia.GetChangesAsync(MenusRoute, first.Cursor);

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Forbidden, "without Read at Batel the member is refused, so the device drops that location's data");
        (await response.ReadCodeAsync()).ShouldBe("access.denied", "no records or tombstones are sent to a member who lost access");
    }

    [Fact]
    [Trait("Rule", "M89")]
    public async Task GetMenuChanges_MenuDeletedEightDaysAgo_IsStillReturnedAsATombstone()
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        await factory.SeedMenuAsync(CantinaId, MenuId(1), "Jantar");
        await factory.LinkAsync(CantinaId, MenuId(1), BatelId);
        var client = factory.AnaAt(BatelId);
        var first = await client.ReadChangesAsync<MenuRow>(MenusRoute);
        await factory.DeleteMenuAsync(CantinaId, MenuId(1));
        await factory.InOrganizationAsync<MenuDbContext, int>(CantinaId, db =>
            db.Database.ExecuteSqlRawAsync("UPDATE menu_tombstones SET deleted_at = now() - interval '8 days'"));
        var codec = new ChangeCursorCodec(TimeProvider.System);
        var eightDaysOld = codec.Encode(codec.Decode(first.Cursor, ChangeFeeds.Menus, CantinaId, BatelId) with { IssuedAt = DateTimeOffset.UtcNow.AddDays(-8) });

        // Act
        var page = await client.ReadChangesAsync<MenuRow>(MenusRoute, eightDaysOld);

        // Assert
        page.Deleted.ShouldBe(new[] { MenuId(1) }, ignoreOrder: false, customMessage: "a tombstone is kept 14 days, longer than the 7 day offline limit");
    }

    [Fact]
    [Trait("Rule", "M90")]
    public async Task GetMenuChanges_UnchangedMenuAttachedToTheLocation_IsReturnedAsChanged()
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        await factory.SeedMenuAsync(CantinaId, MenuId(1), "Jantar");
        await factory.SeedMenuAsync(CantinaId, MenuId(2), "Almoço");
        await factory.LinkAsync(CantinaId, MenuId(1), BatelId);
        var client = factory.AnaAt(BatelId);
        var first = await client.ReadChangesAsync<MenuRow>(MenusRoute);
        await factory.LinkAsync(CantinaId, MenuId(2), BatelId);

        // Act
        var second = await client.ReadChangesAsync<MenuRow>(MenusRoute, first.Cursor);

        // Assert
        first.Changed.Select(m => m.Id).ShouldBe(new[] { MenuId(1) }, ignoreOrder: false, customMessage: "the menu not yet attached is not part of the first sync");
        second.Changed.Select(m => m.Id).ShouldBe(new[] { MenuId(2) }, ignoreOrder: false, customMessage: "attaching a menu that itself did not change still makes it new at that location");
    }

    [Fact]
    [Trait("Rule", "M91")]
    public async Task GetChanges_MenuDeleted_MenuAndItsLinkComeBackAsTombstonesInTheirFeeds()
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        var linkId = Guid.Parse("77777777-0000-4000-8000-000000000001");
        await factory.SeedMenuAsync(CantinaId, MenuId(1), "Jantar");
        await factory.LinkAsync(CantinaId, MenuId(1), BatelId, linkId);
        var client = factory.AnaAt(BatelId);
        var menusStart = await client.ReadChangesAsync<MenuRow>(MenusRoute);
        var linksStart = await client.ReadChangesAsync<LinkRow>(LinksRoute);
        await factory.DeleteMenuAsync(CantinaId, MenuId(1));

        // Act
        var menus = await client.ReadChangesAsync<MenuRow>(MenusRoute, menusStart.Cursor);
        var links = await client.ReadChangesAsync<LinkRow>(LinksRoute, linksStart.Cursor);

        // Assert
        menus.Deleted.ShouldBe(new[] { MenuId(1) }, ignoreOrder: false, customMessage: "the deleted menu is reported so devices drop it");
        links.Deleted.ShouldBe(new[] { linkId }, ignoreOrder: false, customMessage: "the link removed by the database cascade is reported too");
    }

    [Fact]
    [Trait("Rule", "M92")]
    public async Task GetLinkChanges_OneLinkEdited_ReturnsExactlyThatLinkWithItsMenuAndLocation()
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        var linkId = Guid.Parse("77777777-0000-4000-8000-000000000002");
        foreach (var n in new[] { 1, 2, 3 })
            await factory.SeedMenuAsync(CantinaId, MenuId(n), $"Menu {n}");
        await factory.LinkAsync(CantinaId, MenuId(1), BatelId);
        await factory.LinkAsync(CantinaId, MenuId(2), BatelId, linkId);
        await factory.LinkAsync(CantinaId, MenuId(3), BatelId);
        var client = factory.AnaAt(BatelId);
        var first = await client.ReadChangesAsync<LinkRow>(LinksRoute);
        await factory.InOrganizationAsync<MenuDbContext, bool>(CantinaId, async db =>
        {
            (await db.LocationMenus.SingleAsync(l => l.Id == linkId)).IsActive = false;
            await db.SaveChangesAsync();
            return true;
        });

        // Act
        var second = await client.ReadChangesAsync<LinkRow>(LinksRoute, first.Cursor);

        // Assert
        first.Changed.Count.ShouldBe(3, "the first sync returns every link of Batel");
        second.Changed.ShouldBe(new[] { new LinkRow(linkId, MenuId(2), BatelId, false) }, ignoreOrder: false, customMessage: "only the edited link comes back, with its menu, location and active flag");
    }
}
