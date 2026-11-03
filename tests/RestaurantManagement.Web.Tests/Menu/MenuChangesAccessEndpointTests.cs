using System.Net;
using RestaurantManagement.Shared.Services.Changes;
using Shouldly;
using static RestaurantManagement.Web.Tests.MenuChangesWorld;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Who may read the menu "changes since" feeds (menus, menu-location links, menu categories) and how a bad cursor or a missing
/// location header is refused, on a real PostgreSQL database. The access is the one of the normal per-location read:
/// Menus Read at the location named by X-Location-Id. Cantina da Nonna has Batel and Mercado, Trattoria do Sol has Centro.
/// </summary>
public class MenuChangesAccessEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _postgres;

    public MenuChangesAccessEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    [Theory]
    [InlineData(MenusRoute)]
    [InlineData(LinksRoute)]
    [InlineData(CategoriesRoute)]
    [Trait("Rule", "M77")]
    [Trait("Rule", "SH149")]
    public async Task GetChanges_NoLocationHeader_Returns400LocationRequired(string route)
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.GetChangesAsync(route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a location feed needs the location the device syncs");
        (await response.ReadCodeAsync()).ShouldBe("location.required", "the refusal says the location header is missing");
    }

    [Theory]
    [InlineData(MenusRoute)]
    [InlineData(LinksRoute)]
    [InlineData(CategoriesRoute)]
    [Trait("Rule", "M78")]
    public async Task GetChanges_MemberWithoutMenusReadAtTheLocation_Returns403AccessDenied(string route)
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        var client = factory.ClientFor(FredId, CantinaId, BatelId.ToString());

        // Act
        var response = await client.GetChangesAsync(route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "Fred reads only Locations at Batel, so he may not read its menus");
        (await response.ReadCodeAsync()).ShouldBe("access.denied", "the refusal is the normal access denied, never an answer with tombstones");
    }

    [Theory]
    [InlineData(MenusRoute, AnaText)]
    [InlineData(LinksRoute, AnaText)]
    [InlineData(CategoriesRoute, AnaText)]
    [InlineData(MenusRoute, BiaText)]
    [InlineData(LinksRoute, BiaText)]
    [InlineData(CategoriesRoute, BiaText)]
    [Trait("Rule", "M79")]
    public async Task GetChanges_OwnerWithNoStoredLevelOrMemberWithMenusRead_Returns200(string route, string userText)
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        var client = factory.ClientFor(Guid.Parse(userText), CantinaId, BatelId.ToString());

        // Act
        var response = await client.GetChangesAsync(route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "the Owner is unrestricted and Read is enough, Manage is not needed");
    }

    [Theory]
    [InlineData(MenusRoute, ChangeFeeds.Menus)]
    [InlineData(LinksRoute, ChangeFeeds.MenuLocationLinks)]
    [InlineData(CategoriesRoute, ChangeFeeds.MenuCategories)]
    [Trait("Rule", "M80")]
    [Trait("Rule", "SH150")]
    public async Task GetChanges_CursorOlderThanFourteenDays_Returns410CursorExpired(string route, string feed)
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        var client = factory.AnaAt(BatelId);
        var tooOld = new ChangeCursorCodec(TimeProvider.System).Encode(
            new ChangeCursor(feed, CantinaId, BatelId, DateTimeOffset.UtcNow.AddDays(-14).AddMinutes(-1), 0, 0, null, null));

        // Act
        var response = await client.GetChangesAsync(route, tooOld);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Gone, "tombstones older than 14 days are gone, so the device must reload in full");
        (await response.ReadCodeAsync()).ShouldBe("changes.cursor_expired", "the refusal says the cursor expired");
    }

    [Theory]
    [InlineData(MenusRoute)]
    [InlineData(LinksRoute)]
    [InlineData(CategoriesRoute)]
    [Trait("Rule", "M81")]
    public async Task GetChanges_CursorOfAnotherOrganization_Returns400CursorInvalid(string route)
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        var anaCursor = (await factory.AnaAt(BatelId).ReadChangesAsync<MenuRow>(route)).Cursor;
        var joao = factory.ClientFor(JoaoId, TrattoriaId, CentroId.ToString());

        // Act
        var response = await joao.GetChangesAsync(route, anaCursor);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a cursor of Cantina da Nonna means nothing to Trattoria do Sol");
        (await response.ReadCodeAsync()).ShouldBe("changes.cursor_invalid", "a cursor bound to another organization is invalid");
    }
}
