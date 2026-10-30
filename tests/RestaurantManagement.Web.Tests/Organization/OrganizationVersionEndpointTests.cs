using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Organization.Data;
using RestaurantManagement.Shared.Auth;
using RestaurantManagement.Shared.Data.AuditLog;
using Shouldly;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Organization edit (with its settings) needs the version the caller read (lost-update check) on a
/// real PostgreSQL database. Ana owns Cantina da Nonna (BRL) and Maple Leaf Bistro (CAD); Caio is a
/// member of Cantina who manages Locations but is not the Owner; Rita has no organization yet.
/// </summary>
public class OrganizationVersionEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string EditUrl = "/api/v1/organizations/edit";
    private const string ListUrl = "/api/v1/organizations";

    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid CaioId = Guid.Parse("33333333-cccc-4ccc-8ccc-00000000000c");
    private static readonly Guid RitaId = Guid.Parse("88888888-1111-4111-8111-00000000000a");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid MapleId = Guid.Parse("b2b2b2b2-0000-4000-8000-000000000002");
    private static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");

    private readonly PostgresDatabaseFixture _postgres;

    public OrganizationVersionEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private async Task<TenantApiFactory> CreateFactoryAsync()
    {
        var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedOrganizationAsync(MapleId, "Maple Leaf Bistro", Currency.CAD);
        await factory.SeedLocationAsync(CantinaId, BatelId, "Unidade Batel", "BATEL", "America/Sao_Paulo");
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId), TenantApiFactory.OwnerOf(MapleId));
        await factory.SeedUserAsync(CaioId, "caio@cantinadanonna.com.br", "Caio", TenantApiFactory.MemberOf(CantinaId));
        await factory.SeedUserAsync(RitaId, "rita@cantinadanonna.com.br", "Rita");
        await factory.GrantAsync(CaioId, CantinaId, BatelId, (AccessModule.Locations, AccessLevel.Manage));
        return factory;
    }

    private static object EditBody(string name, decimal serviceChargeRate, uint? version) => version is null
        ? new { name, description = "Cozinha italiana de família", serviceChargeRate, discountApprovalLimit = 15.5m, businessDayCutoff = "05:30" }
        : new { name, description = "Cozinha italiana de família", serviceChargeRate, discountApprovalLimit = 15.5m, businessDayCutoff = "05:30", version };

    private static Task<uint> ReadCantinaVersionAsync(HttpClient client) => client.ReadVersionAsync(ListUrl, CantinaId);

    [Fact]
    [Trait("Rule", "OR66")]
    public async Task EditOrganization_VersionMissing_ReturnsBadRequestOnTheVersionFieldAndChangesNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PutAsJsonAsync(EditUrl, EditBody("Cantina da Nonna Centro", 10m, null));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "an edit that does not say which version it read is refused");
        (await response.ReadErrorFieldsAsync()).ShouldContain("version", "the answer names the version field");
        var name = await factory.InOrganizationAsync<OrganizationDbContext, string>(CantinaId, async db => (await db.Organizations.SingleAsync(o => o.Id == CantinaId)).Name);
        name.ShouldBe("Cantina da Nonna", "a refused edit leaves the organization as it was");
    }

    [Fact]
    [Trait("Rule", "OR67")]
    public async Task EditOrganization_VersionOfAnEarlierRead_ReturnsConflictWithTheCurrentOrganizationChangesNothingAndWritesNoAuditEntry()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var staleVersion = await ReadCantinaVersionAsync(client);
        (await client.PutAsJsonAsync(EditUrl, EditBody("Cantina da Nonna Centro", 10m, staleVersion))).EnsureSuccessStatusCode();
        var currentVersion = await ReadCantinaVersionAsync(client);
        var auditBefore = await factory.InOrganizationAsync<AuditLogDbContext, int>(CantinaId, db => db.AuditLogEntries.CountAsync());

        // Act
        var response = await client.PutAsJsonAsync(EditUrl, EditBody("Cantina da Nonna Batel", 12m, staleVersion));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, "an edit sent with an old version must not overwrite the first change");
        var body = await response.ReadBodyAsync();
        body.GetProperty("code").GetString().ShouldBe("version.conflict", "the refusal has its own stable code");
        var current = body.GetProperty("current");
        current.GetProperty("name").GetString().ShouldBe("Cantina da Nonna Centro", "the answer carries the organization as stored now");
        current.GetProperty("serviceChargeRate").GetDecimal().ShouldBe(10m, "the carried organization shows the stored settings, not the refused ones");
        current.GetProperty("version").GetUInt32().ShouldBe(currentVersion, "the carried organization has its current version");
        var auditAfter = await factory.InOrganizationAsync<AuditLogDbContext, int>(CantinaId, db => db.AuditLogEntries.CountAsync());
        auditAfter.ShouldBe(auditBefore, "a refused edit writes no audit entry");
    }

    [Fact]
    [Trait("Rule", "OR68")]
    public async Task EditOrganization_OnlyASettingChanges_ReturnsANewVersion()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var version = await ReadCantinaVersionAsync(client);
        (await client.PutAsJsonAsync(EditUrl, EditBody("Cantina da Nonna", 10m, version))).EnsureSuccessStatusCode();
        var versionAfterFirst = await ReadCantinaVersionAsync(client);

        // Act
        var response = await client.PutAsJsonAsync(EditUrl, EditBody("Cantina da Nonna", 12m, versionAfterFirst));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "the current version may edit the organization");
        var newVersion = (await response.ReadBodyAsync()).GetProperty("version").GetUInt32();
        newVersion.ShouldNotBe(versionAfterFirst, "a change to the settings alone still gives the organization a new version");
        (await ReadCantinaVersionAsync(client)).ShouldBe(newVersion, "the version returned is the stored one");
    }

    [Fact]
    [Trait("Rule", "OR69")]
    public async Task EditOrganization_MemberWhoIsNotTheOwnerSendingAStaleVersion_ReturnsForbiddenNotConflict()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var ana = factory.ClientFor(AnaId, CantinaId);
        var staleVersion = await ReadCantinaVersionAsync(ana);
        (await ana.PutAsJsonAsync(EditUrl, EditBody("Cantina da Nonna Centro", 10m, staleVersion))).EnsureSuccessStatusCode();

        // Act
        var response = await factory.ClientFor(CaioId, CantinaId).PutAsJsonAsync(EditUrl, EditBody("Cantina Invadida", 99m, staleVersion));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "only the Owner edits the organization, and that check comes before the version check");
        (await response.ReadBodyAsync()).TryGetProperty("current", out _).ShouldBeFalse("a caller who may not edit is never handed the current record");
    }

    [Fact]
    [Trait("Rule", "OR70")]
    public async Task EditOrganization_OwnerOfTwoOrganizations_ChangesOnlyTheTokenOrganizationAndItsVersion()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var mapleVersion = await factory.ClientFor(AnaId, MapleId).ReadVersionAsync(ListUrl, MapleId);
        var cantinaVersion = await ReadCantinaVersionAsync(client);

        // Act
        var response = await client.PutAsJsonAsync(EditUrl, EditBody("Cantina da Nonna Centro", 10m, cantinaVersion));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var maple = await factory.InOrganizationAsync<OrganizationDbContext, string>(MapleId, async db => (await db.Organizations.SingleAsync(o => o.Id == MapleId)).Name);
        maple.ShouldBe("Maple Leaf Bistro", "the other organization is never edited");
        (await factory.ClientFor(AnaId, MapleId).ReadVersionAsync(ListUrl, MapleId)).ShouldBe(mapleVersion, "the other organization keeps its version");
    }

    [Theory]
    [InlineData("GET", ListUrl, null, "11111111-aaaa-4aaa-8aaa-00000000000a", "a1a1a1a1-0000-4000-8000-000000000001")]
    [InlineData("POST", "/api/v1/organizations/create", """{"id":"c3c3c3c3-0000-4000-8000-000000000003","name":"Pizzaria Vila Madalena","currency":"BRL"}""", "88888888-1111-4111-8111-00000000000a", null)]
    [Trait("Rule", "OR71")]
    public async Task OrganizationResponses_ListAndCreate_CarryTheVersion(string method, string url, string? json, string user, string? organization)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(Guid.Parse(user), organization is null ? null : Guid.Parse(organization));
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (json is not null)
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.ReadBodyAsync();
        var organizationJson = body.ValueKind == System.Text.Json.JsonValueKind.Array ? body[0] : body;
        organizationJson.GetProperty("version").GetUInt32().ShouldBeGreaterThan(0u, "every organization answer carries the version the next edit must send back");
    }
}
