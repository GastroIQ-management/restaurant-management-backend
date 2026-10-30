using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Menu.Data;
using RestaurantManagement.Shared.Auth;
using Shouldly;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Menu category update and delete need the version the caller read (lost-update check) on a real
/// PostgreSQL database. Cantina da Nonna has the menu "Jantar" with the category "Massas";
/// Trattoria do Sol has the category "Pizze". Ana owns both, Dora reads Menus at Batel.
/// </summary>
public class MenuCategoryVersionEndpointTests : IClassFixture<PostgresDatabaseFixture>
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
    private static readonly Guid TrattoriaPizzaCategoryId = Guid.Parse("66666666-0000-4000-8000-000000000003");

    private static readonly string PastaUrl = $"/api/v1/menu-categories/{PastaCategoryId}";

    private readonly PostgresDatabaseFixture _postgres;

    public MenuCategoryVersionEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

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
        await factory.SeedCategoryAsync(TrattoriaId, TrattoriaLunchMenuId, TrattoriaPizzaCategoryId, "Pizze");
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId), TenantApiFactory.OwnerOf(TrattoriaId));
        await factory.SeedUserAsync(DoraId, "dora@cantinadanonna.com.br", "Dora", TenantApiFactory.MemberOf(CantinaId));
        await factory.GrantAsync(DoraId, CantinaId, BatelId, (AccessModule.Locations, AccessLevel.Read), (AccessModule.Menus, AccessLevel.Read));
        return factory;
    }

    private static object CategoryBody(string name, uint? version) => version is null
        ? new { name, description = "Massas frescas", sortOrder = 2 }
        : new { name, description = "Massas frescas", sortOrder = 2, version };

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [Trait("Rule", "M67")]
    public async Task CategoryChange_VersionMissing_ReturnsBadRequestOnTheVersionFieldAndChangesNothing(string method)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var request = new HttpRequestMessage(new HttpMethod(method), PastaUrl) { Content = JsonContent.Create(CategoryBody("Massas artesanais", null)) };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "an update or delete that does not say which version it read is refused");
        (await response.ReadErrorFieldsAsync()).ShouldContain("version", "the answer names the version field");
        var stored = await factory.InOrganizationAsync<MenuDbContext, string?>(CantinaId, async db => (await db.MenuCategories.SingleOrDefaultAsync(c => c.Id == PastaCategoryId))?.Name);
        stored.ShouldBe("Massas", "a refused change leaves the category stored as it was");
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [Trait("Rule", "M68")]
    public async Task CategoryChange_VersionOfAnEarlierRead_ReturnsConflictWithTheCurrentCategoryAndKeepsTheFirstChange(string method)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var staleVersion = await client.ReadVersionAsync(PastaUrl);
        (await client.PutAsJsonAsync(PastaUrl, CategoryBody("Massas artesanais", staleVersion))).EnsureSuccessStatusCode();
        var currentVersion = await client.ReadVersionAsync(PastaUrl);
        var request = new HttpRequestMessage(new HttpMethod(method), $"{PastaUrl}?version={staleVersion}")
        {
            Content = JsonContent.Create(CategoryBody("Massas de verão", staleVersion))
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, "a change sent with an old version must not overwrite or delete the first change");
        var body = await response.ReadBodyAsync();
        body.GetProperty("code").GetString().ShouldBe("version.conflict", "the refusal has its own stable code");
        body.GetProperty("current").GetProperty("name").GetString().ShouldBe("Massas artesanais", "the answer carries the category as stored now");
        body.GetProperty("current").GetProperty("version").GetUInt32().ShouldBe(currentVersion, "the carried category has its current version");
        (await client.ReadVersionAsync(PastaUrl)).ShouldBe(currentVersion, "the category is still stored and unchanged");
    }

    [Fact]
    [Trait("Rule", "M69")]
    public async Task UpdateCategory_CurrentVersion_SavesAndReturnsANewVersion()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var version = await client.ReadVersionAsync(PastaUrl);

        // Act
        var response = await client.PutAsJsonAsync(PastaUrl, CategoryBody("Massas artesanais", version));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "the current version may update the category");
        var newVersion = (await response.ReadBodyAsync()).GetProperty("version").GetUInt32();
        newVersion.ShouldNotBe(version, "an update gives the category a new version");
        (await client.ReadVersionAsync(PastaUrl)).ShouldBe(newVersion, "the version returned is the stored one");
    }

    [Fact]
    [Trait("Rule", "M70")]
    public async Task DeleteCategory_CurrentVersion_DeletesTheCategory()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var version = await client.ReadVersionAsync(PastaUrl);

        // Act
        var response = await client.DeleteAtVersionAsync(PastaUrl, version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, "the current version may delete the category");
        var count = await factory.InOrganizationAsync<MenuDbContext, int>(CantinaId, db => db.MenuCategories.CountAsync());
        count.ShouldBe(0, "the category is gone");
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [Trait("Rule", "M71")]
    public async Task CategoryChange_CategoryOfAnotherOrganizationWithItsRealVersion_ReturnsNotFoundAndRevealsNothing(string method)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var trattoriaUrl = $"/api/v1/menu-categories/{TrattoriaPizzaCategoryId}";
        var realVersion = await factory.ClientFor(AnaId, TrattoriaId).ReadVersionAsync(trattoriaUrl);
        var request = new HttpRequestMessage(new HttpMethod(method), $"{trattoriaUrl}?version={realVersion}")
        {
            Content = JsonContent.Create(CategoryBody("Categoria Invadida", realVersion))
        };

        // Act
        var response = await factory.ClientFor(AnaId, CantinaId).SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "another organization's category is not found, even with its real version, never a version conflict");
        var body = await response.ReadBodyAsync();
        body.GetProperty("code").GetString().ShouldBe("menu_category.not_found", "the answer is the ordinary not-found");
        body.TryGetProperty("current", out _).ShouldBeFalse("nothing about the other organization's category is in the body");
        body.GetRawText().ShouldNotContain("Pizze", customMessage: "the other organization's category name never reaches the caller");
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [Trait("Rule", "M72")]
    public async Task CategoryChange_MemberWithMenusReadOnlySendingAStaleVersion_ReturnsForbiddenNotConflict(string method)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var ana = factory.ClientFor(AnaId, CantinaId);
        var staleVersion = await ana.ReadVersionAsync(PastaUrl);
        (await ana.PutAsJsonAsync(PastaUrl, CategoryBody("Massas artesanais", staleVersion))).EnsureSuccessStatusCode();
        var request = new HttpRequestMessage(new HttpMethod(method), $"{PastaUrl}?version={staleVersion}")
        {
            Content = JsonContent.Create(CategoryBody("Massas de verão", staleVersion))
        };

        // Act
        var response = await factory.ClientFor(DoraId, CantinaId).SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "the access check comes before the version check");
        (await response.ReadBodyAsync()).TryGetProperty("current", out _).ShouldBeFalse("a caller who may not change the category is never handed its current record");
    }
}
