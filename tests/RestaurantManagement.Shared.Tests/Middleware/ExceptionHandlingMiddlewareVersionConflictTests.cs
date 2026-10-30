using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RestaurantManagement.Shared.Middleware;
using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Middleware;

/// <summary>The 409 `version.conflict` answer: the usual problem body plus the record as stored now.</summary>
public class ExceptionHandlingMiddlewareVersionConflictTests : IDisposable
{
    private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

    public void Dispose() => CultureInfo.CurrentUICulture = _originalUiCulture;

    private static async Task<(int StatusCode, JsonElement Body)> InvokeAsync(string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        var current = new { id = Guid.Parse("0b3f6c1e-5d2a-4e8b-9a47-1c6d8e2f7a10"), name = "Cardápio de almoço", version = 4321u };
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new VersionConflictException(current),
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
        return (context.Response.StatusCode, body);
    }

    [Theory]
    [InlineData("en", "This record was changed by someone else. Review the current version and try again.")]
    [InlineData("pt", "Este registro foi alterado por outra pessoa. Revise a versão atual e tente novamente.")]
    [InlineData("fr", "Cet enregistrement a été modifié par quelqu'un d'autre. Vérifiez la version actuelle et réessayez.")]
    [Trait("Rule", "SH76")]
    public async Task InvokeAsync_VersionConflict_Answers409WithCodeCurrentRecordAndTranslatedTitle(string culture, string expectedTitle)
    {
        // Arrange
        CultureInfo.CurrentUICulture = new CultureInfo(culture);

        // Act
        var (statusCode, body) = await InvokeAsync(Environments.Production);

        // Assert
        statusCode.ShouldBe(StatusCodes.Status409Conflict, "a stale version answers 409 Conflict");
        body.GetProperty("code").GetString().ShouldBe("version.conflict", "the code is fixed English text, never translated");
        body.GetProperty("title").GetString().ShouldBe(expectedTitle, "the title is shown in the language of the request");
        body.GetProperty("current").GetProperty("version").GetUInt32().ShouldBe(4321u, "the body carries the record as stored now, version included");
    }

    [Fact]
    [Trait("Rule", "SH77")]
    public async Task InvokeAsync_VersionConflictInDevelopment_BodyHoldsOnlyTheProblemFieldsAndTheCurrentRecord()
    {
        // Arrange
        CultureInfo.CurrentUICulture = new CultureInfo("en");

        // Act
        var (_, body) = await InvokeAsync(Environments.Development);

        // Assert
        var members = body.EnumerateObject().Select(p => p.Name).ToList();
        members.ShouldBe(new[] { "status", "title", "code", "traceId", "current" }, ignoreOrder: true, "the body holds the problem fields and the current record, and nothing else");
        body.GetRawText().ShouldNotContain("StackTrace", customMessage: "no stack trace or exception detail ever reaches the caller");
    }
}
