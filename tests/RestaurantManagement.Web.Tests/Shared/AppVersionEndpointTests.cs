using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using RestaurantManagement.Shared.Middleware;
using Shouldly;
using static RestaurantManagement.Web.Tests.LocalizedResponses;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The X-App-Version check through the real pipeline: a write without a valid one is refused
/// before sign-in, so a public and a signed-in caller get the same answer, and a browser can read it.
/// </summary>
public class AppVersionEndpointTests : IDisposable
{
    private const string LoginUrl = "/api/v1/auth/login";
    private const string LogoutAllUrl = "/api/v1/auth/logout-all";
    private const string AllowedOrigin = "http://localhost:5173";
    private static readonly Guid MariaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");

    private readonly PermissionProbeApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private HttpRequestMessage Write(string url, string? appVersion, string culture = "en")
    {
        var request = Request("POST", url, culture, new { email = "ana@cantinadanonna.com.br", password = "Massa@Fresca2026" });
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestAccessTokens.Create(_factory.Services, MariaId));
        if (appVersion is not null)
            request.Headers.Add(AppVersionMiddleware.HeaderName, appVersion);
        return request;
    }

    private HttpClient ClientWithoutAppVersion()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Remove(AppVersionMiddleware.HeaderName);
        return client;
    }

    [Theory]
    [InlineData(LoginUrl)]
    [InlineData(LogoutAllUrl)]
    [Trait("Rule", "SH62")]
    public async Task Write_WithoutAppVersion_Answers400RequiredOnPublicAndSignedInEndpoints(string url)
    {
        // Arrange
        var client = ClientWithoutAppVersion();

        // Act
        var response = await client.SendAsync(Write(url, appVersion: null));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a write without the app version is refused, signed in or not");
        (await CodeAsync(response)).ShouldBe("app_version.required", "the answer says the header is missing");
    }

    [Theory]
    [InlineData(LoginUrl, "latest")]
    [InlineData(LogoutAllUrl, "latest")]
    [InlineData(LoginUrl, "1.0.0-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData(LogoutAllUrl, "1.0.0-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [Trait("Rule", "SH63")]
    [Trait("Rule", "SH64")]
    public async Task Write_WithMalformedOrTooLongAppVersion_Answers400InvalidOnPublicAndSignedInEndpoints(string url, string appVersion)
    {
        // Arrange
        var client = ClientWithoutAppVersion();

        // Act
        var response = await client.SendAsync(Write(url, appVersion));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a malformed or over-long app version is refused, signed in or not");
        (await CodeAsync(response)).ShouldBe("app_version.invalid", "the answer says the value is not an acceptable version");
    }

    [Fact]
    [Trait("Rule", "SH67")]
    public async Task Write_WithoutAppVersion_AnonymousAndSignedInCallersGetTheSameAnswer()
    {
        // Arrange
        var client = ClientWithoutAppVersion();
        var anonymous = Request("POST", LoginUrl, "en", new { email = "ana@cantinadanonna.com.br", password = "Massa@Fresca2026" });

        // Act
        var anonymousAnswer = await client.SendAsync(anonymous);
        var signedInAnswer = await client.SendAsync(Write(LogoutAllUrl, appVersion: null));

        // Assert
        signedInAnswer.StatusCode.ShouldBe(anonymousAnswer.StatusCode, "the check runs before sign-in, so the status is the same");
        (await ProblemWithoutTraceIdAsync(signedInAnswer)).ShouldBe(await ProblemWithoutTraceIdAsync(anonymousAnswer),
            "the check runs before sign-in, so the body is the same");
    }

    [Fact]
    [Trait("Rule", "SH68")]
    public async Task Write_WithoutAppVersionFromAllowedOrigin_RefusalCanBeReadByTheBrowser()
    {
        // Arrange
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = AllowedOrigin })));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Remove(AppVersionMiddleware.HeaderName);
        var request = Write(LoginUrl, appVersion: null);
        request.Headers.Add("Origin", AllowedOrigin);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "the write is refused");
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeTrue("the check runs after CORS, so a browser page on an allowed origin can read the refusal");
    }

    [Theory]
    [InlineData("pt")]
    [InlineData("fr")]
    [Trait("Rule", "SH69")]
    public async Task Write_WithoutAppVersion_TitleFollowsTheLanguageAndCodeStaysTheSame(string culture)
    {
        // Arrange
        var client = ClientWithoutAppVersion();

        // Act
        var response = await client.SendAsync(Write(LoginUrl, appVersion: null, culture));
        var english = await client.SendAsync(Write(LoginUrl, appVersion: null, "en"));

        // Assert
        (await CodeAsync(response)).ShouldBe("app_version.required", "the code is never translated");
        (await MessageAsync(response)).ShouldNotBeNullOrWhiteSpace("the refusal has a title in every language");
        (await MessageAsync(response)).ShouldNotBe(await MessageAsync(english), "the title follows the caller's language");
    }

    [Fact]
    [Trait("Rule", "SH65")]
    public async Task Read_WithoutAppVersion_AnswersAsBefore()
    {
        // Arrange
        var client = ClientWithoutAppVersion();

        // Act
        var response = await client.GetAsync("/api/test-probe/members");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "a read without the app version is not refused for it; it gets the answer it got before");
    }

    [Fact]
    [Trait("Rule", "SH66")]
    public async Task Write_WithAValidAppVersion_ReachesTheEndpoint()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = Write(LogoutAllUrl, appVersion: null);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue("the default test client sends a valid app version, so the write reaches the endpoint");
        await _factory.SessionService.Received(1).LogoutAllAsync(MariaId);
    }
}
