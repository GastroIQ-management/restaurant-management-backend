using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The native apps (Capacitor shells) run on fixed local origins and must be allowed to call the API with credentials.
/// </summary>
public class NativeAppCorsTests : IDisposable
{
    private const string RefreshUrl = "/api/v1/auth/refresh";

    private readonly AuthApiFactory _factory = new();

    public void Dispose()
    {
        _factory.Dispose();
    }

    [Theory]
    [InlineData("capacitor://localhost")]
    [InlineData("https://localhost")]
    [Trait("Rule", "W36")]
    public async Task Cors_PreflightFromNativeAppOrigin_AllowsThatOriginWithCredentialsAndTheRefreshHeader(string origin)
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var request = new HttpRequestMessage(HttpMethod.Options, RefreshUrl);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "x-refresh-token");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe(new[] { origin }, "the native app origin must be allowed");
        response.Headers.GetValues("Access-Control-Allow-Credentials").ShouldBe(new[] { "true" }, "the native app may send credentials");
        string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers")).ToLowerInvariant()
            .ShouldContain("x-refresh-token", customMessage: "the native app sends its refresh token in this header");
    }

    [Theory]
    [InlineData("capacitor://localhost")]
    [InlineData("https://localhost")]
    [Trait("Rule", "W36")]
    public async Task Cors_ActualRequestFromNativeAppOrigin_GetsThatOriginBack(string origin)
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var request = new HttpRequestMessage(HttpMethod.Post, RefreshUrl);
        request.Headers.Add("Origin", origin);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldNotBe(HttpStatusCode.NotFound, "the request must reach the refresh endpoint");
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe(new[] { origin }, "the native app must be able to read the answer");
    }

    [Fact]
    [Trait("Rule", "W36")]
    public async Task Cors_PreflightFromOtherLocalhostOrigin_GetsNoCrossOriginPermission()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var request = new HttpRequestMessage(HttpMethod.Options, RefreshUrl);
        request.Headers.Add("Origin", "http://localhost");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse("only the two exact native app origins are allowed, not other localhost origins");
    }
}
