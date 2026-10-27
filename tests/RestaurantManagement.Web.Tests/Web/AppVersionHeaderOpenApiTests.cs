using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using RestaurantManagement.Shared.Middleware;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>The OpenAPI document tells generated clients to send X-App-Version on every write, and only on writes.</summary>
public class AppVersionHeaderOpenApiTests : IDisposable
{
    private static readonly string[] WriteMethods = { "post", "put", "patch", "delete" };

    private readonly List<IDisposable> _disposables = new();

    public void Dispose() => _disposables.ForEach(d => d.Dispose());

    private async Task<List<(string Path, string Method, JsonNode Operation)>> ReadOperationsAsync()
    {
        var factory = new AuthApiFactory().WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Development));
        _disposables.Add(factory);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var document = JsonNode.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"))!;
        return document["paths"]!.AsObject()
            .SelectMany(path => path.Value!.AsObject().Select(method => (path.Key, method.Key, method.Value!)))
            .ToList();
    }

    private static JsonNode? HeaderOf(JsonNode operation)
        => operation["parameters"]?.AsArray()
            .FirstOrDefault(p => p!["name"]!.GetValue<string>() == AppVersionMiddleware.HeaderName && p["in"]!.GetValue<string>() == "header");

    [Fact]
    [Trait("Rule", "WD8")]
    public async Task V1Document_EveryWriteOperation_DeclaresTheRequiredAppVersionHeader()
    {
        // Act
        var operations = await ReadOperationsAsync();

        // Assert
        var writes = operations.Where(o => WriteMethods.Contains(o.Method)).ToList();
        writes.ShouldNotBeEmpty();
        writes.Where(o => HeaderOf(o.Operation)?["required"]?.GetValue<bool>() != true)
            .Select(o => $"{o.Method} {o.Path}").ShouldBeEmpty("every POST, PUT, PATCH and DELETE needs X-App-Version, so generated clients must send it");
    }

    [Fact]
    [Trait("Rule", "WD9")]
    public async Task V1Document_ReadOperations_DoNotDeclareTheAppVersionHeader()
    {
        // Act
        var operations = await ReadOperationsAsync();

        // Assert
        var reads = operations.Where(o => !WriteMethods.Contains(o.Method)).ToList();
        reads.ShouldNotBeEmpty();
        reads.Where(o => HeaderOf(o.Operation) is not null)
            .Select(o => $"{o.Method} {o.Path}").ShouldBeEmpty("reads need no app version, so the document must not ask for one");
    }

    [Fact]
    [Trait("Rule", "WD10")]
    public async Task V1Document_AppVersionHeader_DeclaresItsFormatLengthAndPurpose()
    {
        // Act
        var operations = await ReadOperationsAsync();

        // Assert
        var login = operations.Single(o => o.Method == "post" && o.Path == "/api/v1/auth/login").Operation;
        var header = HeaderOf(login)!;
        header["schema"]!["maxLength"]!.GetValue<int>().ShouldBe(32, "the app version is at most 32 characters");
        header["schema"]!["pattern"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace("the header declares the semantic version format");
        header["description"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace("the header says why it is sent");
    }
}
