using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RestaurantManagement.Shared.Middleware;
using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;

namespace RestaurantManagement.Shared.Tests
{
    /// <summary>
    /// Every error answer is RFC 9457 Problem Details (Request for Comments 9457, the standard error body)
    /// with a stable code: a business refusal and an unexpected failure.
    /// </summary>
    public class ExceptionHandlingMiddlewareTests
    {
        private const string BusinessErrorMessage = "Invalid email or password";
        private const int UnauthorizedStatusCode = 401;
        private const int InternalServerErrorStatusCode = 500;
        private const string ProblemContentType = "application/problem+json";

        private static async Task<(int StatusCode, string? ContentType, JsonElement Body)> InvokeAsync(Exception exception, string environmentName = "Production")
        {
            var environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns(environmentName);
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw exception,
                NullLogger<ExceptionHandlingMiddleware>.Instance,
                environment);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddLocalization(options => options.ResourcesPath = "Localization");
            var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            context.Response.Body.Position = 0;
            var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
            return (context.Response.StatusCode, context.Response.ContentType, body);
        }

        [Fact]
        [Trait("Rule", "SH17")]
        public async Task InvokeAsync_BusinessExceptionWithCode_AnswersProblemDetailsWithStatusTitleCodeAndTraceId()
        {
            // Arrange
            var exception = new BusinessException(ErrorCodes.InvalidToken, BusinessErrorMessage);

            // Act
            var (statusCode, contentType, body) = await InvokeAsync(exception);

            // Assert
            statusCode.ShouldBe(UnauthorizedStatusCode, "a business error answers with the status of its code");
            contentType.ShouldBe(ProblemContentType, "every error answer is Problem Details");
            body.GetProperty("status").GetInt32().ShouldBe(UnauthorizedStatusCode, "the body repeats the status");
            body.GetProperty("title").GetString().ShouldBe(BusinessErrorMessage, "the title is the localized message of the refusal");
            body.GetProperty("code").GetString().ShouldBe("auth.invalid_token", "the code is a fixed English string the app can act on");
            body.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace("the answer names the request for support");
            body.TryGetProperty("message", out _).ShouldBeFalse("the old message property is gone, the title replaces it");
        }

        [Fact]
        [Trait("Rule", "SH18")]
        public async Task InvokeAsync_UnexpectedExceptionInProduction_AnswersServerUnexpectedErrorWithoutDetail()
        {
            // Act
            var (statusCode, contentType, body) = await InvokeAsync(new InvalidOperationException("Connection string Host=db-prod-cantina"));

            // Assert
            statusCode.ShouldBe(InternalServerErrorStatusCode, "an unexpected error answers with a server error status");
            contentType.ShouldBe(ProblemContentType, "an unexpected error is Problem Details too");
            body.GetProperty("code").GetString().ShouldBe("server.unexpected_error", "an unexpected failure has its own retry code");
            body.TryGetProperty("detail", out _).ShouldBeFalse("the exception text is hidden outside Development");
            body.GetRawText().ShouldNotContain("db-prod-cantina", customMessage: "internal details such as connection settings never reach the caller");
        }
    }
}
