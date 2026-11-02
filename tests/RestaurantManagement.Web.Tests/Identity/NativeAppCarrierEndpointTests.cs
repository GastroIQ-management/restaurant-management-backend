using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RestaurantManagement.Modules.Identity.Models.Sessions;
using RestaurantManagement.Modules.Identity.Services.Sessions;
using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Which way the refresh token travels, decided by the Origin header alone: the two native app origins get it in the
/// body and send it in the X-Refresh-Token header; every other request keeps the cookie. The session service is
/// substituted; its rules are covered by the Identity unit tests and NativeAppSessionEndpointTests.
/// </summary>
public class NativeAppCarrierEndpointTests : IDisposable
{
    private const string LoginUrl = "/api/v1/auth/login";
    private const string RefreshUrl = "/api/v1/auth/refresh";
    private const string LogoutUrl = "/api/v1/auth/logout";
    private const string CookieName = "__Secure-refresh_token";
    private const string TokenHeader = "X-Refresh-Token";

    private const string IssuedToken = "a2l0Y2hlbi10YWJsZXQtcmVmcmVzaC10b2tlbi0wMDAx";
    private const string CookieToken = "Y3VycmVudC1jb29raWUtcmVmcmVzaC10b2tlbi0wMDAy";
    private const string HeaderToken = "bmF0aXZlLWFwcC1oZWFkZXItcmVmcmVzaC10b2tlbi0wMDAz";

    private static readonly Guid MariaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly DateTime TokenExpiry = new(2026, 11, 14, 18, 30, 0, DateTimeKind.Utc);

    private readonly AuthApiFactory _factory = new();

    public void Dispose()
    {
        _factory.Dispose();
    }

    private static SessionResult IssuedSession() => new()
    {
        Auth = new AuthResponse
        {
            UserId = MariaId,
            Email = "maria@cantinadanonna.com.br",
            FirstName = "Maria",
            LastName = "Souza",
            Token = "access-pass-for-maria"
        },
        RefreshToken = IssuedToken,
        RefreshTokenExpiresAt = TokenExpiry
    };

    private static HttpRequestMessage Post(string url, string? origin, string? headerToken = null, string? cookieToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (url == LoginUrl)
            request.Content = JsonContent.Create(new { email = "maria@cantinadanonna.com.br", password = "Sup3r$ecret!" });
        if (origin is not null)
            request.Headers.TryAddWithoutValidation("Origin", origin);
        if (headerToken is not null)
            request.Headers.Add(TokenHeader, headerToken);
        if (cookieToken is not null)
            request.Headers.Add("Cookie", $"{CookieName}={cookieToken}");
        return request;
    }

    private static bool SetsCookie(HttpResponseMessage response)
        => response.Headers.TryGetValues(HeaderNames.SetCookie, out var values)
           && SetCookieHeaderValue.ParseList(values.ToList()).Any(c => c.Name == CookieName);

