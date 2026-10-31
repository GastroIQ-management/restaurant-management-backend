using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Menu.Data;
using RestaurantManagement.Shared.Auth;
using RestaurantManagement.Shared.Data.Idempotency;
using RestaurantManagement.Shared.Services.Idempotency;
using Shouldly;
using static RestaurantManagement.Web.Tests.LocalizedResponses;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Menu writes with an Idempotency-Key, on a real PostgreSQL database. Ana is Owner of Cantina da Nonna
/// (Batel, menu "Jantar"); Bia is a member with Menus Read only at Batel.
/// </summary>
public class MenuIdempotencyEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid BiaId = Guid.Parse("22222222-bbbb-4bbb-8bbb-00000000000b");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");
    private static readonly Guid DinnerMenuId = Guid.Parse("55555555-0000-4000-8000-000000000001");
    private static readonly Guid Key = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e11");

    private readonly PostgresDatabaseFixture _postgres;

    public MenuIdempotencyEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private async Task<TenantApiFactory> CreateFactoryAsync()
    {
        var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedLocationAsync(CantinaId, BatelId, "Unidade Batel", "BATEL", "America/Sao_Paulo");
        await factory.SeedMenuAsync(CantinaId, DinnerMenuId, "Jantar");
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId));
        await factory.SeedUserAsync(BiaId, "bia@cantinadanonna.com.br", "Bia", TenantApiFactory.MemberOf(CantinaId));
        await factory.GrantAsync(BiaId, CantinaId, BatelId, (AccessModule.Menus, AccessLevel.Read));
        return factory;
    }

    private static HttpRequestMessage Post(string url, string key, string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        request.Headers.Add(IdempotencyKeyFilter.HeaderName, key);
        return request;
    }

    private static Task<int> CountKeyRowsAsync(TenantApiFactory factory)
        => factory.InOrganizationAsync<MenuDbContext, int>(CantinaId, db => db.Set<IdempotencyKey>().CountAsync());

    // Failure first
    [Fact]
    [Trait("Rule", "M73")]
    public async Task CreateMenu_MemberWithoutMenusManageSendsAKey_ReturnsForbiddenAndStoresNoKey()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(BiaId, CantinaId);

        // Act
        var response = await client.SendAsync(Post("/api/v1/menus/create", Key.ToString(),
            """{ "id": "55555555-0000-4000-8000-000000000009", "name": "Almoço" }"""));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "a key never opens access the member does not have");
        (await CountKeyRowsAsync(factory)).ShouldBe(0, "a refused request stores no key");
    }

    [Fact]
    [Trait("Rule", "M74")]
    public async Task CreateMenu_ReusedMenuIdWithAKey_ReturnsConflictAndReleasesTheKey()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var refused = await client.SendAsync(Post("/api/v1/menus/create", Key.ToString(), """{ "id": "55555555-0000-4000-8000-000000000001", "name": "Almoço" }"""));

        // Act
        var retried = await client.SendAsync(Post("/api/v1/menus/create", Key.ToString(), """{ "id": "55555555-0000-4000-8000-000000000009", "name": "Almoço" }"""));

        // Assert
        refused.StatusCode.ShouldBe(HttpStatusCode.Conflict, "a menu ID already used is still refused with a key");
        (await CodeAsync(refused)).ShouldBe("id.already_used", "the refusal keeps its own code");
        retried.StatusCode.ShouldBe(HttpStatusCode.OK, "the refused attempt released the key, so the same key can run a corrected request");
    }

    [Fact]
    [Trait("Rule", "M75")]
    public async Task CreateMenuCategory_SameKeyTwice_CreatesOneCategoryAndReplaysCreatedWithLocation()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        const string body = """{ "id": "66666666-0000-4000-8000-000000000001", "menuId": "55555555-0000-4000-8000-000000000001", "name": "Massas", "sortOrder": 1 }""";
        var first = await client.SendAsync(Post("/api/v1/menu-categories", Key.ToString(), body));

        // Act
        var second = await client.SendAsync(Post("/api/v1/menu-categories", Key.ToString(), body));

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        second.StatusCode.ShouldBe(HttpStatusCode.Created, "the replay keeps the 201 status");
        second.Headers.Location.ShouldBe(first.Headers.Location, "the replay keeps the Location header");
        (await factory.InOrganizationAsync<MenuDbContext, int>(CantinaId, db => db.MenuCategories.CountAsync())).ShouldBe(1, "the category was created once");
    }
}
