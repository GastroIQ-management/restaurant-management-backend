using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>One OpenAPI document per API version, served in Development, holding only that version's endpoints.</summary>
public class ApiVersionDocumentsTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();

    public void Dispose() => _disposables.ForEach(d => d.Dispose());

    private (HttpClient Client, AuthApiFactory Factory) Development()
    {
        var factory = new AuthApiFactory();
        var development = factory.WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Development));
        _disposables.Add(factory);
        _disposables.Add(development);
        return (development.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false }), factory);
    }

    private async Task<JsonNode> ReadV1Async(HttpClient client)
        => JsonNode.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"))!;

    [Fact]
    [Trait("Rule", "W30")]
    public async Task V1Document_InDevelopment_ListsOnlyV1PathsAndEveryV1Operation()
    {
        // Arrange
        var (client, factory) = Development();
        var expectedOperations = factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items.SelectMany(group => group.Items)
            .Count(api => api.RelativePath!.StartsWith("api/v1/"));

        // Act
        var document = await ReadV1Async(client);

        // Assert
        var paths = document["paths"]!.AsObject();
        paths.Select(path => path.Key).Where(key => !key.StartsWith("/api/v1/")).ShouldBeEmpty("the v1 document holds only /api/v1/ paths");
        paths.Sum(path => path.Value!.AsObject().Count).ShouldBe(expectedOperations, "every /api/v1/ operation is in the v1 document");
    }

    [Fact]
    [Trait("Rule", "W31")]
    public async Task Document_VersionThatIsNotSupported_IsNotServed()
    {
        // Arrange
        var (client, _) = Development();

        // Act
        var response = await client.GetAsync("/swagger/v2/swagger.json");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "only supported versions have a document");
    }

    [Fact]
    [Trait("Rule", "WD6")]
    public async Task V1Document_InDevelopment_BearerDescriptionPointsToTheVersionedLogin()
    {
        // Arrange
        var (client, _) = Development();

        // Act
        var document = await ReadV1Async(client);

        // Assert
        var description = document["components"]!["securitySchemes"]!["Bearer"]!["description"]!.GetValue<string>();
        description.ShouldContain("/api/v1/auth/login", customMessage: "the description names the real login path");
    }

    [Fact]
    [Trait("Rule", "WD7")]
    public async Task V1Document_InDevelopment_DescriptionStatesTheAdditiveOnlyAndThirtyDayRules()
    {
        // Arrange
        var (client, _) = Development();

        // Act
        var document = await ReadV1Async(client);

        // Assert
        var description = document["info"]!["description"]!.GetValue<string>();
        description.ShouldContain("additive", Case.Insensitive, "inside a version, changes are additive only");
        description.ShouldContain("30 days", Case.Insensitive, "an old version stays served at least 30 days after the next one ships");
    }
}
