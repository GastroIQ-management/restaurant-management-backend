using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Organization.Data;
using RestaurantManagement.Shared.Auth;
using RestaurantManagement.Shared.Data.AuditLog;
using Shouldly;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Location edit and delete (closing) need the version the caller read (lost-update check) on a
/// real PostgreSQL database. Cantina da Nonna has Batel (BATEL) and Mercado Municipal (MERCADO),
/// Trattoria do Sol has Centro. Ana owns both, Caio only reads Locations at Batel and Maria
/// manages Locations at Batel without being the Owner.
/// </summary>
public class LocationVersionEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid CaioId = Guid.Parse("33333333-cccc-4ccc-8ccc-00000000000c");
    private static readonly Guid MariaId = Guid.Parse("66666666-ffff-4fff-8fff-00000000000f");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid TrattoriaId = Guid.Parse("b2b2b2b2-0000-4000-8000-000000000002");
    private static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");
    private static readonly Guid MercadoId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000002");
    private static readonly Guid CentroId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000003");

    private static readonly string BatelUrl = $"/api/v1/locations/{BatelId}";

    private readonly PostgresDatabaseFixture _postgres;

    public LocationVersionEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private async Task<TenantApiFactory> CreateFactoryAsync()
    {
        var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedOrganizationAsync(TrattoriaId, "Trattoria do Sol", Currency.BRL);
        await factory.SeedLocationAsync(CantinaId, BatelId, "Unidade Batel", "BATEL", "America/Sao_Paulo");
        await factory.SeedLocationAsync(CantinaId, MercadoId, "Unidade Mercado Municipal", "MERCADO", "America/Sao_Paulo");
        await factory.SeedLocationAsync(TrattoriaId, CentroId, "Unidade Centro", "CENTRO", "America/Sao_Paulo");
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId), TenantApiFactory.OwnerOf(TrattoriaId));
        await factory.SeedUserAsync(CaioId, "caio@cantinadanonna.com.br", "Caio", TenantApiFactory.MemberOf(CantinaId));
        await factory.SeedUserAsync(MariaId, "maria@cantinadanonna.com.br", "Maria", TenantApiFactory.MemberOf(CantinaId));
        await factory.GrantAsync(CaioId, CantinaId, BatelId, (AccessModule.Locations, AccessLevel.Read));
        await factory.GrantAsync(MariaId, CantinaId, BatelId, (AccessModule.Locations, AccessLevel.Manage));
        return factory;
    }

    private static object LocationBody(string name, string code, uint? version, string status = "Active") => version is null
        ? new { name, address = "Rua Augusta, 1200", city = "Curitiba", state = "PR", postalCode = "80420-000", country = "Brasil", phoneNumber = "+55 41 3000-1000", status, timeZone = "America/Sao_Paulo", code }
        : new { name, address = "Rua Augusta, 1200", city = "Curitiba", state = "PR", postalCode = "80420-000", country = "Brasil", phoneNumber = "+55 41 3000-1000", status, timeZone = "America/Sao_Paulo", code, version };

    private static Task<uint> ReadBatelVersionAsync(HttpClient client) => client.ReadVersionAsync("/api/v1/locations", BatelId);

    private static async Task<string> ReadBatelNameAsync(TenantApiFactory factory)
        => await factory.InOrganizationAsync<OrganizationDbContext, string>(CantinaId, async db => (await db.Locations.SingleAsync(l => l.Id == BatelId)).Name);

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [Trait("Rule", "OR60")]
    public async Task LocationChange_VersionMissing_ReturnsBadRequestOnTheVersionFieldAndChangesNothing(string method)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var request = new HttpRequestMessage(new HttpMethod(method), BatelUrl) { Content = JsonContent.Create(LocationBody("Unidade Batel Reformada", "BATEL", null)) };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "an edit or delete that does not say which version it read is refused");
        (await response.ReadErrorFieldsAsync()).ShouldContain("version", "the answer names the version field");
        var stored = await factory.InOrganizationAsync<OrganizationDbContext, (string Name, string Status)>(CantinaId,
            async db => { var l = await db.Locations.SingleAsync(x => x.Id == BatelId); return (l.Name, l.Status.ToString()); });
        stored.ShouldBe(("Unidade Batel", "Active"), "a refused change leaves the location open and unchanged");
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [Trait("Rule", "OR61")]
    public async Task LocationChange_VersionOfAnEarlierRead_ReturnsConflictWithTheCurrentLocationChangesNothingAndWritesNoAuditEntry(string method)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var staleVersion = await ReadBatelVersionAsync(client);
        (await client.PutAsJsonAsync(BatelUrl, LocationBody("Unidade Batel Reformada", "BATEL", staleVersion))).EnsureSuccessStatusCode();
        var currentVersion = await ReadBatelVersionAsync(client);
        var request = new HttpRequestMessage(new HttpMethod(method), $"{BatelUrl}?version={staleVersion}")
        {
            Content = JsonContent.Create(LocationBody("Unidade Batel Fechada", "BATEL", staleVersion, "Closed"))
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, "a change sent with an old version must not overwrite or close the location");
        var body = await response.ReadBodyAsync();
        body.GetProperty("code").GetString().ShouldBe("version.conflict", "the refusal has its own stable code");
        body.GetProperty("current").GetProperty("name").GetString().ShouldBe("Unidade Batel Reformada", "the answer carries the location as stored now");
        body.GetProperty("current").GetProperty("version").GetUInt32().ShouldBe(currentVersion, "the carried location has its current version");
        (await ReadBatelNameAsync(factory)).ShouldBe("Unidade Batel Reformada", "the location keeps the first change");
        body.GetProperty("current").GetProperty("status").ToString().ShouldBe("Active", "the location is still open");
        var audit = await factory.InOrganizationAsync<AuditLogDbContext, int>(CantinaId, db => db.AuditLogEntries.CountAsync());
        audit.ShouldBe(0, "a refused change writes no audit entry");
    }

    [Fact]
    [Trait("Rule", "OR62")]
    public async Task EditLocation_StaleVersionWithACodeAnotherLocationUses_ReturnsVersionConflictNotTheCodeError()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var staleVersion = await ReadBatelVersionAsync(client);
        (await client.PutAsJsonAsync(BatelUrl, LocationBody("Unidade Batel Reformada", "BATEL", staleVersion))).EnsureSuccessStatusCode();

        // Act
        var response = await client.PutAsJsonAsync(BatelUrl, LocationBody("Unidade Batel Reformada", "MERCADO", staleVersion));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, "the version check comes before the taken-code check");
        (await response.ReadBodyAsync()).GetProperty("code").GetString().ShouldBe("version.conflict", "the refusal is the version conflict, not location.code_already_used");
    }

    [Fact]
    [Trait("Rule", "OR63")]
    public async Task EditLocation_CurrentVersion_SavesAndReturnsANewVersion()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var version = await ReadBatelVersionAsync(client);

        // Act
        var response = await client.PutAsJsonAsync(BatelUrl, LocationBody("Unidade Batel Reformada", "BATEL", version));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "the current version may edit the location");
        var newVersion = (await response.ReadBodyAsync()).GetProperty("version").GetUInt32();
        newVersion.ShouldNotBe(version, "an edit gives the location a new version");
        (await ReadBatelVersionAsync(client)).ShouldBe(newVersion, "the version returned is the stored one");
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [Trait("Rule", "OR64")]
    public async Task LocationChange_LocationOfAnotherOrganizationWithItsRealVersion_ReturnsNotFoundAndRevealsNothing(string method)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var realVersion = await factory.ClientFor(AnaId, TrattoriaId).ReadVersionAsync("/api/v1/locations", CentroId);
        var request = new HttpRequestMessage(new HttpMethod(method), $"/api/v1/locations/{CentroId}?version={realVersion}")
        {
            Content = JsonContent.Create(LocationBody("Unidade Invadida", "INVADIDA", realVersion))
        };

        // Act
        var response = await factory.ClientFor(AnaId, CantinaId).SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "another organization's location is not found, even with its real version, never a version conflict");
        var body = await response.ReadBodyAsync();
        body.GetProperty("code").GetString().ShouldBe("location.not_found", "the answer is the ordinary not-found");
        body.TryGetProperty("current", out _).ShouldBeFalse("nothing about the other organization's location is in the body");
        body.GetRawText().ShouldNotContain("Centro", customMessage: "the other organization's location name never reaches the caller");
    }

    [Theory]
    [InlineData("PUT", "33333333-cccc-4ccc-8ccc-00000000000c")]
    [InlineData("DELETE", "66666666-ffff-4fff-8fff-00000000000f")]
    [Trait("Rule", "OR65")]
    public async Task LocationChange_MemberWhoMayNotMakeIt_SendingAStaleVersion_ReturnsForbiddenNotConflict(string method, string member)
    {
        // Arrange: Caio only reads Locations; Maria manages them but only the Owner may close or delete a location
        await using var factory = await CreateFactoryAsync();
        var ana = factory.ClientFor(AnaId, CantinaId);
        var staleVersion = await ReadBatelVersionAsync(ana);
        (await ana.PutAsJsonAsync(BatelUrl, LocationBody("Unidade Batel Reformada", "BATEL", staleVersion))).EnsureSuccessStatusCode();
        var request = new HttpRequestMessage(new HttpMethod(method), $"{BatelUrl}?version={staleVersion}")
        {
            Content = JsonContent.Create(LocationBody("Unidade Batel Reformada", "BATEL", staleVersion))
        };

        // Act
        var response = await factory.ClientFor(Guid.Parse(member), CantinaId).SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "the access check comes before the version check");
        (await response.ReadBodyAsync()).TryGetProperty("current", out _).ShouldBeFalse("a caller who may not make the change is never handed the current record");
    }
}
