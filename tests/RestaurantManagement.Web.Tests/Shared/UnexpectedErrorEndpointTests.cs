using System.Net;
using NSubstitute;
using RestaurantManagement.Modules.Identity.Models.Sessions;
using Shouldly;
using static RestaurantManagement.Web.Tests.LocalizedResponses;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// An unexpected failure behind a real endpoint answers 500 in the language the client asked for
/// (spec Outcome #7). The sign-in service is a substitute, so it can be made to fail on purpose.
/// </summary>
public class UnexpectedErrorEndpointTests : IDisposable
{
    private readonly AuthApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    // Scenario 6
    [Fact]
    [Trait("Rule", "SH18")]
    public async Task Login_UnexpectedFailureInPortuguese_ReturnsServerErrorWithPortugueseMessage()
    {
        // Arrange
        _factory.SessionService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<ClientInfo>(), Arg.Any<string?>())
            .Returns(Task.FromException<SessionResult>(new InvalidOperationException("Session store unavailable")));
        var client = _factory.CreateClient();
        var request = Request("POST", "/api/auth/login", "pt", new { email = "ana@cantinadanonna.com.br", password = "Massa@Fresca2026" });

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        (await MessageAsync(response)).ShouldBe("Ocorreu um erro inesperado.");
    }

    [Fact]
    [Trait("Rule", "SH18")]
    public async Task Login_UnexpectedFailure_AnswersProblemDetailsWithServerUnexpectedErrorCode()
    {
        // Arrange
        _factory.SessionService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<ClientInfo>(), Arg.Any<string?>())
            .Returns(Task.FromException<SessionResult>(new InvalidOperationException("Session store unavailable")));
        var client = _factory.CreateClient();
        var request = Request("POST", "/api/auth/login", "fr", new { email = "ana@cantinadanonna.com.br", password = "Massa@Fresca2026" });

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json", "an unexpected failure is answered as Problem Details");
        (await CodeAsync(response)).ShouldBe("server.unexpected_error", "the app can tell a server failure from any refusal by its code");
        (await ProblemAsync(response))["detail"].ShouldBeNull("the exception text never reaches the caller outside Development");
    }
}
