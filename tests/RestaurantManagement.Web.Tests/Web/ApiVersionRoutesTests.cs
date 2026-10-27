using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using RestaurantManagement.Web.OpenApi;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>Every endpoint of the app lives under a supported API version.</summary>
public class ApiVersionRoutesTests : IDisposable
{
    private readonly AuthApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    [Trait("Rule", "W29")]
    public void EveryEndpoint_RouteTemplate_StartsWithASupportedApiVersion()
    {
        // Arrange
        var templates = _factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items
            .Select(action => action.AttributeRouteInfo!.Template!)
            .ToList();

        // Act
        var outsideEveryVersion = templates
            .Where(template => !ApiVersions.Supported.Any(version => ApiVersions.IncludesRoute(version, template)))
            .ToList();

        // Assert
        templates.ShouldNotBeEmpty();
        outsideEveryVersion.ShouldBeEmpty("a route outside a supported version (api/v1/...) has no document and breaks the versioning rules");
    }
}
