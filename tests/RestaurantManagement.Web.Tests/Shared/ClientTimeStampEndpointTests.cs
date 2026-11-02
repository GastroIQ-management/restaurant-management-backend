using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using RestaurantManagement.Modules.Menu.Data;
using RestaurantManagement.Shared.Data.AuditLog;
using Shouldly;
using static RestaurantManagement.Web.Tests.ClientTimeWebSupport;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// What a signed-in write stores: the device action time, the corrected action time, the clock error and the
/// server receive time, on a real PostgreSQL database. Ana is Owner of Cantina da Nonna (Batel).
/// </summary>
public class ClientTimeStampEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string CreateUrl = "/api/v1/menus/create";
    private const string DinnerJson = """{ "id": "55555555-0000-4000-8000-000000000001", "name": "Jantar" }""";

    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(1);
    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");
    private static readonly Guid DinnerMenuId = Guid.Parse("55555555-0000-4000-8000-000000000001");
    private static readonly Guid Key = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e11");

    private readonly PostgresDatabaseFixture _postgres;

    public ClientTimeStampEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private async Task<TenantApiFactory> CreateFactoryAsync()
    {
        var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedLocationAsync(CantinaId, BatelId, "Unidade Batel", "BATEL", "America/Sao_Paulo");
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId));
        return factory;
    }

    private static Task<RestaurantManagement.Modules.Menu.Entities.Menu> ReadMenuAsync(TenantApiFactory factory)
        => factory.InOrganizationAsync<MenuDbContext, RestaurantManagement.Modules.Menu.Entities.Menu>(CantinaId,
            db => db.Menus.AsNoTracking().SingleAsync(m => m.Id == DinnerMenuId));

    // Outcome 1
    [Fact]
    [Trait("Rule", "SH132")]
    public async Task CreateMenu_ActionFiveHoursOld_StoresTheActionTimeAndALaterReceivedTime()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var expectedAction = DateTime.UtcNow.AddHours(-5);

        // Act
        var response = await client.SendAsync(PostActedAgo(CreateUrl, DinnerJson, TimeSpan.FromHours(5)));

        // Assert
        response.EnsureSuccessStatusCode();
        var menu = await ReadMenuAsync(factory);
        menu.CorrectedActionTime.ShouldNotBeNull("the corrected action time is stored").ShouldBe(expectedAction, Tolerance, "it is the moment the user acted, 5 hours ago");
        menu.ReceivedAt.ShouldNotBeNull("the server receive time is stored").ShouldBeGreaterThan(menu.CorrectedActionTime!.Value, "the write was received after the action");
        menu.ReceivedAt.Value.ShouldBe(DateTime.UtcNow, Tolerance, "the receive time is when the server got the write");
    }

    // Outcome 2
    [Fact]
    [Trait("Rule", "SH133")]
    public async Task CreateMenu_DeviceClockTenMinutesFast_StoresTheCorrectedTimeAndTheError()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var deviceNow = DateTimeOffset.UtcNow.AddMinutes(10);
        var request = Post(CreateUrl, DinnerJson, Iso(deviceNow.AddHours(-5)), Iso(deviceNow));

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var menu = await ReadMenuAsync(factory);
        menu.ClockError.ShouldNotBeNull("the clock error is stored").ShouldBe(TimeSpan.FromMinutes(10), Tolerance, "the device clock was 10 minutes fast");
        (menu.ClientActionTime!.Value - menu.CorrectedActionTime!.Value).ShouldBe(menu.ClockError.Value, TimeSpan.FromSeconds(1),
            "the corrected time is the device time minus the error");
        menu.ClientActionOffsetMinutes.ShouldBe(0, "the offset the device sent is stored");
    }

    // Outcome 7
    [Fact]
    [Trait("Rule", "SH134")]
    public async Task CreateMenu_SameKeyReplayedLaterWithOtherTimes_LeavesTheStoredTimesUnchanged()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        (await client.SendAsync(PostActedAgo(CreateUrl, DinnerJson, TimeSpan.FromHours(5), Key))).EnsureSuccessStatusCode();
        var before = await ReadMenuAsync(factory);

        // Act
        var replay = await client.SendAsync(PostActedAgo(CreateUrl, DinnerJson, TimeSpan.FromHours(1), Key));

        // Assert
        replay.EnsureSuccessStatusCode();
        var after = await ReadMenuAsync(factory);
        after.CorrectedActionTime.ShouldNotBeNull("the first times were stored").ShouldBe(before.CorrectedActionTime!.Value, "a replay never changes the stored action time");
        after.ReceivedAt.ShouldBe(before.ReceivedAt, "a replay never changes the stored receive time");
        after.ClockError.ShouldBe(before.ClockError, "a replay never changes the stored clock error");
    }

    // Audit log rows are stamped like every other row
    [Fact]
    [Trait("Rule", "SH135")]
    public async Task EditOrganization_AuditEntryWritten_CarriesTheTimesOfTheWrite()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var version = await client.ReadVersionAsync("/api/v1/organizations", CantinaId);
        var body = new { name = "Cantina da Nonna Centro", serviceChargeRate = 10m, discountApprovalLimit = 15m, businessDayCutoff = "05:30", version };

        // Act
        (await client.PutAsJsonAsync("/api/v1/organizations/edit", body)).EnsureSuccessStatusCode();

        // Assert
        var entries = await factory.InOrganizationAsync<AuditLogDbContext, List<AuditLogEntry>>(CantinaId, db => db.AuditLogEntries.AsNoTracking().ToListAsync());
        var entry = entries.ShouldHaveSingleItem("the edit wrote one audit entry");
        entry.CorrectedActionTime.ShouldNotBeNull("audit log rows are stamped like every other row");
        entry.ReceivedAt.ShouldNotBeNull("audit log rows carry the receive time");
    }
}
