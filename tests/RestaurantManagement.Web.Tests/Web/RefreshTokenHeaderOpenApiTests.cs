using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>The OpenAPI document tells native app clients to send X-Refresh-Token where the session cookie would otherwise carry the token.</summary>
public class RefreshTokenHeaderOpenApiTests : IDisposable
{
    private const string HeaderName = "X-Refresh-Token";
    private static readonly string[] SessionPaths = ["/api/v1/auth/refresh", "/api/v1/auth/select-organization", "/api/v1/auth/logout"];

    private readonly AuthApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<List<(string Path, string Method, JsonNode Operation)>> ReadOperationsAsync()
    {
        var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Development));
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var document = JsonNode.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"))!;
        return document["paths"]!.AsObject()
            .SelectMany(path => path.Value!.AsObject().Select(method => (path.Key, method.Key, method.Value!)))
            .ToList();
    }

    private static JsonNode? HeaderOf(JsonNode operation)
        => operation["parameters"]?.AsArray()
            .FirstOrDefault(p => p!["name"]!.GetValue<string>() == HeaderName && p["in"]!.GetValue<string>() == "header");

    [Fact]
    [Trait("Rule", "WD18")]
    public async Task V1Document_RefreshSelectOrganizationAndLogout_DeclareTheOptionalRefreshTokenHeaderForNativeApps()
    {
        // Act
        var operations = await ReadOperationsAsync();

        // Assert
        foreach (var path in SessionPaths)
        {
            var header = HeaderOf(operations.Single(o => o.Method == "post" && o.Path == path).Operation);
            header.ShouldNotBeNull($"{path} must tell native app clients to send the refresh token header");
            (header["required"]?.GetValue<bool>() ?? false).ShouldBeFalse($"{path}: the web uses the cookie, so the header stays optional");
            header["description"]!.GetValue<string>().ShouldContain("native", customMessage: "the header says it is for native apps only");
        }
    }

    [Fact]
    [Trait("Rule", "WD19")]
    public async Task V1Document_OtherOperations_DoNotDeclareTheRefreshTokenHeader()
    {
        // Act
        var operations = await ReadOperationsAsync();

        // Assert
        operations.Where(o => !SessionPaths.Contains(o.Path) && HeaderOf(o.Operation) is not null)
            .Select(o => $"{o.Method} {o.Path}").ShouldBeEmpty("only the three session operations read the refresh token header");
    }
}
