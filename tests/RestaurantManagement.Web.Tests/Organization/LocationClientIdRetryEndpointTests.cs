using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Organization.Data;
using RestaurantManagement.Modules.Organization.Entities;
using RestaurantManagement.Shared.Auth;
using Shouldly;
using static RestaurantManagement.Web.Tests.LocalizedResponses;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// A retried <c>POST api/v1/locations</c> (same ID, same code) after the first attempt was stored.
/// Ana is the Owner of Cantina da Nonna.
/// </summary>
public class LocationClientIdRetryEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid RetriedLocationId = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e77");

    private readonly PostgresDatabaseFixture _postgres;

    public LocationClientIdRetryEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    [Fact]
    [Trait("Rule", "OR59")]
    public async Task CreateLocation_RetriedWithTheSameIdAndCode_ReturnsIdConflictAndCreatesNothingNew()
    {
        // Arrange
        await using var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId));
        var client = factory.ClientFor(AnaId, CantinaId);
        var body = new
        {
            id = RetriedLocationId,
            name = "Unidade Moema",
            address = "Rua Augusta, 1200",
            city = "São Paulo",
            state = "SP",
            postalCode = "01304-001",
            country = "Brasil",
            phoneNumber = "+55 11 3000-1000",
            status = "Active",
            timeZone = "America/Sao_Paulo",
            code = "MOE"
        };
        (await client.PostAsJsonAsync("/api/v1/locations", body)).StatusCode.ShouldBe(HttpStatusCode.OK, "the first attempt is stored");

        // Act
        var retry = await client.PostAsJsonAsync("/api/v1/locations", body);

        // Assert
        retry.StatusCode.ShouldBe(HttpStatusCode.Conflict, "the retry is a reused ID (409), not a taken-code error (400)");
        (await CodeAsync(retry)).ShouldBe("id.already_used", "the refusal carries the reused-ID code");
        var count = await factory.InOrganizationAsync<OrganizationDbContext, int>(CantinaId, db => db.Locations.CountAsync());
        count.ShouldBe(1, "the retry creates no second location");
    }
}
