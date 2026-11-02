using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using RestaurantManagement.Shared.Services.ClientTime;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The OpenAPI document tells generated clients to send the two device-time headers on every signed-in write
/// (the three session actions included), and nowhere else.
/// </summary>
public class ClientTimeHeadersOpenApiTests : IDisposable
{
    private static readonly string[] WriteMethods = { "post", "put", "patch", "delete" };

    // Writes that need no sign-in.
    private static readonly string[] PublicWrites =
    {
        "/api/v1/auth/register", "/api/v1/auth/login", "/api/v1/auth/refresh", "/api/v1/auth/accept-invitation", "/api/v1/auth/logout",
        "/api/v1/auth/verify", "/api/v1/auth/resend-verification", "/api/v1/auth/forgot-password", "/api/v1/auth/reset-password"
    };

    private static readonly string[] Headers = { ClientTimeHeaders.ActionTime, ClientTimeHeaders.SentAt };

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

    private static JsonNode? HeaderOf(JsonNode operation, string name)
        => operation["parameters"]?.AsArray()
            .FirstOrDefault(p => p!["name"]!.GetValue<string>() == name && p["in"]!.GetValue<string>() == "header");

    private static bool IsDeclaredAsRequiredDateTime(JsonNode operation, string name)
        => HeaderOf(operation, name) is { } header
            && header["required"]?.GetValue<bool>() == true
            && header["schema"]?["format"]?.GetValue<string>() == "date-time"
            && header["schema"]?["maxLength"] is not null
            && !string.IsNullOrWhiteSpace(header["description"]?.GetValue<string>());

    [Fact]
    [Trait("Rule", "WD16")]
    public async Task V1Document_SignedInWrites_DeclareBothDeviceTimeHeadersAsRequiredDateTimes()
    {
        // Act
        var operations = await ReadOperationsAsync();

        // Assert
        var signedInWrites = operations.Where(o => WriteMethods.Contains(o.Method) && !PublicWrites.Contains(o.Path)).ToList();
        signedInWrites.ShouldNotBeEmpty();
        signedInWrites.Where(o => !Headers.All(h => IsDeclaredAsRequiredDateTime(o.Operation, h)))
            .Select(o => $"{o.Method} {o.Path}").ShouldBeEmpty("every signed-in write needs both device times, with their format, length limit and purpose shown");
    }

    [Fact]
    [Trait("Rule", "WD17")]
    public async Task V1Document_PublicWritesAndReads_DoNotDeclareTheDeviceTimeHeaders()
    {
        // Act
        var operations = await ReadOperationsAsync();

        // Assert
        var notCovered = operations.Where(o => !WriteMethods.Contains(o.Method) || PublicWrites.Contains(o.Path)).ToList();
        notCovered.ShouldNotBeEmpty();
        notCovered.Where(o => Headers.Any(h => HeaderOf(o.Operation, h) is not null))
            .Select(o => $"{o.Method} {o.Path}").ShouldBeEmpty("reads and anonymous writes are not asked for device times");
    }
}
