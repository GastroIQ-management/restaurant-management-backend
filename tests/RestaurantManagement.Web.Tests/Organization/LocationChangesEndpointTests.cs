using System.Net;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Organization.Data;
using RestaurantManagement.Modules.Organization.Entities;
using RestaurantManagement.Shared.Services.Changes;
using Shouldly;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The locations "changes since" feed on a real PostgreSQL database. Cantina da Nonna has Batel, Mercado Municipal and
/// Água Verde, Trattoria do Sol has Centro. Ana owns both organizations; Maria is a member of Cantina with nothing granted.
/// Locations are organization data, so the feed needs no location header.
/// </summary>
[Collection(ChangesFeedCollection.Name)]
public class LocationChangesEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string Route = "/api/v1/locations/changes";

    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid MariaId = Guid.Parse("66666666-ffff-4fff-8fff-00000000000f");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid TrattoriaId = Guid.Parse("b2b2b2b2-0000-4000-8000-000000000002");
    private static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");
    private static readonly Guid MercadoId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000002");
    private static readonly Guid AguaVerdeId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000004");
    private static readonly Guid CentroId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000003");

    private readonly PostgresDatabaseFixture _postgres;

    public LocationChangesEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private sealed record LocationRow(Guid Id, string Name, string Status);

    private async Task<TenantApiFactory> CreateFactoryAsync()
    {
        var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedOrganizationAsync(TrattoriaId, "Trattoria do Sol", Currency.BRL);
        await factory.SeedLocationAsync(CantinaId, BatelId, "Unidade Batel", "BATEL", "America/Sao_Paulo");
        await factory.SeedLocationAsync(CantinaId, MercadoId, "Unidade Mercado Municipal", "MERCADO", "America/Sao_Paulo");
        await factory.SeedLocationAsync(CantinaId, AguaVerdeId, "Unidade Água Verde", "AGUAVERDE", "America/Sao_Paulo");
        await factory.SeedLocationAsync(TrattoriaId, CentroId, "Unidade Centro", "CENTRO", "America/Sao_Paulo");
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId), TenantApiFactory.OwnerOf(TrattoriaId));
        await factory.SeedUserAsync(MariaId, "maria@cantinadanonna.com.br", "Maria", TenantApiFactory.MemberOf(CantinaId));
        return factory;
    }

    private static Task<bool> ChangeLocationAsync(TenantApiFactory factory, Guid organizationId, Guid locationId, Action<Location> change)
        => factory.InOrganizationAsync<OrganizationDbContext, bool>(organizationId, async db =>
        {
            change(await db.Locations.SingleAsync(l => l.Id == locationId));
            await db.SaveChangesAsync();
            return true;
        });

    [Fact]
    [Trait("Rule", "OR79")]
    public async Task GetLocationChanges_OneOfThreeLocationsEdited_ReturnsExactlyItAndNeverAnotherOrganizationsLocations()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var first = await client.ReadChangesAsync<LocationRow>(Route);
        await ChangeLocationAsync(factory, CantinaId, BatelId, l => l.Name = "Unidade Batel Reformada");
        await ChangeLocationAsync(factory, TrattoriaId, CentroId, l => l.Name = "Unidade Centro Histórico");

        // Act
        var second = await client.ReadChangesAsync<LocationRow>(Route, first.Cursor);

        // Assert
        first.Changed.Select(l => l.Id).OrderBy(id => id).ShouldBe(new[] { BatelId, MercadoId, AguaVerdeId },
            ignoreOrder: false, customMessage: "the owner of Cantina da Nonna gets its three locations and never Trattoria do Sol's");
        second.Changed.Select(l => l.Id).ShouldBe(new[] { BatelId }, ignoreOrder: false,
            customMessage: "only the edited location comes back, and another organization's edit never does");
        second.Changed.Single().Name.ShouldBe("Unidade Batel Reformada");
        second.Deleted.ShouldBeEmpty("locations are never deleted");
    }

    [Fact]
    [Trait("Rule", "OR80")]
    public async Task GetLocationChanges_CursorOfAnotherOrganization_Returns400CursorInvalid()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var cantinaCursor = (await factory.ClientFor(AnaId, CantinaId).ReadChangesAsync<LocationRow>(Route)).Cursor;
        var trattoria = factory.ClientFor(AnaId, TrattoriaId);

        // Act
        var response = await trattoria.GetChangesAsync(Route, cantinaCursor);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a cursor of Cantina da Nonna means nothing to Trattoria do Sol");
        (await response.ReadCodeAsync()).ShouldBe("changes.cursor_invalid", "a cursor bound to another organization is invalid");
    }

    [Fact]
    [Trait("Rule", "OR81")]
    public async Task GetLocationChanges_LocationClosed_ComesBackAsChangedWithStatusClosedNeverAsDeleted()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var first = await client.ReadChangesAsync<LocationRow>(Route);
        await ChangeLocationAsync(factory, CantinaId, MercadoId, l => l.Status = LocationStatus.Closed);

        // Act
        var second = await client.ReadChangesAsync<LocationRow>(Route, first.Cursor);

        // Assert
        second.Changed.ShouldHaveSingleItem("closing a location is a change").Id.ShouldBe(MercadoId);
        second.Changed.Single().Status.ShouldBe("Closed", "the device learns the location is closed from its status");
        second.Deleted.ShouldBeEmpty("a closed location is never reported as a tombstone");
    }

    [Fact]
    [Trait("Rule", "OR82")]
    public async Task GetLocationChanges_MemberWithNoAccessAtAll_Returns403()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(MariaId, CantinaId);

        // Act
        var response = await client.GetChangesAsync(Route);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "a member with nothing granted may not list locations, so may not sync them");
    }

    [Fact]
    [Trait("Rule", "OR83")]
    public async Task GetLocationChanges_CursorOlderThanFourteenDays_Returns410CursorExpired()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var tooOld = new ChangeCursorCodec(TimeProvider.System).Encode(
            new ChangeCursor(ChangeFeeds.Locations, CantinaId, null, DateTimeOffset.UtcNow.AddDays(-14).AddMinutes(-1), 0, 0, null, null));

        // Act
        var response = await client.GetChangesAsync(Route, tooOld);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Gone, "a cursor older than 14 days is gone, so the device reloads in full");
        (await response.ReadCodeAsync()).ShouldBe("changes.cursor_expired");
    }
}
