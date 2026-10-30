using System.Net;
using System.Net.Http.Json;
using RestaurantManagement.Modules.Organization.Entities;
using RestaurantManagement.Shared;
using RestaurantManagement.Shared.Services.AuditLog;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// End to end on a real PostgreSQL database (plan T3 and T4): a settings edit or a location close made
/// through the API shows up in GET api/v1/audit-log for the same organization.
/// Ana is Owner of both Cantina da Nonna and Trattoria do Sol.
/// </summary>
public class AuditLogRecordingEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string AuditLogUrl = "/api/v1/audit-log";

    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid TrattoriaId = Guid.Parse("b2b2b2b2-0000-4000-8000-000000000002");
    private static readonly Guid CantinaBatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");

    private static object OrganizationBodyAt(uint version) => new
    {
        name = "Cantina da Nonna",
        description = "Cozinha italiana de família",
        serviceChargeRate = 12m,
        discountApprovalLimit = 5m,
        businessDayCutoff = "04:00",
        version
    };

    private readonly PostgresDatabaseFixture _postgres;

    public AuditLogRecordingEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private sealed record EntryRow(Guid Id, Guid? LocationId, string Action, string EntityType, Guid EntityId, string? Before, string? After);

    private sealed record AuditPage(List<EntryRow> Items);

    private async Task<TenantApiFactory> CreateFactoryAsync()
    {
        var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana",
            TenantApiFactory.OwnerOf(CantinaId),
            TenantApiFactory.OwnerOf(TrattoriaId));
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedOrganizationAsync(TrattoriaId, "Trattoria do Sol", Currency.BRL);
        await factory.SeedLocationAsync(CantinaId, CantinaBatelId, "Unidade Batel", "BATEL", "America/Sao_Paulo");
        return factory;
    }

    private static async Task<List<EntryRow>> ReadAuditLogAsync(HttpClient client)
    {
        var response = await client.GetAsync(AuditLogUrl);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuditPage>())!.Items;
    }

    // Scenario 20
    [Fact]
    [Trait("Rule", "SH13")]
    public async Task EditOrganization_ServiceChargeRateChanged_EntryVisibleOnlyToSameOrganization()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var cantinaClient = factory.ClientFor(AnaId, CantinaId);
        var trattoriaClient = factory.ClientFor(AnaId, TrattoriaId);

        var version = await cantinaClient.ReadVersionAsync("/api/v1/organizations", CantinaId);

        // Act
        var edit = await cantinaClient.PutAsJsonAsync("/api/v1/organizations/edit", OrganizationBodyAt(version));

        // Assert
        edit.StatusCode.ShouldBe(HttpStatusCode.OK);
        var entry = (await ReadAuditLogAsync(cantinaClient)).ShouldHaveSingleItem();
        entry.Action.ShouldBe(AuditActions.SettingsChanged);
        entry.EntityType.ShouldBe("OrganizationSettings");
        (await ReadAuditLogAsync(trattoriaClient)).ShouldBeEmpty();
    }

    // Scenario 25
    [Fact]
    [Trait("Rule", "SH16")]
    public async Task DeleteLocation_ActiveLocation_EntryShowsActiveToClosed()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);

        var version = await client.ReadVersionAsync("/api/v1/locations", CantinaBatelId);

        // Act
        var delete = await client.DeleteAtVersionAsync($"/api/v1/locations/{CantinaBatelId}", version);

        // Assert
        delete.IsSuccessStatusCode.ShouldBeTrue();
        var entry = (await ReadAuditLogAsync(client)).ShouldHaveSingleItem();
        entry.Action.ShouldBe(AuditActions.LocationStatusChanged);
        entry.LocationId.ShouldBe(CantinaBatelId);
        entry.EntityId.ShouldBe(CantinaBatelId);
        entry.Before.ShouldBe("Active");
        entry.After.ShouldBe("Closed");
    }
}
