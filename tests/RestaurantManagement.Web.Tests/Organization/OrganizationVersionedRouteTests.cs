using System.Net;
using System.Net.Http.Headers;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>Organization endpoints answer only under /api/v1/; the unversioned paths are gone.</summary>
public class OrganizationVersionedRouteTests : IDisposable
{
    private readonly AuthApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("GET", "/api/organizations")]
    [InlineData("GET", "/api/locations")]
    [InlineData("PUT", "/api/organizations/edit")]
    [Trait("Rule", "OR48")]
    public async Task UnversionedPath_OrganizationEndpoints_Answer404(string method, string url)
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
