using System.Net;
using System.Net.Http.Headers;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>Identity endpoints answer only under /api/v1/; the unversioned paths are gone.</summary>
public class IdentityVersionedRouteTests : IDisposable
{
    private readonly AuthApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("POST", "/api/auth/login")]
    [InlineData("GET", "/api/roles")]
    [InlineData("POST", "/api/invitations")]
    [InlineData("PUT", "/api/users/me/language")]
    [InlineData("PUT", "/api/members/22222222-bbbb-4bbb-8bbb-00000000000b/access/44444444-dddd-4ddd-8ddd-000000000001")]
    [Trait("Rule", "T147")]
    public async Task UnversionedPath_IdentityEndpoints_Answer404(string method, string url)
    {
        // Arrange
        // Signed in: the fallback policy answers 401 to an anonymous caller before a missing route can answer 404
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestAccessTokens.Create(_factory.Services, Guid.NewGuid()));

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "the unversioned path no longer exists; the endpoint lives under /api/v1/");
    }
}
