using System.Net;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Organization.Data;
using Shouldly;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The change stamp the Organization database puts on locations, read straight from PostgreSQL (a database trigger
/// does this, so it cannot be proven on SQLite). Locations are never deleted: closing one is a stamped change.
/// </summary>
public class LocationChangeStampTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");

    private readonly PostgresDatabaseFixture _postgres;

    public LocationChangeStampTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private sealed record LocationRow(string Status);

    [Fact]
    [Trait("Rule", "ORD7")]
    public async Task CloseLocation_NewLocation_IsStampedOnCreateAndStampedAgainOnCloseAndTheRowStays()
    {
        // Arrange
        await using var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedLocationAsync(CantinaId, BatelId, "Unidade Batel", "BATEL", "America/Sao_Paulo");
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId));
        var client = factory.ClientFor(AnaId, CantinaId);
        var version = await client.ReadVersionAsync("/api/v1/locations", BatelId);
        var stampAfterCreate = await ReadStampAsync(factory);

        // Act
        var response = await client.DeleteAtVersionAsync($"/api/v1/locations/{BatelId}", version);
        var stampAfterClose = await ReadStampAsync(factory);

        // Assert
        stampAfterCreate.ShouldBeGreaterThan(0, "a new location is stamped by the database with its transaction");
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, "an Owner may close a location");
        stampAfterClose.ShouldBeGreaterThan(stampAfterCreate, "closing is a stamped change, so devices see it");
        var rows = await factory.InOrganizationAsync<OrganizationDbContext, List<string>>(CantinaId, db =>
            db.Locations.Where(l => l.Id == BatelId).Select(l => l.Status.ToString()).ToListAsync());
        rows.ShouldBe(new[] { "Closed" }, ignoreOrder: false, customMessage: "a closed location stays as a row, it is never deleted");
    }

#pragma warning disable EF1002 // fixed SQL from this test, no input
    private static Task<long> ReadStampAsync(TenantApiFactory factory)
        => factory.InOrganizationAsync<OrganizationDbContext, long>(CantinaId, async db =>
            await db.Database.SqlQueryRaw<long>("SELECT change_stamp AS \"Value\" FROM locations LIMIT 1").SingleAsync());
#pragma warning restore EF1002
}
