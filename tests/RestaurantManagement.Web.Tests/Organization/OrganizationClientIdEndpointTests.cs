using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Identity.Data;
using RestaurantManagement.Modules.Organization.Data;
using RestaurantManagement.Modules.Organization.Entities;
using RestaurantManagement.Shared.Auth;
using Shouldly;
using static RestaurantManagement.Web.Tests.LocalizedResponses;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Location and organization create with an ID chosen by the client, on a real PostgreSQL database.
/// Ana is Owner of Cantina da Nonna (location Batel); Bruno is Owner of Trattoria do Sol (location
/// Centro); Bia is a member of Cantina with Locations Manage at Batel (not the Owner).
/// </summary>
public class OrganizationClientIdEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid BrunoId = Guid.Parse("33333333-cccc-4ccc-8ccc-00000000000c");
    private static readonly Guid BiaId = Guid.Parse("22222222-bbbb-4bbb-8bbb-00000000000b");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid TrattoriaId = Guid.Parse("b2b2b2b2-0000-4000-8000-000000000002");
    private static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");
    private static readonly Guid TrattoriaCentroId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000003");
    private static readonly Guid NewLocationId = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e33");
    private static readonly Guid NewOrganizationId = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e44");

    private readonly PostgresDatabaseFixture _postgres;

    public OrganizationClientIdEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private async Task<TenantApiFactory> CreateFactoryAsync()
    {
        var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedOrganizationAsync(TrattoriaId, "Trattoria do Sol", Currency.BRL);
        await factory.SeedLocationAsync(CantinaId, BatelId, "Unidade Batel", "BATEL", "America/Sao_Paulo");
        await factory.SeedLocationAsync(TrattoriaId, TrattoriaCentroId, "Unidade Centro", "CENTRO", "America/Sao_Paulo");
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId));
        await factory.SeedUserAsync(BrunoId, "bruno@trattoriadosol.com.br", "Bruno", TenantApiFactory.OwnerOf(TrattoriaId));
        await factory.SeedUserAsync(BiaId, "bia@cantinadanonna.com.br", "Bia", TenantApiFactory.MemberOf(CantinaId));
        await factory.GrantAsync(BiaId, CantinaId, BatelId, (AccessModule.Locations, AccessLevel.Manage));
        return factory;
    }

    /// <summary>Moves Cantina out of the trial plan, so the trial limit does not refuse Ana's organization create.</summary>
    private static Task LeaveTrialAsync(TenantApiFactory factory)
        => factory.InOrganizationAsync<OrganizationDbContext, bool>(CantinaId, async db =>
        {
            var settings = await db.OrganizationSettings.SingleAsync();
            settings.PlanType = "ENTERPRISE";
            settings.IsTrialActive = false;
            await db.SaveChangesAsync();
            return true;
        });

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private static object LocationBody(Guid id, string code) => new
    {
        id,
        organizationId = TrattoriaId,
        name = "Unidade Moema",
        address = "Rua Augusta, 1200",
        city = "São Paulo",
        state = "SP",
        postalCode = "01304-001",
        country = "Brasil",
        phoneNumber = "+55 11 3000-1000",
        status = "Active",
        timeZone = "America/Sao_Paulo",
        code
    };

    private static string LocationJson(string idPart) => $$"""
        { {{idPart}} "name": "Unidade Moema", "address": "Rua Augusta, 1200", "city": "São Paulo", "state": "SP",
          "postalCode": "01304-001", "country": "Brasil", "phoneNumber": "+55 11 3000-1000", "status": "Active",
          "timeZone": "America/Sao_Paulo", "code": "MOE" }
        """;

    private static string OrganizationJson(string idPart) => $$"""
        { {{idPart}} "name": "Pizzaria Vila Madalena", "currency": "BRL" }
        """;

    private static Task<int> CountLocationsAsync(TenantApiFactory factory, Guid organizationId)
        => factory.InOrganizationAsync<OrganizationDbContext, int>(organizationId, db => db.Locations.CountAsync());

    private static Task<int> CountMembershipsAsync(TenantApiFactory factory, Guid organizationId, Guid userId)
        => factory.InOrganizationAsync<IdentityDbContext, int>(organizationId,
            db => db.OrganizationMembers.IgnoreQueryFilters().CountAsync(m => m.OrganizationId == organizationId && m.UserId == userId));

    // Locations: failures first
    [Theory]
    [InlineData("")]
    [InlineData("\"id\": null,")]
    [InlineData("\"id\": \"terraco\",")]
    [InlineData("\"id\": \"00000000-0000-0000-0000-000000000000\",")]
    [Trait("Rule", "OR49")]
    public async Task CreateLocation_IdMissingNullMalformedOrEmpty_ReturnsBadRequestOnId(string idPart)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PostAsync("/api/v1/locations", Json(LocationJson(idPart)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a location without a usable ID (missing, null, not an ID, all zeros) is refused");
        (await FieldErrorsAsync(response, "Id")).ShouldContain("The Id field is required.", "the error is on the ID field");
    }

    [Fact]
    [Trait("Rule", "OR50")]
    public async Task CreateLocation_IdUsedInSameOrganization_ReturnsConflictNotCodeError()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/locations", LocationBody(BatelId, "MOE"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, "a location ID already used in the organization is refused with 409");
        (await CodeAsync(response)).ShouldBe("id.already_used", "a reused ID is not reported as a taken location code");
        (await CountLocationsAsync(factory, CantinaId)).ShouldBe(1, "the refused create stores nothing");
    }

    [Fact]
    [Trait("Rule", "OR51")]
    public async Task CreateLocation_IdUsedByAnotherOrganization_AnswersTheSameConflictAndRevealsNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var sameOrganization = await client.PostAsJsonAsync("/api/v1/locations", LocationBody(BatelId, "MOE"));

        // Act
        var otherOrganization = await client.PostAsJsonAsync("/api/v1/locations", LocationBody(TrattoriaCentroId, "MOE"));

        // Assert
        otherOrganization.StatusCode.ShouldBe(sameOrganization.StatusCode, "a clash with another organization has the same status as a clash in the own one");
        (await ProblemWithoutTraceIdAsync(otherOrganization)).ShouldBe(await ProblemWithoutTraceIdAsync(sameOrganization),
            "title and code are identical, so nobody can tell whose location holds the ID");
        var body = await otherOrganization.Content.ReadAsStringAsync();
        body.ShouldNotContain("Unidade Centro", Case.Insensitive, "the body holds no name of the other organization's location");
        body.ShouldNotContain(TrattoriaId.ToString(), Case.Insensitive, "the body holds no ID of the other organization");
        var untouched = await factory.InOrganizationAsync<OrganizationDbContext, string>(TrattoriaId,
            async db => (await db.Locations.SingleAsync(l => l.Id == TrattoriaCentroId)).Name);
        untouched.ShouldBe("Unidade Centro", "the other organization's location is unchanged");
    }

    [Fact]
    [Trait("Rule", "OR52")]
    public async Task CreateLocation_NonOwnerWithLocationsManageSendsTakenId_ReturnsForbiddenNotConflict()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(BiaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/locations", LocationBody(BatelId, "MOE"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "only the Owner creates locations, checked before the ID, so a non-Owner never learns whether an ID is taken");
    }

    [Fact]
    [Trait("Rule", "OR53")]
    public async Task CreateLocation_NewId_StoresLocationUnderThatIdInTokenOrganization()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/locations", LocationBody(NewLocationId, "MOE"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "the Owner creates a location with an ID of the client's choice");
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!["id"]!.GetValue<Guid>().ShouldBe(NewLocationId, "the response carries the ID the client chose");
        var organizationId = await factory.InOrganizationAsync<OrganizationDbContext, Guid?>(CantinaId,
            async db => await db.Locations.Where(l => l.Id == NewLocationId).Select(l => (Guid?)l.OrganizationId).SingleOrDefaultAsync());
        organizationId.ShouldBe(CantinaId, "the location is stored under the client's ID in the token organization, whatever organization the body names");
    }

    // Organizations
    [Theory]
    [InlineData("")]
    [InlineData("\"id\": null,")]
    [InlineData("\"id\": \"pizzaria\",")]
    [InlineData("\"id\": \"00000000-0000-0000-0000-000000000000\",")]
    [Trait("Rule", "OR54")]
    public async Task CreateOrganization_IdMissingNullMalformedOrEmpty_ReturnsBadRequestOnId(string idPart)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, organizationId: null);

        // Act
        var response = await client.PostAsync("/api/v1/organizations/create", Json(OrganizationJson(idPart)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "an organization without a usable ID (missing, null, not an ID, all zeros) is refused");
        (await FieldErrorsAsync(response, "Id")).ShouldContain("The Id field is required.", "the error is on the ID field");
    }

    [Fact]
    [Trait("Rule", "OR55")]
    public async Task CreateOrganization_IdOfUsersOwnOrganization_ReturnsConflictAndCreatesNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        await LeaveTrialAsync(factory);
        var client = factory.ClientFor(AnaId, organizationId: null);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/organizations/create", new { id = CantinaId, name = "Pizzaria Vila Madalena", currency = "BRL" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, "the ID of the user's own organization is refused with 409, never a 500");
        (await CodeAsync(response)).ShouldBe("id.already_used", "the refusal carries the reused-ID code");
        var name = await factory.InOrganizationAsync<OrganizationDbContext, string>(CantinaId, async db => (await db.Organizations.SingleAsync()).Name);
        name.ShouldBe("Cantina da Nonna", "the existing organization is unchanged");
        (await CountMembershipsAsync(factory, CantinaId, AnaId)).ShouldBe(1, "no second Owner access row is created");
    }

    [Fact]
    [Trait("Rule", "OR56")]
    public async Task CreateOrganization_IdOfAnotherUsersOrganization_AnswersTheSameConflictAndRevealsNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        await LeaveTrialAsync(factory);
        var client = factory.ClientFor(AnaId, organizationId: null);
        var ownOrganization = await client.PostAsJsonAsync("/api/v1/organizations/create", new { id = CantinaId, name = "Pizzaria Vila Madalena", currency = "BRL" });

        // Act
        var otherUsersOrganization = await client.PostAsJsonAsync("/api/v1/organizations/create", new { id = TrattoriaId, name = "Pizzaria Vila Madalena", currency = "BRL" });

        // Assert
        otherUsersOrganization.StatusCode.ShouldBe(ownOrganization.StatusCode, "a clash with another user's organization has the same status as a clash with the user's own");
        (await ProblemWithoutTraceIdAsync(otherUsersOrganization)).ShouldBe(await ProblemWithoutTraceIdAsync(ownOrganization),
            "title and code are identical, so nobody can tell whose organization holds the ID");
        (await otherUsersOrganization.Content.ReadAsStringAsync()).ShouldNotContain("Trattoria do Sol", Case.Insensitive, "the body holds no name of the other organization");
        (await CountMembershipsAsync(factory, TrattoriaId, AnaId)).ShouldBe(0, "the refused create gives Ana no access to the other organization");
        (await CountMembershipsAsync(factory, TrattoriaId, BrunoId)).ShouldBe(1, "the other organization keeps its Owner and gains no second row");
    }

    [Fact]
    [Trait("Rule", "OR57")]
    public async Task CreateOrganization_UserInTrialSendsTakenId_ReturnsTrialLimitNotConflict()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, organizationId: null);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/organizations/create", new { id = TrattoriaId, name = "Pizzaria Vila Madalena", currency = "BRL" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "the trial limit is checked before the ID, so a user in trial never learns whether an ID is taken");
        (await CodeAsync(response)).ShouldBe("organization.trial_limit_reached", "the trial-limit refusal keeps its place before the clash");
    }

    [Fact]
    [Trait("Rule", "OR58")]
    public async Task CreateOrganization_NewId_StoresOrganizationUnderThatIdWithOwnerAccess()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        await LeaveTrialAsync(factory);
        var client = factory.ClientFor(AnaId, organizationId: null);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/organizations/create", new { id = NewOrganizationId, name = "Pizzaria Vila Madalena", currency = "BRL" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "a user may create an organization with an ID of the client's choice");
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!["id"]!.GetValue<Guid>().ShouldBe(NewOrganizationId, "the response carries the ID the client chose");
        var name = await factory.InOrganizationAsync<OrganizationDbContext, string?>(NewOrganizationId,
            async db => (await db.Organizations.IgnoreQueryFilters().SingleOrDefaultAsync(o => o.Id == NewOrganizationId))?.Name);
        name.ShouldBe("Pizzaria Vila Madalena", "the organization is stored under the client's ID");
        (await CountMembershipsAsync(factory, NewOrganizationId, AnaId)).ShouldBe(1, "the creator becomes the Owner of the new organization");
    }
}
