using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Net.Http.Headers;
using RestaurantManagement.Modules.Identity.Data;
using RestaurantManagement.Modules.Identity.Entities.Members;
using RestaurantManagement.Modules.Identity.Entities.Sessions;
using RestaurantManagement.Modules.Identity.Entities.Users;
using RestaurantManagement.Modules.Identity.Services.Sessions;
using RestaurantManagement.Shared.Services.ClientTime;
using Shouldly;
using static RestaurantManagement.Web.Tests.LocalizedResponses;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The native app session end to end on a real PostgreSQL database with the real session service: the refresh token
/// travels in the X-Refresh-Token header and the response body, from the native app origin. Maria (Owner of Cantina
/// da Nonna) and João (Owner of Trattoria do Sol) sign in on phones; Maria also has a browser session.
/// </summary>
public class NativeAppSessionEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string NativeOrigin = "https://localhost";
    private const string TokenHeader = "X-Refresh-Token";
    private const string CookieName = "__Secure-refresh_token";
    private const string RefreshUrl = "/api/v1/auth/refresh";
    private const string SelectOrganizationUrl = "/api/v1/auth/select-organization";
    private const string LogoutUrl = "/api/v1/auth/logout";
    private const string LogoutAllUrl = "/api/v1/auth/logout-all";
    private const string ChangePasswordUrl = "/api/v1/auth/change-password";
    private const string MariaPassword = "Massa@Fresca2026";
    private const string NewPassword = "Nhoque@Batata2027";
    private const string PhoneAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) Mobile/15E148";
    private const string BrowserAgent = "Mozilla/5.0 (Macintosh) Safari/605.1.15";
    private const string UnknownOrganization = "ffffffff-0000-4000-8000-000000000009";
    private const string TrattoriaText = "a2a2a2a2-0000-4000-8000-000000000002";

    private static readonly Guid MariaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid JoaoId = Guid.Parse("22222222-bbbb-4bbb-8bbb-00000000000b");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid TrattoriaId = Guid.Parse(TrattoriaText);

    private readonly PostgresDatabaseFixture _postgres;

    public NativeAppSessionEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private async Task<(WebApplicationFactory<Program> App, TenantApiFactory Tenant)> StartAsync()
    {
        var tenant = await TenantApiFactory.CreateAsync(_postgres);
        var app = tenant.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISessionService>();
            services.AddScoped<ISessionService, SessionService>();
        }));

        await tenant.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", RestaurantManagement.Modules.Organization.Entities.Currency.BRL);
        await tenant.SeedOrganizationAsync(TrattoriaId, "Trattoria do Sol", RestaurantManagement.Modules.Organization.Entities.Currency.BRL);
        await SeedUserAsync(app, tenant, MariaId, "maria@cantinadanonna.com.br", "Maria", CantinaId);
        await SeedUserAsync(app, tenant, JoaoId, "joao@trattoriadosol.com.br", "João", TrattoriaId);
        return (app, tenant);
    }

    private static async Task SeedUserAsync(WebApplicationFactory<Program> app, TenantApiFactory tenant, Guid userId, string email, string firstName, Guid organizationId)
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            db.Users.Add(new User
            {
                Id = userId,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(MariaPassword),
                FirstName = firstName,
                LastName = "Souza",
                IsVerified = true
            });
            await db.SaveChangesAsync();
        }

        await tenant.AddMembershipAsync(userId, TenantApiFactory.OwnerOf(organizationId));
    }

    private static async Task<string> IssueAsync(WebApplicationFactory<Program> app, Guid userId, string device)
    {
        using var scope = app.Services.CreateScope();
        var issued = await scope.ServiceProvider.GetRequiredService<IRefreshTokenService>().IssueAsync(userId, null, device, "203.0.113.20");
        return issued.Token;
    }

    private static async Task<RefreshToken> RowOfAsync(WebApplicationFactory<Program> app, string rawToken)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        using var scope = app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().RefreshTokens.AsNoTracking().SingleAsync(t => t.TokenHash == hash);
    }

    private static async Task<int> RowCountAsync(WebApplicationFactory<Program> app)
    {
        using var scope = app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().RefreshTokens.CountAsync();
    }

    private static HttpClient NewClient(WebApplicationFactory<Program> app)
        => app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static HttpRequestMessage NativePost(string url, string? headerToken, string? cookieToken = null, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.TryAddWithoutValidation("Origin", NativeOrigin);
        if (headerToken is not null)
            request.Headers.Add(TokenHeader, headerToken);
        if (cookieToken is not null)
            request.Headers.Add("Cookie", $"{CookieName}={cookieToken}");
        return request;
    }

    private static HttpRequestMessage SignedIn(WebApplicationFactory<Program> app, Guid userId, HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestAccessTokens.Create(app.Services, userId));
        return request;
    }

    private static Task<HttpResponseMessage> NativeRefreshAsync(WebApplicationFactory<Program> app, string? token)
        => NewClient(app).SendAsync(NativePost(RefreshUrl, token));

    private static async Task<string> NativeRotateAsync(WebApplicationFactory<Program> app, string token)
    {
        var response = await NativeRefreshAsync(app, token);
        var replacement = JsonNode.Parse(await response.Content.ReadAsStringAsync())!["refreshToken"]?.GetValue<string>();
        replacement.ShouldNotBeNullOrEmpty("a native refresh returns the replacement token in the body");
        return replacement!;
    }

    private static async Task<HttpResponseMessage> WebRefreshAsync(WebApplicationFactory<Program> app, string cookieToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, RefreshUrl);
        request.Headers.Add("Cookie", $"{CookieName}={cookieToken}");
        return await NewClient(app).SendAsync(request);
    }

    private static bool SetsRefreshCookie(HttpResponseMessage response)
        => response.Headers.TryGetValues(HeaderNames.SetCookie, out var values)
           && SetCookieHeaderValue.ParseList(values.ToList()).Any(c => c.Name == CookieName);

    private static StringContent JsonBody(string json) => new(json, Encoding.UTF8, "application/json");

    // Failure first: a rotated token sent again is a theft signal
    [Fact]
    [Trait("Rule", "F16")]
    public async Task Refresh_NativeRotatedTokenSentAgain_IsRefusedAndRevokesTheFamilyButNotOtherDevices()
    {
        // Arrange
        var (app, tenant) = await StartAsync();
        await using var _ = tenant;
        var phoneToken = await IssueAsync(app, MariaId, PhoneAgent);
        var browserToken = await IssueAsync(app, MariaId, BrowserAgent);
        var replacement = await NativeRotateAsync(app, phoneToken);

        // Act
        var reuse = await NativeRefreshAsync(app, phoneToken);

        // Assert
        reuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "a rotated token sent again is refused");
        (await RowOfAsync(app, replacement)).RevokedAt.ShouldNotBeNull("reuse revokes the whole device family, so the replacement stops working too");
        (await WebRefreshAsync(app, browserToken)).StatusCode.ShouldBe(HttpStatusCode.OK, "the user's other devices stay signed in");
    }

    // Failure first: nothing is issued without a valid header token
    [Theory]
    [InlineData(null)]
    [InlineData("dW5rbm93bi1uYXRpdmUtdG9rZW4tbm90LXN0b3JlZA")]
    [Trait("Rule", "F17")]
    public async Task Refresh_NativeHeaderMissingOrUnknown_ReturnsUnauthorizedAndIssuesNothing(string? headerToken)
    {
        // Arrange
        var (app, tenant) = await StartAsync();
        await using var _ = tenant;
        var cookieOnlyToken = await IssueAsync(app, MariaId, BrowserAgent);
        var rowsBefore = await RowCountAsync(app);
        var request = NativePost(RefreshUrl, headerToken, cookieToken: cookieOnlyToken);

        // Act
        var response = await NewClient(app).SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "no usable header token means no session to renew, whatever cookie is sent");
        (await CodeAsync(response)).ShouldBe("session.invalid_or_expired", "the refusal is the same as for the cookie flow");
        (await RowCountAsync(app)).ShouldBe(rowsBefore, "nothing is issued");
    }

    [Theory]
    [InlineData(1, -1)]
    [InlineData(8, -1440)]
    [InlineData(31, 60)]
    [Trait("Rule", "F17")]
    public async Task Refresh_NativeTokenExpiredIdleOrPastThirtyDays_ReturnsUnauthorizedAndIssuesNothing(int startedDaysAgo, int expiresInMinutes)
    {
        // Arrange
        var (app, tenant) = await StartAsync();
        await using var _ = tenant;
        var controlToken = await IssueAsync(app, MariaId, BrowserAgent);
        var token = await IssueAsync(app, MariaId, PhoneAgent);
        var tokenHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var now = DateTime.UtcNow;
            await db.RefreshTokens.Where(t => t.TokenHash == tokenHash).ExecuteUpdateAsync(s => s
                .SetProperty(t => t.SessionStartedAt, now.AddDays(-startedDaysAgo))
                .SetProperty(t => t.ExpiresAt, now.AddMinutes(expiresInMinutes)));
        }
        (await NativeRefreshAsync(app, controlToken)).StatusCode.ShouldBe(HttpStatusCode.OK, "control: a valid token of the same user refreshes through the header, so the refusal below comes from the token's state");
        var rowsBefore = await RowCountAsync(app);

        // Act
        var response = await NativeRefreshAsync(app, token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "an expired, 7-day idle or past-30-day token is refused");
        (await CodeAsync(response)).ShouldBe("session.invalid_or_expired", "the refusal is the same as for the cookie flow");
        (await RowCountAsync(app)).ShouldBe(rowsBefore, "nothing is issued");
    }

    [Fact]
    [Trait("Rule", "S15")]
    public async Task SelectOrganization_NativeHeaderHoldsAnotherUsersToken_ReturnsUnauthorizedAndLeavesTheirTokenActive()
    {
        // Arrange
        var (app, tenant) = await StartAsync();
        await using var _ = tenant;
        var mariaToken = await IssueAsync(app, MariaId, PhoneAgent);
        var joaoToken = await IssueAsync(app, JoaoId, PhoneAgent);
        var control = SignedIn(app, JoaoId, NativePost(SelectOrganizationUrl, joaoToken, content: JsonBody($$"""{"organizationId":"{{TrattoriaText}}"}""")));
        (await NewClient(app).SendAsync(control)).StatusCode.ShouldBe(HttpStatusCode.OK, "control: the user's own header token is accepted, so the refusal below comes from the owner check");
        var request = SignedIn(app, JoaoId, NativePost(SelectOrganizationUrl, mariaToken, content: JsonBody($$"""{"organizationId":"{{TrattoriaText}}"}""")));

        // Act
        var response = await NewClient(app).SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "a signed-in user cannot rotate another user's token");
        (await RowOfAsync(app, mariaToken)).RevokedAt.ShouldBeNull("the other user's token is left untouched");
    }

    [Theory]
    [InlineData(TrattoriaText)]
    [InlineData(UnknownOrganization)]
    [Trait("Rule", "S16")]
    public async Task SelectOrganization_NativeOrganizationOfAnotherTenantOrUnknown_ReturnsTheSameForbidden(string organizationId)
    {
        // Arrange
        var (app, tenant) = await StartAsync();
        await using var _ = tenant;
        var token = await IssueAsync(app, MariaId, PhoneAgent);
        var request = SignedIn(app, MariaId, NativePost(SelectOrganizationUrl, token, content: JsonBody($$"""{"organizationId":"{{organizationId}}"}""")));

        // Act
        var response = await NewClient(app).SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "an organization the user does not belong to is refused");
        (await CodeAsync(response)).ShouldBe("session.not_a_member", "another organization and an unknown one cannot be told apart");
    }

    [Fact]
    [Trait("Rule", "F15")]
    public async Task Refresh_NativeHeaderToken_RotatesInTheSameFamilyAndReturnsReplacementInBody()
    {
        // Arrange
        var (app, tenant) = await StartAsync();
        await using var _ = tenant;
        var oldToken = await IssueAsync(app, MariaId, PhoneAgent);

        // Act
        var response = await NativeRefreshAsync(app, oldToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "a valid header token renews the session");
        SetsRefreshCookie(response).ShouldBeFalse("a native app gets no cookie");
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        var newToken = body["refreshToken"]?.GetValue<string>();
        newToken.ShouldNotBeNullOrEmpty("the replacement refresh token is in the body");
        newToken.ShouldNotBe(oldToken, "the token is rotated");
        body["token"]?.GetValue<string>().ShouldNotBeNullOrEmpty("a new access token is issued");
        var oldRow = await RowOfAsync(app, oldToken);
        var newRow = await RowOfAsync(app, newToken);
        oldRow.RevokedAt.ShouldNotBeNull("the old token is revoked");
        oldRow.RevocationReason.ShouldBe(RefreshToken.ReasonRotated, "it is revoked as rotated");
        oldRow.ReplacedByTokenId.ShouldBe(newRow.Id, "it points to its replacement");
        newRow.FamilyId.ShouldBe(oldRow.FamilyId, "the replacement stays in the same device family");
        newRow.ExpiresAt.ShouldBe(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1), "the replacement expires 7 days after the refresh, the earlier of that and 30 days after login");
    }

    [Fact]
    [Trait("Rule", "S14")]
    public async Task SelectOrganization_NativeHeaderToken_RotatesTheTokenAndRefusesTheOldOne()
    {
        // Arrange
        var (app, tenant) = await StartAsync();
        await using var _ = tenant;
        var oldToken = await IssueAsync(app, MariaId, PhoneAgent);
        var request = SignedIn(app, MariaId, NativePost(SelectOrganizationUrl, oldToken, content: JsonBody($$"""{"organizationId":"{{CantinaId}}"}""")));

        // Act
        var response = await NewClient(app).SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "a member may choose the organization");
        SetsRefreshCookie(response).ShouldBeFalse("a native app gets no cookie");
        var newToken = JsonNode.Parse(await response.Content.ReadAsStringAsync())!["refreshToken"]?.GetValue<string>();
        newToken.ShouldNotBeNullOrEmpty("the replacement refresh token is in the body");
        (await NativeRefreshAsync(app, oldToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "the token from before the switch is refused");
    }

    [Theory]
    [InlineData(SelectOrganizationUrl, """{"organizationId":"a1a1a1a1-0000-4000-8000-000000000001"}""")]
    [InlineData(LogoutAllUrl, "{}")]
    [InlineData(ChangePasswordUrl, """{"currentPassword":"Massa@Fresca2026","newPassword":"Nhoque@Batata2027"}""")]
    [Trait("Rule", "S17")]
    public async Task NativeSessionAction_NoDeviceTimeHeaders_ReturnsBadRequest(string url, string json)
    {
        // Arrange
        var (app, tenant) = await StartAsync();
        await using var _ = tenant;
        var token = await IssueAsync(app, MariaId, PhoneAgent);
        var client = NewClient(app);
        client.DefaultRequestHeaders.Remove(ClientTimeHeaders.ActionTime);
        client.DefaultRequestHeaders.Remove(ClientTimeHeaders.SentAt);
        var request = SignedIn(app, MariaId, NativePost(url, token, content: JsonBody(json)));

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a native session action is a signed-in write and needs both device times");
        (await CodeAsync(response)).ShouldBe("client_time.required", "the refusal carries the required code");
    }

    [Fact]
    [Trait("Rule", "O7")]
    public async Task Logout_NativeHeaderToken_RevokesOnlyThatDeviceAndSetsNoCookie()
    {
        // Arrange
        var (app, tenant) = await StartAsync();
        await using var _ = tenant;
        var phoneToken = await IssueAsync(app, MariaId, PhoneAgent);
        var browserToken = await IssueAsync(app, MariaId, BrowserAgent);
        var joaoToken = await IssueAsync(app, JoaoId, PhoneAgent);

        // Act
        var response = await NewClient(app).SendAsync(NativePost(LogoutUrl, phoneToken));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, "logging out succeeds without a body");
        SetsRefreshCookie(response).ShouldBeFalse("a native app has no cookie to clear");
        var row = await RowOfAsync(app, phoneToken);
        row.RevokedAt.ShouldNotBeNull("that device's token is revoked");
        row.RevocationReason.ShouldBe(RefreshToken.ReasonLogout, "it is revoked as a logout");
        (await WebRefreshAsync(app, browserToken)).StatusCode.ShouldBe(HttpStatusCode.OK, "the user's other device stays signed in");
        (await RowOfAsync(app, joaoToken)).RevokedAt.ShouldBeNull("another user's token is never revoked by a logout");
    }

    [Fact]
    [Trait("Rule", "O8")]
    public async Task Logout_NativeRotatedTokenSentAgain_RevokesThatDevicesFamily()
    {
        // Arrange
        var (app, tenant) = await StartAsync();
        await using var _ = tenant;
        var oldToken = await IssueAsync(app, MariaId, PhoneAgent);
        var replacement = await NativeRotateAsync(app, oldToken);

        // Act
        var response = await NewClient(app).SendAsync(NativePost(LogoutUrl, oldToken));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, "logging out with a reused token still answers 204");
        (await RowOfAsync(app, replacement)).RevokedAt.ShouldNotBeNull("a reused token revokes that device's whole family");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("dW5rbm93bi1uYXRpdmUtdG9rZW4tbm90LXN0b3JlZA")]
    [Trait("Rule", "O9")]
    public async Task Logout_NativeHeaderMissingOrUnknown_ReturnsNoContentAndChangesNothing(string? headerToken)
    {
        // Arrange
        var (app, tenant) = await StartAsync();
        await using var _ = tenant;
        var cookieToken = await IssueAsync(app, MariaId, BrowserAgent);

        // Act
        var response = await NewClient(app).SendAsync(NativePost(LogoutUrl, headerToken, cookieToken: cookieToken));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, "logging out without a usable header token is not an error");
        SetsRefreshCookie(response).ShouldBeFalse("a native app has no cookie to clear");
        (await RowOfAsync(app, cookieToken)).RevokedAt.ShouldBeNull("a cookie sent on a native request is never read, so nothing changes");
    }

    [Fact]
    [Trait("Rule", "O11")]
    public async Task LogoutAll_NativeSignedIn_RevokesEveryTokenOfTheCallerAndSetsNoCookie()
    {
        // Arrange
        var (app, tenant) = await StartAsync();
        await using var _ = tenant;
        var phoneToken = await IssueAsync(app, MariaId, PhoneAgent);
        var tabletToken = await IssueAsync(app, MariaId, "Mozilla/5.0 (Linux; Android 14; Tablet) Chrome/124.0");
        var request = SignedIn(app, MariaId, NativePost(LogoutAllUrl, headerToken: null, content: JsonBody("{}")));

        // Act
        var response = await NewClient(app).SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, "any signed-in user may log out of all devices");
        SetsRefreshCookie(response).ShouldBeFalse("a native app has no cookie to clear");
        (await NativeRefreshAsync(app, phoneToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "an earlier header token is refused afterwards");
        (await NativeRefreshAsync(app, tabletToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "every device of the caller is signed out");
    }

    [Fact]
    [Trait("Rule", "C12")]
    public async Task ChangePassword_NativeCorrectCurrentPassword_RefusesEarlierHeaderTokensAndSetsNoCookie()
    {
        // Arrange
        var (app, tenant) = await StartAsync();
        await using var _ = tenant;
        var phoneToken = await IssueAsync(app, MariaId, PhoneAgent);
        var request = SignedIn(app, MariaId, NativePost(ChangePasswordUrl, headerToken: null,
            content: JsonContent.Create(new { currentPassword = MariaPassword, newPassword = NewPassword })));

        // Act
        var response = await NewClient(app).SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue("a change with the correct current password is accepted");
        SetsRefreshCookie(response).ShouldBeFalse("a native app has no cookie to clear");
        (await NativeRefreshAsync(app, phoneToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "changing the password signs the person out everywhere, so an earlier header token is refused");
    }
}
