using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Menu.Data;
using RestaurantManagement.Shared.Auth;
using RestaurantManagement.Shared.Data.Idempotency;
using RestaurantManagement.Shared.Services.Idempotency;
using Shouldly;
using static RestaurantManagement.Web.Tests.LocalizedResponses;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The <c>Idempotency-Key</c> header on a signed-in write, run through the menu create endpoint on a real
/// PostgreSQL database (green once Menu wires its key table). Ana is Owner of Cantina da Nonna (Batel) and
/// Bruno is Owner of Trattoria do Sol (Centro); Caio is a member of Cantina with Menus Manage at Batel.
/// </summary>
public class IdempotencyKeyEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string CreateUrl = "/api/v1/menus/create";
    private const string CaioText = "33333333-cccc-4ccc-8ccc-00000000000c";
    private const string BrunoText = "88888888-0000-4000-8000-0000000000b1";
    private const string CantinaText = "a1a1a1a1-0000-4000-8000-000000000001";
    private const string TrattoriaText = "b2b2b2b2-0000-4000-8000-000000000002";
    private const string DinnerJson = """{ "id": "55555555-0000-4000-8000-000000000001", "name": "Jantar" }""";
    private const string DinnerAtBatelJson = """{ "id": "55555555-0000-4000-8000-000000000001", "name": "Jantar", "locationIds": ["44444444-dddd-4ddd-8ddd-000000000001"] }""";

    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid BrunoId = Guid.Parse(BrunoText);
    private static readonly Guid CaioId = Guid.Parse(CaioText);
    private static readonly Guid CantinaId = Guid.Parse(CantinaText);
    private static readonly Guid TrattoriaId = Guid.Parse(TrattoriaText);
    private static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");
    private static readonly Guid CentroId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000003");
    private static readonly Guid DinnerMenuId = Guid.Parse("55555555-0000-4000-8000-000000000001");
    private static readonly Guid LunchMenuId = Guid.Parse("55555555-0000-4000-8000-000000000002");
    private static readonly Guid Key = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e11");

    private readonly PostgresDatabaseFixture _postgres;

    public IdempotencyKeyEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private async Task<TenantApiFactory> CreateFactoryAsync()
    {
        var factory = await TenantApiFactory.CreateAsync(_postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedOrganizationAsync(TrattoriaId, "Trattoria do Sol", Currency.BRL);
        await factory.SeedLocationAsync(CantinaId, BatelId, "Unidade Batel", "BATEL", "America/Sao_Paulo");
        await factory.SeedLocationAsync(TrattoriaId, CentroId, "Unidade Centro", "CENTRO", "America/Sao_Paulo");
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId));
        await factory.SeedUserAsync(BrunoId, "bruno@trattoriadosol.com.br", "Bruno", TenantApiFactory.OwnerOf(TrattoriaId));
        await factory.SeedUserAsync(CaioId, "caio@cantinadanonna.com.br", "Caio", TenantApiFactory.MemberOf(CantinaId));
        await factory.GrantAsync(CaioId, CantinaId, BatelId, (AccessModule.Locations, AccessLevel.Read), (AccessModule.Menus, AccessLevel.Manage));
        return factory;
    }

    private static HttpRequestMessage Post(string url, string? key, string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        if (key is not null)
            request.Headers.Add(IdempotencyKeyFilter.HeaderName, key);
        return request;
    }

    private static Task<int> CountMenusAsync(TenantApiFactory factory, Guid organizationId)
        => factory.InOrganizationAsync<MenuDbContext, int>(organizationId, db => db.Menus.CountAsync());

    private static Task<List<IdempotencyKey>> KeyRowsAsync(TenantApiFactory factory, Guid organizationId)
        => factory.InOrganizationAsync<MenuDbContext, List<IdempotencyKey>>(organizationId, db => db.Set<IdempotencyKey>().ToListAsync());

    // Outcome 6: failure first
    [Theory]
    [InlineData("not-a-uuid")]
    [InlineData("12345678")]
    [InlineData("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e1z")]
    [Trait("Rule", "SH103")]
    public async Task CreateMenu_KeyThatIsNotAUuid_ReturnsBadRequestAndRunsNothing(string key)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.SendAsync(Post(CreateUrl, key, DinnerJson));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a key that is not a UUID is refused");
        (await CodeAsync(response)).ShouldBe("idempotency_key.invalid", "the refusal carries the invalid-key code");
        (await CountMenusAsync(factory, CantinaId)).ShouldBe(0, "nothing runs when the key is invalid");
    }

    // Outcome 2
    [Theory]
    [InlineData(CreateUrl, """{ "id": "55555555-0000-4000-8000-000000000001", "name": "Almoço executivo" }""")]
    [InlineData(CreateUrl + "?source=tablet", DinnerJson)]
    [Trait("Rule", "SH100")]
    public async Task CreateMenu_SameKeyWithAnotherBodyOrTarget_ReturnsUnprocessableAndChangesNothing(string url, string json)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        (await client.SendAsync(Post(CreateUrl, Key.ToString(), DinnerJson))).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act
        var response = await client.SendAsync(Post(url, Key.ToString(), json));

        // Assert
        response.StatusCode.ShouldBe((HttpStatusCode)422, "a reused key with another body or another target is refused");
        (await CodeAsync(response)).ShouldBe("idempotency_key.reused", "the refusal carries the reused-key code");
        var menu = await factory.InOrganizationAsync<MenuDbContext, string>(CantinaId, async db => (await db.Menus.SingleAsync()).Name);
        menu.ShouldBe("Jantar", "the refused request changed nothing");
    }

    // Outcome 3
    [Fact]
    [Trait("Rule", "SH101")]
    public async Task CreateMenu_SameKeyWhileTheFirstRequestIsStillRunning_ReturnsConflictAndRunsNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var hash = IdempotencyRequestHash.Compute("POST", CreateUrl, null, Encoding.UTF8.GetBytes(DinnerJson));
        await factory.InOrganizationAsync<MenuDbContext, bool>(CantinaId, async db =>
        {
            db.Set<IdempotencyKey>().Add(new IdempotencyKey
            {
                Id = Guid.NewGuid(), UserId = AnaId, OrganizationId = CantinaId, Key = Key, RequestHash = hash,
                State = IdempotencyKeyState.InProgress, CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            return true;
        });
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.SendAsync(Post(CreateUrl, Key.ToString(), DinnerJson));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, "a key whose first request is still running is refused with 409");
        (await CodeAsync(response)).ShouldBe("request.in_progress", "the refusal carries the retry code");
        (await CountMenusAsync(factory, CantinaId)).ShouldBe(0, "the second request does not run");
    }

    // Outcome 8
    [Fact]
    [Trait("Rule", "SH105")]
    public async Task CreateMenu_FailureWhileSavingTheKey_StoresNoMenuEither()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        await factory.InOrganizationAsync<MenuDbContext, int>(CantinaId, db => db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION fail_key_completion() RETURNS trigger AS $$ BEGIN RAISE EXCEPTION 'forced failure while saving the key'; END; $$ LANGUAGE plpgsql;
            CREATE TRIGGER fail_key_completion BEFORE UPDATE ON menu_idempotency_keys FOR EACH ROW EXECUTE FUNCTION fail_key_completion();
            """));
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.SendAsync(Post(CreateUrl, Key.ToString(), DinnerAtBatelJson));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError, "the failed key save fails the request");
        (await CountMenusAsync(factory, CantinaId)).ShouldBe(0, "the write and its key are saved together or not at all: no menu without its key");
        (await factory.InOrganizationAsync<MenuDbContext, int>(CantinaId, db => db.LocationMenus.CountAsync())).ShouldBe(0, "no location link is left either");
    }

    // Outcome 4
    [Theory]
    [InlineData(CaioText, CantinaText, 2)]
    [InlineData(BrunoText, TrattoriaText, 1)]
    [Trait("Rule", "SH102")]
    public async Task CreateMenu_SameKeyFromAnotherUserOrOrganization_RunsTheirOwnWriteAndNeverReturnsTheFirstAnswer(string userText, string organizationText, int menusInCantina)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var userId = Guid.Parse(userText);
        var organizationId = Guid.Parse(organizationText);
        var firstResponse = await factory.ClientFor(AnaId, CantinaId).SendAsync(Post(CreateUrl, Key.ToString(), DinnerJson));
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act
        var response = await factory.ClientFor(userId, organizationId).SendAsync(Post(CreateUrl, Key.ToString(),
            """{ "id": "55555555-0000-4000-8000-000000000002", "name": "Pranzo" }"""));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "another user's write runs on its own");
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Pranzo", Case.Sensitive, "the answer is the second user's own menu");
        body.ShouldNotContain("Jantar", Case.Sensitive, "the first user's answer is never returned to anyone else");
        (await factory.InOrganizationAsync<MenuDbContext, bool>(organizationId, db => db.Menus.AnyAsync(m => m.Id == LunchMenuId))).ShouldBeTrue("the second menu was created");
        (await CountMenusAsync(factory, CantinaId)).ShouldBe(menusInCantina, "the first user's menu is kept and nothing else is added to the first organization");
    }

    // Outcome 7
    [Fact]
    [Trait("Rule", "SH104")]
    public async Task CreateMenu_FirstAttemptFailsValidation_CorrectedRequestWithTheSameKeyRuns()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var failed = await client.SendAsync(Post(CreateUrl, Key.ToString(), """{ "id": "55555555-0000-4000-8000-000000000001" }"""));
        failed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Act
        var corrected = await client.SendAsync(Post(CreateUrl, Key.ToString(), DinnerJson));

        // Assert
        corrected.StatusCode.ShouldBe(HttpStatusCode.OK, "a failed first attempt saves nothing, so the corrected request runs");
        (await CountMenusAsync(factory, CantinaId)).ShouldBe(1, "the corrected request created the menu");
    }

    // Outcome 9
    [Fact]
    [Trait("Rule", "SH106")]
    public async Task CreateMenu_KeyOlderThanTheRetentionTime_RunsAsNewAndReplacesTheOldRow()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        await factory.InOrganizationAsync<MenuDbContext, bool>(CantinaId, async db =>
        {
            db.Set<IdempotencyKey>().Add(new IdempotencyKey
            {
                Id = Guid.NewGuid(), UserId = AnaId, OrganizationId = CantinaId, Key = Key, RequestHash = "hash-of-an-old-request",
                State = IdempotencyKeyState.Completed, ResponseStatus = 200, ResponseContentType = "application/json",
                ResponseBody = Encoding.UTF8.GetBytes("""{ "name": "Cardápio de ontem" }"""), CreatedAt = DateTime.UtcNow.AddDays(-9)
            });
            await db.SaveChangesAsync();
            return true;
        });
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.SendAsync(Post(CreateUrl, Key.ToString(), DinnerJson));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "a key older than 8 days counts as new");
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("Cardápio de ontem", Case.Sensitive, "the expired answer is never replayed");
        var rows = await KeyRowsAsync(factory, CantinaId);
        var row = rows.ShouldHaveSingleItem("the expired row is replaced, not kept next to the new one");
        row.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-5), "the stored row is the new one");
    }

    // Outcome 1
    [Fact]
    [Trait("Rule", "SH99")]
    public async Task CreateMenu_SameKeyAndSameRequestTwice_RunsOnceAndAnswersTheSameBothTimes()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var first = await client.SendAsync(Post(CreateUrl, Key.ToString(), DinnerAtBatelJson));

        // Act
        var second = await client.SendAsync(Post(CreateUrl, Key.ToString(), DinnerAtBatelJson));

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(first.StatusCode, "the replay keeps the status");
        (await second.Content.ReadAsStringAsync()).ShouldBe(await first.Content.ReadAsStringAsync(), "the replay returns the saved body");
        (await CountMenusAsync(factory, CantinaId)).ShouldBe(1, "the write ran once");
        (await factory.InOrganizationAsync<MenuDbContext, int>(CantinaId, db => db.LocationMenus.CountAsync())).ShouldBe(1, "its location link exists once");
        (await KeyRowsAsync(factory, CantinaId)).ShouldHaveSingleItem("one key row is kept");
    }

    // Guard: requests the filter does not cover behave as today
    [Theory]
    [InlineData("POST", "/api/v1/auth/login", "{}", "not-a-uuid", 400)]
    [InlineData("GET", "/api/v1/menu-categories", null, "not-a-uuid", 200)]
    [InlineData("POST", CreateUrl, DinnerJson, null, 200)]
    [Trait("Rule", "SH107")]
    public async Task Request_AnonymousWriteReadOrWriteWithNoKey_RunsAsToday(string method, string url, string? json, string? key, int expectedStatus)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync();
        var client = factory.ClientFor(AnaId, CantinaId);
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (json is not null)
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        if (key is not null)
            request.Headers.Add(IdempotencyKeyFilter.HeaderName, key);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        ((int)response.StatusCode).ShouldBe(expectedStatus, "an anonymous write, a read, or a write with no key is answered as before");
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("idempotency_key", Case.Insensitive, "the key rules do not apply to this request");
    }
}