    private HttpClient CreateClient(Dictionary<string, string?>? settings = null)
    {
        var factory = settings is null
            ? _factory
            : _factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings)));
        return factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    // Failure first: a browser page can never opt in
    [Theory]
    [InlineData("https://app.cantinadanonna.com.br")]
    [InlineData("https://localhost:8100")]
    [InlineData("http://localhost")]
    [InlineData("HTTPS://localhost")]
    [InlineData("https://localhost/")]
    [InlineData(null)]
    [Trait("Rule", "L40")]
    public async Task Login_WebOrigin_SetsCookieAndBodyHasNoRefreshToken(string? origin)
    {
        // Arrange
        _factory.SessionService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<ClientInfo>(), Arg.Any<string?>()).Returns(IssuedSession());
        var client = CreateClient();

        // Act
        var response = await client.SendAsync(Post(LoginUrl, origin, headerToken: HeaderToken));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "valid credentials log the person in");
        SetsCookie(response).ShouldBeTrue("a request from any origin that is not a native app origin keeps the cookie");
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("refreshToken", Case.Insensitive, "a web page, or a request with no origin, never gets the refresh token in the body, even when it sends the native header");
    }

    // Failure first: the header is never read for the web
    [Fact]
    [Trait("Rule", "F19")]
    public async Task Refresh_WebOrigin_IgnoresTheHeaderReadsTheCookieAndBodyHasNoRefreshToken()
    {
        // Arrange
        _factory.SessionService.RefreshTokenAsync(CookieToken).Returns(IssuedSession());
        var client = CreateClient();

        // Act
        var response = await client.SendAsync(Post(RefreshUrl, "https://localhost:8100", HeaderToken, CookieToken));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "the cookie token renews the session");
        await _factory.SessionService.DidNotReceive().RefreshTokenAsync(HeaderToken);
        SetsCookie(response).ShouldBeTrue("the web keeps the cookie");
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("refreshToken", Case.Insensitive, "the web response has no refresh token fields at all");
    }

    // Failure first: a cookie is never read on native
    [Fact]
    [Trait("Rule", "F18")]
    public async Task Refresh_NativeOriginWithOnlyACookie_ReadsNoTokenAndIsRefused()
    {
        // Arrange
        _factory.SessionService.RefreshTokenAsync(Arg.Is<string?>(t => t == null))
            .Throws(new BusinessException(SessionErrorCodes.InvalidOrExpiredSession, "InvalidOrExpiredToken"));
        var client = CreateClient();

        // Act
        var response = await client.SendAsync(Post(RefreshUrl, "https://localhost", headerToken: null, cookieToken: CookieToken));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "a native request without the header has no session to renew");
        await _factory.SessionService.DidNotReceive().RefreshTokenAsync(CookieToken);
    }

    [Theory]
    [InlineData("https://localhost")]
    [InlineData("capacitor://localhost")]
    [Trait("Rule", "L39")]
    public async Task Login_NativeOrigin_ReturnsRefreshTokenInBodyAndSetsNoCookie(string origin)
    {
        // Arrange
        _factory.SessionService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<ClientInfo>(), Arg.Any<string?>()).Returns(IssuedSession());
        var client = CreateClient();

        // Act
        var response = await client.SendAsync(Post(LoginUrl, origin));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "valid credentials log the person in");
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        body["refreshToken"]?.GetValue<string>().ShouldBe(IssuedToken, "a native app gets the refresh token in the body");
        body["refreshTokenExpiresAt"]?.GetValue<DateTime>().ToUniversalTime().ShouldBe(TokenExpiry, "the body also tells when the refresh token expires");
        body["token"]?.GetValue<string>().ShouldBe("access-pass-for-maria", "the access token stays in the body as before");
        SetsCookie(response).ShouldBeFalse("a native app gets no cookie");
    }

    [Fact]
    [Trait("Rule", "L39")]
    public async Task Login_NativeOriginSendingACookie_PassesNoPreviousTokenToSession()
    {
        // Arrange
        _factory.SessionService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<ClientInfo>(), Arg.Any<string?>()).Returns(IssuedSession());
        var client = CreateClient();

        // Act
        await client.SendAsync(Post(LoginUrl, "capacitor://localhost", cookieToken: CookieToken));

        // Assert
        await _factory.SessionService.Received(1).LoginAsync(
            Arg.Any<LoginRequest>(), Arg.Any<ClientInfo>(), Arg.Is<string?>(t => t == null));
    }

    [Fact]
    [Trait("Rule", "F18")]
    public async Task Refresh_NativeOriginWithHeader_SendsHeaderTokenToSessionAndReturnsReplacementInBody()
    {
        // Arrange
        _factory.SessionService.RefreshTokenAsync(HeaderToken).Returns(IssuedSession());
        var client = CreateClient();

        // Act
        var response = await client.SendAsync(Post(RefreshUrl, "https://localhost", HeaderToken, CookieToken));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "a valid header token renews the session");
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        body["refreshToken"]?.GetValue<string>().ShouldBe(IssuedToken, "the replacement refresh token comes back in the body");
        SetsCookie(response).ShouldBeFalse("a native app gets no cookie");
        await _factory.SessionService.DidNotReceive().RefreshTokenAsync(CookieToken);
    }

    [Theory]
    [InlineData(RefreshUrl)]
    [InlineData(LogoutUrl)]
    [Trait("Rule", "F20")]
    [Trait("Rule", "O10")]
    public async Task NativeRefreshOrLogout_LimitExceeded_ReturnsTooManyRequests(string url)
    {
        // Arrange: one request per 10 minutes
        _factory.SessionService.RefreshTokenAsync(Arg.Any<string?>()).Returns(IssuedSession());
        var client = CreateClient(new Dictionary<string, string?>
        {
            ["AuthRateLimit:Refresh:PermitLimit"] = "1",
            ["AuthRateLimit:Refresh:WindowSeconds"] = "600",
        });
        await client.SendAsync(Post(url, "https://localhost", HeaderToken));

        // Act
        var response = await client.SendAsync(Post(url, "https://localhost", HeaderToken));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests, "native refresh and logout share the web limit, and this is the second request");
    }
}
