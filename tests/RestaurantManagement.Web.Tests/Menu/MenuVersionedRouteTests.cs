using System.Net;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>Menu endpoints answer only under /api/v1/; the unversioned paths are gone.</summary>
public class MenuVersionedRouteTests : IDisposable
{
    private readonly AuthApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("GET", "/api/menus")]
    [InlineData("POST", "/api/menus/create")]
    [InlineData("POST", "/api/menu-categories")]
    [Trait("Rule", "M48")]
    public async Task UnversionedPath_MenuEndpoints_Answer404(string method, string url)
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), url);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "the unversioned path no longer exists; the endpoint lives under /api/v1/");
    }
}
