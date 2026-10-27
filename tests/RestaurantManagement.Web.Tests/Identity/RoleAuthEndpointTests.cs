using System.Net;
using Shouldly;
using static RestaurantManagement.Web.Tests.RoleWebSupport;

namespace RestaurantManagement.Web.Tests;

/// <summary>Sign-in rule of the role endpoints (no database needed).</summary>
public class RoleAuthEndpointTests
{
    private const string RoleId = "22222222-bbbb-4bbb-8bbb-00000000000b";

    [Theory]
    [InlineData("GET", "/api/v1/roles")]
    [InlineData("GET", "/api/v1/roles/" + RoleId)]
    [InlineData("POST", "/api/v1/roles")]
    [InlineData("PUT", "/api/v1/roles/" + RoleId + "/levels")]
    [InlineData("PUT", "/api/v1/roles/" + RoleId + "/name")]
    [Trait("Rule", "T120")]
    public async Task RoleEndpoints_NoAccessToken_ReturnUnauthorized(string method, string url)
    {
        // Arrange
        using var factory = new AuthApiFactory();
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), url) { Content = Json("""{"name":"Marketing"}""") };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "reading and changing roles need a signed-in user");
    }
}
