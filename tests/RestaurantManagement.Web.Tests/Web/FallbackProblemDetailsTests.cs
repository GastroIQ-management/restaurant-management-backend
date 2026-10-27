using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Shouldly;
using static RestaurantManagement.Web.Tests.LocalizedResponses;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// An error the framework makes (bodiless error result, unknown route, wrong method, wrong content type)
/// is Problem Details with the fallback code of its status and a title in the caller's language.
/// </summary>
public class FallbackProblemDetailsTests : IDisposable
{
    private static readonly Guid MariaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");

    private readonly PermissionProbeApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<HttpResponseMessage> SendAsync(string method, string url, string culture, string? plainTextBody)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestAccessTokens.Create(_factory.Services, MariaId));
        request.Headers.Add("Accept-Language", culture);
        if (plainTextBody is not null)
            request.Content = new StringContent(plainTextBody, Encoding.UTF8, "text/plain");
        return await _factory.CreateClient().SendAsync(request);
    }

    [Theory]
    [InlineData("GET", "/api/does-not-exist", null, HttpStatusCode.NotFound, "http.not_found")]
    [InlineData("POST", "/api/test-probe/members", null, HttpStatusCode.MethodNotAllowed, "http.method_not_allowed")]
    [InlineData("GET", "/api/test-probe/bodiless-conflict", null, HttpStatusCode.Conflict, "http.conflict")]
    [InlineData("POST", "/api/v1/auth/login", "ana@cantinadanonna.com.br", HttpStatusCode.UnsupportedMediaType, "http.unsupported_media_type")]
    [Trait("Rule", "W28")]
    public async Task FrameworkError_UnknownRouteWrongMethodBodilessResultWrongContentType_AnswersFallbackCodeWithATitlePerLanguage(
        string method, string url, string? plainTextBody, HttpStatusCode status, string expectedCode)
    {
        // Act
        var english = await SendAsync(method, url, "en", plainTextBody);
        var portuguese = await SendAsync(method, url, "pt", plainTextBody);

        // Assert
        english.StatusCode.ShouldBe(status, "the status of the framework answer is unchanged");
        english.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json", "a framework error is Problem Details too");
        (await CodeAsync(english)).ShouldBe(expectedCode, "every status has one fallback code");
        (await CodeAsync(portuguese)).ShouldBe(expectedCode, "the code is the same in every language");
        (await MessageAsync(english)).ShouldNotBeNullOrWhiteSpace("the fallback answer has a title");
        (await MessageAsync(portuguese)).ShouldNotBe(await MessageAsync(english), "the title follows the caller's language");
    }
}
