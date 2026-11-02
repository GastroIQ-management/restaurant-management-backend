using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Menu.Data;
using RestaurantManagement.Shared.Auth;
using Shouldly;
using static RestaurantManagement.Web.Tests.ClientTimeWebSupport;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

/// <summary>A Menu write stores the client times on the new row. Ana owns Cantina da Nonna, which serves the Jantar menu.</summary>
public class MenuCategoryClientTimeEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid DinnerMenuId = Guid.Parse("55555555-0000-4000-8000-000000000001");
    private static readonly Guid DessertCategoryId = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e22");

    private readonly PostgresDatabaseFixture _postgres;

    public MenuCategoryClientTimeEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    // Outcome 8
    [Fact]
    [Trait("Rule", "M76")]
    public async Task CreateMenuCategory_SignedInWriteFourHoursOld_StoresTheTimesOnTheNewRow()
    {
        // Arrange
        await using var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedMenuAsync(CantinaId, DinnerMenuId, "Jantar");
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId));
        var client = factory.ClientFor(AnaId, CantinaId);
        var body = $$"""{ "id": "{{DessertCategoryId}}", "menuId": "{{DinnerMenuId}}", "name": "Sobremesas", "sortOrder": 2 }""";

        // Act
        var response = await client.SendAsync(PostActedAgo("/api/v1/menu-categories", body, TimeSpan.FromHours(4)));

        // Assert
        response.EnsureSuccessStatusCode();
        var category = await factory.InOrganizationAsync<MenuDbContext, RestaurantManagement.Modules.Menu.Entities.MenuCategory>(CantinaId,
            db => db.MenuCategories.AsNoTracking().SingleAsync(c => c.Id == DessertCategoryId));
        category.CorrectedActionTime.ShouldNotBeNull("the new category stores the corrected action time").ShouldBe(DateTime.UtcNow.AddHours(-4), TimeSpan.FromMinutes(1), "it is when the user acted");
        category.ClockError.ShouldNotBeNull("the new category stores the clock error");
        category.ReceivedAt.ShouldNotBeNull("the new category stores the receive time");
    }
}
