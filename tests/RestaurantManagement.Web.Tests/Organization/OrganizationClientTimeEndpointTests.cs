using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Organization.Data;
using Shouldly;
using static RestaurantManagement.Web.Tests.ClientTimeWebSupport;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

/// <summary>An Organization write stores the client times on the changed row. Ana owns Cantina da Nonna (BRL).</summary>
public class OrganizationClientTimeEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");

    private readonly PostgresDatabaseFixture _postgres;

    public OrganizationClientTimeEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    // Outcome 8
    [Fact]
    [Trait("Rule", "OR78")]
    public async Task EditOrganization_SignedInWriteTwoHoursOld_StoresTheTimesOnTheChangedRow()
    {
        // Arrange
        await using var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId));
        var client = factory.ClientFor(AnaId, CantinaId);
        var version = await client.ReadVersionAsync("/api/v1/organizations", CantinaId);
        var body = new { name = "Cantina da Nonna Centro", serviceChargeRate = 10m, discountApprovalLimit = 15m, businessDayCutoff = "05:30", version };
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/organizations/edit") { Content = JsonContent.Create(body) };
        var now = DateTimeOffset.UtcNow;
        request.Headers.Add(RestaurantManagement.Shared.Services.ClientTime.ClientTimeHeaders.ActionTime, Iso(now.AddHours(-2)));
        request.Headers.Add(RestaurantManagement.Shared.Services.ClientTime.ClientTimeHeaders.SentAt, Iso(now));

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var organization = await factory.InOrganizationAsync<OrganizationDbContext, RestaurantManagement.Modules.Organization.Entities.Organization>(CantinaId,
            db => db.Organizations.AsNoTracking().SingleAsync(o => o.Id == CantinaId));
        organization.CorrectedActionTime.ShouldNotBeNull("the edit stores the corrected action time").ShouldBe(now.UtcDateTime.AddHours(-2), TimeSpan.FromMinutes(1), "it is when the user acted");
        organization.ClockError.ShouldNotBeNull("the edit stores the clock error");
        organization.ReceivedAt.ShouldNotBeNull("the edit stores the receive time");
    }
}
