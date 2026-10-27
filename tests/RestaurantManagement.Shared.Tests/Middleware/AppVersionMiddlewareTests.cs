using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using NSubstitute;
using RestaurantManagement.Shared.Middleware;
using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Middleware;

/// <summary>
/// The X-App-Version check on writes. Endpoint behaviour through the real pipeline (sign-in,
/// cross-origin reads, languages) is covered in Web.Tests (AppVersionEndpointTests).
/// </summary>
public class AppVersionMiddlewareTests
{
    private bool _nextCalled;

    private static IStringLocalizer<SharedResource> EchoLocalizer()
    {
        var localizer = Substitute.For<IStringLocalizer<SharedResource>>();
        localizer[Arg.Any<string>()].Returns(call => new LocalizedString(call.Arg<string>(), call.Arg<string>()));
        return localizer;
    }

    private static HttpContext Request(string method, string? appVersion)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        if (appVersion is not null)
            context.Request.Headers[AppVersionMiddleware.HeaderName] = appVersion;
        return context;
    }

    private Task InvokeAsync(HttpContext context)
    {
        var sut = new AppVersionMiddleware(_ =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        });
        return sut.InvokeAsync(context, EchoLocalizer());
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    [Trait("Rule", "SH62")]
    public async Task InvokeAsync_WriteWithoutAppVersion_ThrowsRequiredAndStops(string method)
    {
        // Arrange
        var context = Request(method, appVersion: null);

        // Act
        var thrown = await Should.ThrowAsync<BusinessException>(() => InvokeAsync(context));

        // Assert
        thrown.StatusCode.ShouldBe(400, "a write without the app version is a bad request");
        thrown.Code.ShouldBe("app_version.required", "the caller can tell the header is missing from the code");
        _nextCalled.ShouldBeFalse("a refused write never reaches the endpoint");
    }

    [Theory]
    [InlineData("1.2")]
    [InlineData("v1.2.3")]
    [InlineData("latest")]
    [InlineData("1.2.3.4")]
    [InlineData("1.2.3 ")]
    [Trait("Rule", "SH63")]
    public async Task InvokeAsync_WriteWithAnAppVersionThatIsNotSemantic_ThrowsInvalidAndStops(string appVersion)
    {
        // Arrange
        var context = Request("POST", appVersion);

        // Act
        var thrown = await Should.ThrowAsync<BusinessException>(() => InvokeAsync(context));

        // Assert
        thrown.StatusCode.ShouldBe(400, "a malformed app version is a bad request");
        thrown.Code.ShouldBe("app_version.invalid", "the caller can tell the value is malformed from the code");
        _nextCalled.ShouldBeFalse("a refused write never reaches the endpoint");
    }

    [Fact]
    [Trait("Rule", "SH64")]
    public async Task InvokeAsync_WriteWithA33CharacterAppVersion_ThrowsInvalidAndStops()
    {
        // Arrange
        var thirtyThree = "1.0.0-" + new string('a', 27);
        var context = Request("POST", thirtyThree);

        // Act
        var thrown = await Should.ThrowAsync<BusinessException>(() => InvokeAsync(context));

        // Assert
        thirtyThree.Length.ShouldBe(33);
        thrown.Code.ShouldBe("app_version.invalid", "an app version over 32 characters is refused even when it is a semantic version");
        _nextCalled.ShouldBeFalse("a refused write never reaches the endpoint");
    }

    [Fact]
    [Trait("Rule", "SH64")]
    public async Task InvokeAsync_WriteWithA32CharacterAppVersion_ReachesTheEndpoint()
    {
        // Arrange
        var thirtyTwo = "1.0.0-" + new string('a', 26);
        var context = Request("POST", thirtyTwo);

        // Act
        await InvokeAsync(context);

        // Assert
        thirtyTwo.Length.ShouldBe(32);
        _nextCalled.ShouldBeTrue("32 characters is the longest app version allowed");
    }

    [Theory]
    [InlineData("PUT", "1.0.0")]
    [InlineData("PATCH", "2.14.3-beta.1")]
    [InlineData("DELETE", "1.0.0+build.5")]
    [Trait("Rule", "SH66")]
    public async Task InvokeAsync_WriteWithASemanticAppVersion_ReachesTheEndpoint(string method, string appVersion)
    {
        // Arrange
        var context = Request(method, appVersion);

        // Act
        await InvokeAsync(context);

        // Assert
        _nextCalled.ShouldBeTrue("a write with a valid app version goes on to the endpoint");
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    [Trait("Rule", "SH65")]
    public async Task InvokeAsync_ReadWithoutAppVersion_ReachesTheEndpoint(string method)
    {
        // Arrange
        var context = Request(method, appVersion: null);

        // Act
        await InvokeAsync(context);

        // Assert
        _nextCalled.ShouldBeTrue("reads and the browser's cross-origin preflight need no app version");
    }
}
