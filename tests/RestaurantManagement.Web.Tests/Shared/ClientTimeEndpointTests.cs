using System.Net;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Menu.Data;
using RestaurantManagement.Shared.Services.ClientTime;
using Shouldly;
using static RestaurantManagement.Web.Tests.ClientTimeWebSupport;
using static RestaurantManagement.Web.Tests.LocalizedResponses;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The device-time headers on a signed-in write, run through the menu create endpoint on a real PostgreSQL
/// database. Ana is Owner of Cantina da Nonna (Batel). Test clients send both headers with the current time
/// by default; a test sets its own per request or removes them.
/// </summary>
public class ClientTimeEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string CreateUrl = "/api/v1/menus/create";
    private const string DinnerJson = """{ "id": "55555555-0000-4000-8000-000000000001", "name": "Jantar" }""";
    private const string CantinaText = "a1a1a1a1-0000-4000-8000-000000000001";

    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid CantinaId = Guid.Parse(CantinaText);
    private static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");
    private static readonly Guid Key = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e11");

    private readonly PostgresDatabaseFixture _postgres;

    public ClientTimeEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private async Task<TenantApiFactory> CreateFactoryAsync()
    {
        var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedLocationAsync(CantinaId, BatelId, "Unidade Batel", "BATEL", "America/Sao_Paulo");
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId));
        return factory;
    }

    private static Task<int> CountMenusAsync(TenantApiFactory factory)
        => factory.InOrganizationAsync<MenuDbContext, int>(CantinaId, db => db.Menus.CountAsync());

    // Outcome 6: failure first
    [Theory]
    [InlineData(ClientTimeHeaders.ActionTime, "POST", CreateUrl)]
    [InlineData(ClientTimeHeaders.SentAt, "POST", CreateUrl)]
    [InlineData(ClientTimeHeaders.ActionTime, "PUT", "/api/v1/organizations/edit")]
    [InlineData(ClientTimeHeaders.SentAt, "PUT", "/api/v1/organizations/edit")]
    [Trait("Rule", "SH125")]
    public async Task CreateMenu_ClientTimeHeaderMissing_ReturnsBadRequestAndRunsNothing(string missingHeader, string method, string url)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        client.DefaultRequestHeaders.Remove(missingHeader);

        // Act
        var request = new HttpRequestMessage(new HttpMethod(method), url) { Content = new StringContent(DinnerJson, System.Text.Encoding.UTF8, "application/json") };
        var response = await client.SendAsync(request);

        // Assert
        (await CodeAsync(response)).ShouldBe("client_time.required", "the refusal carries the required code");
        (await CountMenusAsync(factory)).ShouldBe(0, "nothing runs when a device time is missing");
    }

    // Outcome 6
    [Theory]
    [InlineData(ClientTimeHeaders.ActionTime, "not-a-time")]
    [InlineData(ClientTimeHeaders.ActionTime, "2026-11-02T10:00:00")]
    [InlineData(ClientTimeHeaders.SentAt, "not-a-time")]
    [Trait("Rule", "SH126")]
    public async Task CreateMenu_ClientTimeUnreadableOrWithoutOffset_ReturnsBadRequestAndRunsNothing(string header, string value)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var now = Iso(DateTimeOffset.UtcNow);
        var request = Post(CreateUrl, DinnerJson, now, now);
        request.Headers.Remove(header);
        request.Headers.Add(header, value);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "an unreadable device time is refused");
        (await CodeAsync(response)).ShouldBe("client_time.invalid", "the refusal carries the invalid code");
        (await CountMenusAsync(factory)).ShouldBe(0, "nothing runs when a device time is unreadable");
    }

    // Outcomes 4 and 5
    [Theory]
    [InlineData(6, "client_time.in_future")]
    [InlineData(-10081, "client_time.too_old")]
    [Trait("Rule", "SH127")]
    public async Task CreateMenu_CorrectedTimeOutsideTheBounds_ReturnsUnprocessableAndStoresNothing(int minutesFromNow, string expectedCode)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var now = DateTimeOffset.UtcNow;

        // Act
        var response = await client.SendAsync(Post(CreateUrl, DinnerJson, Iso(now.AddMinutes(minutesFromNow)), Iso(now)));

        // Assert
        ((int)response.StatusCode).ShouldBe(422, "6 minutes ahead or older than 7 days is refused");
        (await CodeAsync(response)).ShouldBe(expectedCode, "the refusal says which bound was broken");
        (await CountMenusAsync(factory)).ShouldBe(0, "a refused write stores nothing");
    }

    // Outcomes 4 and 5: edge
    [Theory]
    [InlineData(4)]
    [InlineData(-10079)]
    [Trait("Rule", "SH128")]
    public async Task CreateMenu_CorrectedTimeJustInsideTheBounds_IsAccepted(int minutesFromNow)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var now = DateTimeOffset.UtcNow;

        // Act
        var response = await client.SendAsync(Post(CreateUrl, DinnerJson, Iso(now.AddMinutes(minutesFromNow)), Iso(now)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "4 minutes ahead and just under 7 days old are accepted");
        (await CountMenusAsync(factory)).ShouldBe(1, "an accepted write runs");
        var stored = await factory.InOrganizationAsync<MenuDbContext, RestaurantManagement.Modules.Menu.Entities.Menu>(CantinaId,
            db => db.Menus.AsNoTracking().SingleAsync());
        stored.CorrectedActionTime.ShouldNotBeNull("an accepted write stores the corrected action time")
            .ShouldBe(now.UtcDateTime.AddMinutes(minutesFromNow), TimeSpan.FromMinutes(1), "it is the time the user acted");
    }

    // Guard: requests the filter does not cover behave as today
    [Theory]
    [InlineData("POST", "/api/v1/auth/login", "{}", 400)]
    [InlineData("GET", "/api/v1/menu-categories", null, 200)]
    [Trait("Rule", "SH129")]
    public async Task Request_AnonymousWriteOrRead_IsAnsweredWithoutDeviceTimes(string method, string url, string? json, int expectedStatus)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        client.DefaultRequestHeaders.Remove(ClientTimeHeaders.ActionTime);
        client.DefaultRequestHeaders.Remove(ClientTimeHeaders.SentAt);
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (json is not null)
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        ((int)response.StatusCode).ShouldBe(expectedStatus, "an anonymous write and a read are answered as before");
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("client_time", Case.Insensitive, "the device-time rules do not apply to this request");
    }

    // SH104 ordering: a refused write frees its key
    [Fact]
    [Trait("Rule", "SH130")]
    public async Task CreateMenu_RefusedForItsTimeThenCorrectedWithTheSameKey_RunsTheCorrectedRequest()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var now = DateTimeOffset.UtcNow;
        var refused = await client.SendAsync(Post(CreateUrl, DinnerJson, Iso(now.AddMinutes(-10081)), Iso(now), Key));
        refused.StatusCode.ShouldBe((HttpStatusCode)422);

        // Act
        var corrected = await client.SendAsync(PostActedAgo(CreateUrl, DinnerJson, TimeSpan.FromHours(1), Key));

        // Assert
        corrected.StatusCode.ShouldBe(HttpStatusCode.OK, "a write refused for its time frees its key, so the corrected request runs");
        (await CountMenusAsync(factory)).ShouldBe(1, "the corrected request ran once");
    }

    // Outcome 7: the key lookup answers first
    [Fact]
    [Trait("Rule", "SH131")]
    public async Task CreateMenu_ReplayedWithTheSameKeyAndUnreadableTimes_GetsTheFirstAnswerUntouched()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var first = await client.SendAsync(PostActedAgo(CreateUrl, DinnerJson, TimeSpan.FromHours(1), Key));
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var before = await factory.InOrganizationAsync<MenuDbContext, RestaurantManagement.Modules.Menu.Entities.Menu>(CantinaId,
            db => db.Menus.AsNoTracking().SingleAsync());
        before.ReceivedAt.ShouldNotBeNull("the first write stored its times");

        // Act
        var replay = await client.SendAsync(Post(CreateUrl, DinnerJson, "not-a-time", "not-a-time", Key));

        // Assert
        replay.StatusCode.ShouldBe(HttpStatusCode.OK, "a replay is answered from the saved response before the time check");
        (await replay.Content.ReadAsStringAsync()).ShouldBe(await first.Content.ReadAsStringAsync(), "the replay returns the first answer");
        (await CountMenusAsync(factory)).ShouldBe(1, "the write ran once");
        var after = await factory.InOrganizationAsync<MenuDbContext, RestaurantManagement.Modules.Menu.Entities.Menu>(CantinaId,
            db => db.Menus.AsNoTracking().SingleAsync());
        after.ReceivedAt.ShouldBe(before.ReceivedAt, "the stored times are unchanged by the replay");
    }
}
