using System.Net;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>The audit log answers only under /api/v1/; the unversioned path is gone.</summary>
public class AuditLogVersionedRouteTests : IDisposable
{
    private readonly AuthApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("GET", "/api/audit-log")]
    [Trait("Rule", "SH70")]
    public async Task UnversionedPath_AuditLog_Answers404(string method, string url)
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
