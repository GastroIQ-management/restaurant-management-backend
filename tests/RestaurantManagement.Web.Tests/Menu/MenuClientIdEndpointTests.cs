using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Menu.Data;
using RestaurantManagement.Shared.Auth;
using Shouldly;
using static RestaurantManagement.Web.Tests.LocalizedResponses;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Menu and category create with an ID chosen by the client, on a real PostgreSQL database.
/// Ana is Owner of Cantina da Nonna (menu "Jantar", category "Massas") and of Trattoria do Sol
/// (menu "Pranzo", category "Pizze"); Bia is a member of Cantina with Menus Read only at Batel.
/// </summary>
public class MenuClientIdEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid BiaId = Guid.Parse("22222222-bbbb-4bbb-8bbb-00000000000b");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid TrattoriaId = Guid.Parse("b2b2b2b2-0000-4000-8000-000000000002");
    private static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");
    private static readonly Guid TrattoriaCentroId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000003");
    private static readonly Guid CantinaDinnerMenuId = Guid.Parse("55555555-0000-4000-8000-000000000001");
    private static readonly Guid TrattoriaLunchMenuId = Guid.Parse("55555555-0000-4000-8000-000000000002");
    private static readonly Guid CantinaPastaCategoryId = Guid.Parse("66666666-0000-4000-8000-000000000001");
    private static readonly Guid TrattoriaPizzaCategoryId = Guid.Parse("66666666-0000-4000-8000-000000000002");
    private static readonly Guid NewMenuId = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e11");
    private static readonly Guid NewCategoryId = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e22");

    private readonly PostgresDatabaseFixture _postgres;

    public MenuClientIdEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private sealed record Created(Guid Id, Guid OrganizationId, string Name);

    private async Task<TenantApiFactory> CreateFactoryAsync()
    {
        var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedOrganizationAsync(TrattoriaId, "Trattoria do Sol", Currency.BRL);
        await factory.SeedLocationAsync(CantinaId, BatelId, "Unidade Batel", "BATEL", "America/Sao_Paulo");
        await factory.SeedLocationAsync(TrattoriaId, TrattoriaCentroId, "Unidade Centro", "CENTRO", "America/Sao_Paulo");
        await factory.SeedMenuAsync(CantinaId, CantinaDinnerMenuId, "Jantar");
        await factory.SeedMenuAsync(TrattoriaId, TrattoriaLunchMenuId, "Pranzo");
        await factory.SeedCategoryAsync(CantinaId, CantinaDinnerMenuId, CantinaPastaCategoryId, "Massas");
        await factory.SeedCategoryAsync(TrattoriaId, TrattoriaLunchMenuId, TrattoriaPizzaCategoryId, "Pizze");
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana",
            TenantApiFactory.OwnerOf(CantinaId), TenantApiFactory.OwnerOf(TrattoriaId));
        await factory.SeedUserAsync(BiaId, "bia@cantinadanonna.com.br", "Bia", TenantApiFactory.MemberOf(CantinaId));
        await factory.GrantAsync(BiaId, CantinaId, BatelId, (AccessModule.Menus, AccessLevel.Read));
        return factory;
    }

    // Failure first
    [Theory]
    [InlineData("""{ "name": "Almoço executivo" }""")]
    [InlineData("""{ "id": null, "name": "Almoço executivo" }""")]
    [InlineData("""{ "id": "almoco", "name": "Almoço executivo" }""")]
    [InlineData("""{ "id": "00000000-0000-0000-0000-000000000000", "name": "Almoço executivo" }""")]
    [Trait("Rule", "M49")]
    public async Task CreateMenu_IdMissingNullMalformedOrEmpty_ReturnsBadRequestOnId(string body)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.SendAsync(Request("POST", "/api/v1/menus/create", "en", body));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a menu without a usable ID (missing, null, not an ID, all zeros) is refused");
        (await FieldErrorsAsync(response, "Id")).ShouldContain("The Id field is required.", "the error is on the ID field");
    }

    [Fact]
    [Trait("Rule", "M50")]
    public async Task CreateMenu_IdUsedInSameOrganization_ReturnsConflictAndStoresNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/menus/create",
            new { id = CantinaDinnerMenuId, name = "Almoço executivo", locationIds = new[] { BatelId } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, "a menu ID already used in the organization is refused with 409");
        (await CodeAsync(response)).ShouldBe("id.already_used", "the refusal carries the reused-ID code");
        var stored = await factory.InOrganizationAsync<MenuDbContext, (string Name, int Links)>(CantinaId, async db =>
            ((await db.Menus.SingleAsync(m => m.Id == CantinaDinnerMenuId)).Name,
             await db.LocationMenus.CountAsync(l => l.MenuId == CantinaDinnerMenuId)));
        stored.Name.ShouldBe("Jantar", "the existing menu is unchanged");
        stored.Links.ShouldBe(0, "the refused create is rolled back: no location link is stored");
    }

    [Fact]
    [Trait("Rule", "M51")]
    public async Task CreateMenu_IdUsedByAnotherOrganization_AnswersTheSameConflictAndRevealsNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var sameOrganization = await client.PostAsJsonAsync("/api/v1/menus/create", new { id = CantinaDinnerMenuId, name = "Almoço executivo" });

        // Act
        var otherOrganization = await client.PostAsJsonAsync("/api/v1/menus/create", new { id = TrattoriaLunchMenuId, name = "Almoço executivo" });

        // Assert
        otherOrganization.StatusCode.ShouldBe(sameOrganization.StatusCode, "a clash with another organization has the same status as a clash in the own one");
        (await ProblemWithoutTraceIdAsync(otherOrganization)).ShouldBe(await ProblemWithoutTraceIdAsync(sameOrganization),
            "title and code are identical, so nobody can tell whose menu holds the ID");
        var body = await otherOrganization.Content.ReadAsStringAsync();
        body.ShouldNotContain("Pranzo", Case.Insensitive, "the body holds no name of the other organization's menu");
        body.ShouldNotContain(TrattoriaId.ToString(), Case.Insensitive, "the body holds no ID of the other organization");
        var untouched = await factory.InOrganizationAsync<MenuDbContext, Created>(TrattoriaId,
            async db => await db.Menus.Where(m => m.Id == TrattoriaLunchMenuId).Select(m => new Created(m.Id, m.OrganizationId, m.Name)).SingleAsync());
        untouched.ShouldBe(new Created(TrattoriaLunchMenuId, TrattoriaId, "Pranzo"), "the other organization's menu is unchanged");
    }

    [Fact]
    [Trait("Rule", "M52")]
    public async Task CreateMenu_MemberWithoutMenusManageSendsTakenId_ReturnsForbiddenNotConflict()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(BiaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/menus/create", new { id = CantinaDinnerMenuId, name = "Almoço executivo" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "access is checked before the ID, so a caller without the right never learns whether an ID is taken");
    }

    [Fact]
    [Trait("Rule", "M53")]
    public async Task CreateMenu_NewId_StoresMenuUnderThatIdInTokenOrganization()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/menus/create", new { id = NewMenuId, name = "Almoço executivo" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "an Owner creates a menu with an ID of the client's choice");
        (await response.Content.ReadFromJsonAsync<Created>())!.Id.ShouldBe(NewMenuId, "the response carries the ID the client chose");
        var stored = await factory.InOrganizationAsync<MenuDbContext, Created?>(CantinaId,
            db => db.Menus.Where(m => m.Id == NewMenuId).Select(m => new Created(m.Id, m.OrganizationId, m.Name)).SingleOrDefaultAsync());
        stored.ShouldNotBeNull("the menu is stored under the client's ID");
        stored.OrganizationId.ShouldBe(CantinaId, "the menu belongs to the token organization");
    }

    [Theory]
    [InlineData("""{ "menuId": "55555555-0000-4000-8000-000000000001", "name": "Sobremesas" }""")]
    [InlineData("""{ "id": null, "menuId": "55555555-0000-4000-8000-000000000001", "name": "Sobremesas" }""")]
    [InlineData("""{ "id": "sobremesas", "menuId": "55555555-0000-4000-8000-000000000001", "name": "Sobremesas" }""")]
    [InlineData("""{ "id": "00000000-0000-0000-0000-000000000000", "menuId": "55555555-0000-4000-8000-000000000001", "name": "Sobremesas" }""")]
    [Trait("Rule", "M54")]
    public async Task CreateMenuCategory_IdMissingNullMalformedOrEmpty_ReturnsBadRequestOnId(string body)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.SendAsync(Request("POST", "/api/v1/menu-categories", "en", body));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a category without a usable ID (missing, null, not an ID, all zeros) is refused");
        (await FieldErrorsAsync(response, "Id")).ShouldContain("The Id field is required.", "the error is on the ID field");
    }

    [Fact]
    [Trait("Rule", "M55")]
    public async Task CreateMenuCategory_IdUsedInSameOrganization_ReturnsConflictAndStoresNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/menu-categories",
            new { id = CantinaPastaCategoryId, menuId = CantinaDinnerMenuId, name = "Sobremesas", sortOrder = 2 });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, "a category ID already used in the organization is refused with 409");
        (await CodeAsync(response)).ShouldBe("id.already_used", "the refusal carries the reused-ID code");
        var stored = await factory.InOrganizationAsync<MenuDbContext, string>(CantinaId,
            async db => (await db.MenuCategories.SingleAsync(c => c.Id == CantinaPastaCategoryId)).Name);
        stored.ShouldBe("Massas", "the existing category is unchanged");
    }

    [Fact]
    [Trait("Rule", "M56")]
    public async Task CreateMenuCategory_IdUsedByAnotherOrganization_AnswersTheSameConflictAndRevealsNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var sameOrganization = await client.PostAsJsonAsync("/api/v1/menu-categories",
            new { id = CantinaPastaCategoryId, menuId = CantinaDinnerMenuId, name = "Sobremesas" });

        // Act
        var otherOrganization = await client.PostAsJsonAsync("/api/v1/menu-categories",
            new { id = TrattoriaPizzaCategoryId, menuId = CantinaDinnerMenuId, name = "Sobremesas" });

        // Assert
        otherOrganization.StatusCode.ShouldBe(sameOrganization.StatusCode, "a clash with another organization has the same status as a clash in the own one");
        (await ProblemWithoutTraceIdAsync(otherOrganization)).ShouldBe(await ProblemWithoutTraceIdAsync(sameOrganization),
            "title and code are identical, so nobody can tell whose category holds the ID");
        var body = await otherOrganization.Content.ReadAsStringAsync();
        body.ShouldNotContain("Pizze", Case.Insensitive, "the body holds no name of the other organization's category");
        body.ShouldNotContain(TrattoriaId.ToString(), Case.Insensitive, "the body holds no ID of the other organization");
        var untouched = await factory.InOrganizationAsync<MenuDbContext, string>(TrattoriaId,
            async db => (await db.MenuCategories.SingleAsync(c => c.Id == TrattoriaPizzaCategoryId)).Name);
        untouched.ShouldBe("Pizze", "the other organization's category is unchanged");
    }

    [Fact]
    [Trait("Rule", "M57")]
    public async Task CreateMenuCategory_MemberWithMenusReadOnlySendsTakenId_ReturnsForbiddenNotConflict()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(BiaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/menu-categories",
            new { id = CantinaPastaCategoryId, menuId = CantinaDinnerMenuId, locationId = BatelId, name = "Sobremesas" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "access is checked before the ID, so a caller with Menus Read only never learns whether an ID is taken");
    }

    [Fact]
    [Trait("Rule", "M58")]
    public async Task CreateMenuCategory_NewId_StoresCategoryUnderThatIdInTokenOrganization()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/menu-categories",
            new { id = NewCategoryId, organizationId = TrattoriaId, menuId = CantinaDinnerMenuId, name = "Sobremesas", sortOrder = 2 });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created, "an Owner creates a category with an ID of the client's choice");
        (await response.Content.ReadFromJsonAsync<Created>())!.Id.ShouldBe(NewCategoryId, "the response carries the ID the client chose");
        var stored = await factory.InOrganizationAsync<MenuDbContext, Guid?>(CantinaId,
            async db => await db.MenuCategories.Where(c => c.Id == NewCategoryId).Select(c => (Guid?)c.OrganizationId).SingleOrDefaultAsync());
        stored.ShouldBe(CantinaId, "the category is stored under the client's ID in the token organization");
    }
}
