using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Menu.Data;
using RestaurantManagement.Shared.Auth;
using Shouldly;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Menu edit and delete need the version the caller read (lost-update check) on a real PostgreSQL
/// database. Cantina da Nonna has the menu "Jantar" (category "Massas") linked to Batel;
/// Trattoria do Sol has the menu "Pranzo". Ana owns Cantina, Dora reads Menus at Batel.
/// </summary>
public class MenuVersionEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid DoraId = Guid.Parse("44444444-dddd-4ddd-8ddd-00000000000d");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid TrattoriaId = Guid.Parse("b2b2b2b2-0000-4000-8000-000000000002");
    private static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");
    private static readonly Guid TrattoriaCentroId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000003");
    private static readonly Guid DinnerMenuId = Guid.Parse("55555555-0000-4000-8000-000000000001");
    private static readonly Guid TrattoriaLunchMenuId = Guid.Parse("55555555-0000-4000-8000-000000000003");
    private static readonly Guid PastaCategoryId = Guid.Parse("66666666-0000-4000-8000-000000000001");

    private static readonly string DinnerUrl = $"/api/v1/menus/{DinnerMenuId}";

    private readonly PostgresDatabaseFixture _postgres;

    public MenuVersionEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private async Task<TenantApiFactory> CreateFactoryAsync()
    {
        var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedOrganizationAsync(TrattoriaId, "Trattoria do Sol", Currency.BRL);
        await factory.SeedLocationAsync(CantinaId, BatelId, "Unidade Batel", "BATEL", "America/Sao_Paulo");
        await factory.SeedLocationAsync(TrattoriaId, TrattoriaCentroId, "Unidade Centro", "CENTRO", "America/Sao_Paulo");
        await factory.SeedMenuAsync(CantinaId, DinnerMenuId, "Jantar");
        await factory.SeedMenuAsync(TrattoriaId, TrattoriaLunchMenuId, "Pranzo");
        await factory.SeedCategoryAsync(CantinaId, DinnerMenuId, PastaCategoryId, "Massas");
        await factory.InOrganizationAsync<MenuDbContext, bool>(CantinaId, async db =>
        {
            db.LocationMenus.Add(new RestaurantManagement.Modules.Menu.Entities.LocationMenu { Id = Guid.NewGuid(), OrganizationId = CantinaId, MenuId = DinnerMenuId, LocationId = BatelId, IsActive = true });
            await db.SaveChangesAsync();
            return true;
        });
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId), TenantApiFactory.OwnerOf(TrattoriaId));
        await factory.SeedUserAsync(DoraId, "dora@cantinadanonna.com.br", "Dora", TenantApiFactory.MemberOf(CantinaId));
        await factory.GrantAsync(DoraId, CantinaId, BatelId, (AccessModule.Locations, AccessLevel.Read), (AccessModule.Menus, AccessLevel.Read));
        return factory;
    }

    private static object MenuBody(string name, uint? version) => version is null
        ? new { name, description = "Menu da noite", isActive = true }
        : new { name, description = "Menu da noite", isActive = true, version };

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [Trait("Rule", "M59")]
    public async Task MenuChange_VersionMissing_ReturnsBadRequestOnTheVersionFieldAndChangesNothing(string method)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var request = new HttpRequestMessage(new HttpMethod(method), DinnerUrl) { Content = JsonContent.Create(MenuBody("Jantar de inverno", null)) };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "an edit or delete that does not say which version it read is refused");
        (await response.ReadErrorFieldsAsync()).ShouldContain("version", "the answer names the version field");
        var stored = await factory.InOrganizationAsync<MenuDbContext, string>(CantinaId, async db => (await db.Menus.SingleAsync(m => m.Id == DinnerMenuId)).Name);
        stored.ShouldBe("Jantar", "a refused change leaves the menu as it was");
    }

    [Fact]
    [Trait("Rule", "M60")]
    public async Task EditMenu_VersionOfAnEarlierRead_ReturnsConflictWithTheCurrentMenuAndKeepsTheFirstChange()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var staleVersion = await client.ReadVersionAsync("/api/v1/menus", DinnerMenuId);
        (await client.PutAsJsonAsync(DinnerUrl, MenuBody("Jantar de inverno", staleVersion))).EnsureSuccessStatusCode();
        var currentVersion = await client.ReadVersionAsync("/api/v1/menus", DinnerMenuId);

        // Act
        var response = await client.PutAsJsonAsync(DinnerUrl, MenuBody("Jantar de verão", staleVersion));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, "a second editor who read the old version must not overwrite the first change");
        var body = await response.ReadBodyAsync();
        body.GetProperty("code").GetString().ShouldBe("version.conflict", "the refusal has its own stable code");
        body.GetProperty("current").GetProperty("name").GetString().ShouldBe("Jantar de inverno", "the answer carries the menu as stored now");
        body.GetProperty("current").GetProperty("version").GetUInt32().ShouldBe(currentVersion, "the carried menu has its current version");
        (await client.ReadVersionAsync("/api/v1/menus", DinnerMenuId)).ShouldBe(currentVersion, "a refused edit changes nothing");
    }

    [Fact]
    [Trait("Rule", "M61")]
    public async Task DeleteMenu_VersionOfAnEarlierRead_ReturnsConflictAndKeepsTheMenuAndItsCategories()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var staleVersion = await client.ReadVersionAsync("/api/v1/menus", DinnerMenuId);
        (await client.PutAsJsonAsync(DinnerUrl, MenuBody("Jantar de inverno", staleVersion))).EnsureSuccessStatusCode();

        // Act
        var response = await client.DeleteAtVersionAsync(DinnerUrl, staleVersion);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, "deleting a menu changed since it was read is refused");
        (await response.ReadBodyAsync()).GetProperty("current").GetProperty("name").GetString().ShouldBe("Jantar de inverno", "the answer carries the menu as stored now");
        var stored = await factory.InOrganizationAsync<MenuDbContext, (int Menus, int Categories)>(CantinaId,
            async db => (await db.Menus.CountAsync(), await db.MenuCategories.CountAsync()));
        stored.ShouldBe((1, 1), "the menu and its category are still stored");
    }

    [Fact]
    [Trait("Rule", "M62")]
    public async Task EditMenu_CurrentVersion_SavesAndReturnsANewVersion()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var version = await client.ReadVersionAsync("/api/v1/menus", DinnerMenuId);

        // Act
        var response = await client.PutAsJsonAsync(DinnerUrl, MenuBody("Jantar de inverno", version));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "the current version may edit the menu");
        var newVersion = (await response.ReadBodyAsync()).GetProperty("version").GetUInt32();
        newVersion.ShouldNotBe(version, "an edit gives the menu a new version");
        (await client.ReadVersionAsync("/api/v1/menus", DinnerMenuId)).ShouldBe(newVersion, "the version returned is the stored one");
    }

    [Fact]
    [Trait("Rule", "M63")]
    public async Task DeleteMenu_CurrentVersion_DeletesTheMenu()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var version = await client.ReadVersionAsync("/api/v1/menus", DinnerMenuId);

        // Act
        var response = await client.DeleteAtVersionAsync(DinnerUrl, version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "the current version may delete the menu");
        var count = await factory.InOrganizationAsync<MenuDbContext, int>(CantinaId, db => db.Menus.CountAsync());
        count.ShouldBe(0, "the menu is gone");
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [Trait("Rule", "M64")]
    public async Task MenuChange_MenuOfAnotherOrganizationWithItsRealVersion_ReturnsNotFoundAndRevealsNothing(string method)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var realVersion = await factory.ClientFor(AnaId, TrattoriaId).ReadVersionAsync("/api/v1/menus", TrattoriaLunchMenuId);
        var request = new HttpRequestMessage(new HttpMethod(method), $"/api/v1/menus/{TrattoriaLunchMenuId}?version={realVersion}")
        {
            Content = JsonContent.Create(MenuBody("Cardápio Invadido", realVersion))
        };

        // Act
        var response = await factory.ClientFor(AnaId, CantinaId).SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "another organization's menu is not found, even with its real version, never a version conflict");
        var body = await response.ReadBodyAsync();
        body.GetProperty("code").GetString().ShouldBe("menu.not_found", "the answer is the ordinary not-found");
        body.TryGetProperty("current", out _).ShouldBeFalse("nothing about the other organization's menu is in the body");
        body.GetRawText().ShouldNotContain("Pranzo", customMessage: "the other organization's menu name never reaches the caller");
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [Trait("Rule", "M65")]
    public async Task MenuChange_MemberWithMenusReadOnlySendingAStaleVersion_ReturnsForbiddenNotConflict(string method)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var ana = factory.ClientFor(AnaId, CantinaId);
        var staleVersion = await ana.ReadVersionAsync("/api/v1/menus", DinnerMenuId);
        (await ana.PutAsJsonAsync(DinnerUrl, MenuBody("Jantar de inverno", staleVersion))).EnsureSuccessStatusCode();
        var request = new HttpRequestMessage(new HttpMethod(method), $"{DinnerUrl}?version={staleVersion}")
        {
            Content = JsonContent.Create(MenuBody("Jantar de verão", staleVersion))
        };

        // Act
        var response = await factory.ClientFor(DoraId, CantinaId).SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "the access check comes before the version check");
        (await response.ReadBodyAsync()).TryGetProperty("current", out _).ShouldBeFalse("a caller who may not change the menu is never handed its current record");
    }

    [Theory]
    [InlineData("GET", "/api/v1/menus", null)]
    [InlineData("GET", "/api/v1/menus/location/44444444-dddd-4ddd-8ddd-000000000001", null)]
    [InlineData("POST", "/api/v1/menus/create", """{"id":"01933b2e-7c10-7a55-9d3e-5b1f0c2a4eb1","name":"Almoço executivo","description":"Prato feito","isActive":true}""")]
    [Trait("Rule", "M66")]
    public async Task MenuResponses_ListByLocationAndCreate_CarryTheVersion(string method, string url, string? json)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (json is not null)
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.ReadBodyAsync();
        var menu = body.ValueKind == System.Text.Json.JsonValueKind.Array ? body[0] : body;
        menu.GetProperty("version").GetUInt32().ShouldBeGreaterThan(0u, "every menu answer carries the version the next edit must send back");
    }
}
