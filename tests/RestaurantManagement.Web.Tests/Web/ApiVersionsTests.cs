using RestaurantManagement.Web.OpenApi;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>Which routes belong to which API version: the route prefix decides, nothing else.</summary>
public class ApiVersionsTests
{
    [Theory]
    [InlineData("v1", "api/v1/menus", true)]
    [InlineData("v1", "/api/v1/menus/{menuId}", true)]
    [InlineData("v1", "api/v2/menus", false)]
    [InlineData("v1", "api/v10/menus", false)]
    [InlineData("v1", "api/menus", false)]
    [InlineData("v2", "api/v1/menus", false)]
    [Trait("Rule", "WD5")]
    public void IncludesRoute_RouteAndVersion_IncludesOnlyRoutesWithThatVersionPrefix(string version, string route, bool expected)
    {
        // Act
        var included = ApiVersions.IncludesRoute(version, route);

        // Assert
        included.ShouldBe(expected, "a version's document holds only the routes that start with api/<version>/, so v1 never includes v10");
    }
}
