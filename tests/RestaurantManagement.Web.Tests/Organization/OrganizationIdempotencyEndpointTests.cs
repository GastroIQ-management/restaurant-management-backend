using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Identity.Data;
using RestaurantManagement.Modules.Organization.Data;
using RestaurantManagement.Shared.Services.Idempotency;
using Shouldly;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Organization writes with an Idempotency-Key, on a real PostgreSQL database. Ana is Owner of Cantina da Nonna
/// (Batel); Livia has an account and no organization yet.
/// </summary>
public class OrganizationIdempotencyEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid LiviaId = Guid.Parse("99999999-0000-4000-8000-0000000000a1");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");
    private static readonly Guid NewOrganizationId = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e44");
    private static readonly Guid Key = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e11");

    private const string LocationJson = """
        { "id": "01933b2e-7c10-7a55-9d3e-5b1f0c2a4e33", "name": "Unidade Moema", "address": "Rua Augusta, 1200", "city": "São Paulo", "state": "SP",
          "postalCode": "01304-001", "country": "Brasil", "phoneNumber": "+55 11 3000-1000", "status": "Active",
          "timeZone": "America/Sao_Paulo", "code": "MOE" }
        """;

    private const string OrganizationJson = """{ "id": "01933b2e-7c10-7a55-9d3e-5b1f0c2a4e44", "name": "Pizzaria Vila Madalena", "currency": "BRL" }""";

    private readonly PostgresDatabaseFixture _postgres;

    public OrganizationIdempotencyEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private async Task<TenantApiFactory> CreateFactoryAsync()
    {
        var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedLocationAsync(CantinaId, BatelId, "Unidade Batel", "BATEL", "America/Sao_Paulo");
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId));
        await factory.SeedUserAsync(LiviaId, "livia@pizzariavm.com.br", "Livia");
        return factory;
    }

    private static HttpRequestMessage Post(string url, string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        request.Headers.Add(IdempotencyKeyFilter.HeaderName, Key.ToString());
        return request;
    }

    private static Task<int> CountMembershipsAsync(TenantApiFactory factory)
        => factory.InOrganizationAsync<IdentityDbContext, int>(NewOrganizationId,
            db => db.OrganizationMembers.IgnoreQueryFilters().CountAsync(m => m.OrganizationId == NewOrganizationId && m.UserId == LiviaId));

    // Failure first
    [Fact]
    [Trait("Rule", "OR72")]
    public async Task CreateOrganization_FailureWhileSavingTheKey_LeavesNoOrganizationAndNoOwnerAccess()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        await factory.InOrganizationAsync<OrganizationDbContext, int>(CantinaId, db => db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION fail_key_completion() RETURNS trigger AS $$ BEGIN RAISE EXCEPTION 'forced failure while saving the key'; END; $$ LANGUAGE plpgsql;
            CREATE TRIGGER fail_key_completion BEFORE UPDATE ON organization_idempotency_keys FOR EACH ROW EXECUTE FUNCTION fail_key_completion();
            """));
        var client = factory.ClientFor(LiviaId, null);

        // Act
        var response = await client.SendAsync(Post("/api/v1/organizations/create", OrganizationJson));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError, "the failed key save fails the request");
        (await factory.InOrganizationAsync<OrganizationDbContext, int>(NewOrganizationId, db => db.Organizations.CountAsync()))
            .ShouldBe(0, "the organization is not kept without its key");
        (await CountMembershipsAsync(factory)).ShouldBe(0, "the Owner access set up in Identity is revoked when the write does not commit");
    }

    [Fact]
    [Trait("Rule", "OR73")]
    public async Task CreateOrganization_SameKeyTwiceWithNoOrganizationInTheToken_CreatesOneAndReplaysTheAnswer()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(LiviaId, null);
        var first = await client.SendAsync(Post("/api/v1/organizations/create", OrganizationJson));

        // Act
        var second = await client.SendAsync(Post("/api/v1/organizations/create", OrganizationJson));

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK, "the replay is not refused by the one-trial-organization rule");
        (await second.Content.ReadAsStringAsync()).ShouldBe(await first.Content.ReadAsStringAsync(), "the replay returns the saved answer");
        (await CountMembershipsAsync(factory)).ShouldBe(1, "the organization and its Owner were set up once");
    }

    [Fact]
    [Trait("Rule", "OR74")]
    public async Task CreateLocation_SameKeyTwice_CreatesOneLocationAndAnswersTheSameBothTimes()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var first = await client.SendAsync(Post("/api/v1/locations", LocationJson));

        // Act
        var second = await client.SendAsync(Post("/api/v1/locations", LocationJson));

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(first.StatusCode, "the replay keeps the status");
        (await second.Content.ReadAsStringAsync()).ShouldBe(await first.Content.ReadAsStringAsync(), "the replay returns the saved body");
        (await factory.InOrganizationAsync<OrganizationDbContext, int>(CantinaId, db => db.Locations.CountAsync(l => l.Code == "MOE")))
            .ShouldBe(1, "the location was created once");
    }
}
