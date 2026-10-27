using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using RestaurantManagement.Modules.Identity.Services.Sessions;
using RestaurantManagement.Modules.Menu.Services;
using RestaurantManagement.Modules.Organization.Services;
using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The OpenAPI document (served in Development) describes the one Problem Details error body: its
/// `code` lists every code of every catalog, and the retry rule is written in its description.
/// The schema component is named `ApiProblemDetails`.
/// </summary>
public class ErrorCodesOpenApiTests : IDisposable
{
    private const string SwaggerDocumentUrl = "/swagger/v1/swagger.json";
    private const string SchemaName = "ApiProblemDetails";

    private readonly List<IDisposable> _disposables = new();

    public void Dispose() => _disposables.ForEach(d => d.Dispose());

    private async Task<JsonNode> ReadDocumentAsync()
    {
        var factory = new AuthApiFactory().WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Development));
        _disposables.Add(factory);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return JsonNode.Parse(await client.GetStringAsync(SwaggerDocumentUrl))!;
    }

    [Fact]
    [Trait("Rule", "WD4")]
    public async Task SwaggerDocument_InDevelopment_CodeEnumEqualsTheUnionOfEveryCatalog()
    {
        // Arrange
        var expected = ErrorCodeCatalogs.Read(
                typeof(ErrorCodes).Assembly, typeof(MenuErrorCodes).Assembly,
                typeof(OrganizationErrorCodes).Assembly, typeof(SessionErrorCodes).Assembly)
            .Select(code => code.Value).ToList();

        // Act
        var document = await ReadDocumentAsync();

        // Assert
        var listed = document["components"]!["schemas"]![SchemaName]!["properties"]!["code"]!["enum"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
        listed.ShouldBe(expected, ignoreOrder: true, "the document lists every code of every module, read from the catalogs");
        listed.ShouldContain("menu.not_found", "a Menu code is listed");
        listed.ShouldContain("request.in_progress", "a Shared retry code is listed");
        listed.ShouldContain("validation.failed", "the validation code is listed");
    }

    [Fact]
    [Trait("Rule", "WD4")]
    public async Task SwaggerDocument_InDevelopment_SchemaDescriptionStatesTheRetryRuleAndListsTheRetryCodes()
    {
        // Act
        var document = await ReadDocumentAsync();

        // Assert
        var description = document["components"]!["schemas"]![SchemaName]!["description"]!.GetValue<string>();
        description.ShouldContain("network", customMessage: "the rule says to retry on network errors");
        description.ShouldContain("5xx", customMessage: "the rule says to retry on server errors");
        description.ShouldContain("rate_limit.exceeded", customMessage: "the rule lists the 429 retry code");
        description.ShouldContain("request.in_progress", customMessage: "the rule lists the 409 retry code");
        description.ShouldContain("server.unexpected_error", customMessage: "the rule lists the server error retry code");
    }

    [Theory]
    [InlineData("4XX")]
    [InlineData("5XX")]
    [Trait("Rule", "WD4")]
    public async Task SwaggerDocument_InDevelopment_EveryOperationDeclaresItsErrorAnswersWithTheProblemDetailsSchema(string statusRange)
    {
        // Act
        var document = await ReadDocumentAsync();

        // Assert
        var reference = document["paths"]!["/api/menus"]!["get"]!["responses"]![statusRange]!["content"]!["application/problem+json"]!["schema"]!["$ref"]!.GetValue<string>();
        reference.ShouldBe($"#/components/schemas/{SchemaName}", "an operation shows the one Problem Details schema on its client and server error answers");
    }
}
