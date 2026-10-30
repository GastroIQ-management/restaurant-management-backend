using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RestaurantManagement.Shared.Services.Idempotency;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The host runs the idempotency filter for every signed-in write: the OpenAPI document shows the
/// optional `Idempotency-Key` header, startup checks the settings, and no module's writes are left out.
/// </summary>
public class IdempotencyKeyWiringTests : IDisposable
{
    private static readonly string[] WriteMethods = { "post", "put", "patch", "delete" };

    // Writes that need no sign-in, or that ignore the key because their answer carries a token or cookie.
    private static readonly string[] PublicWrites =
    {
        "/api/v1/auth/register", "/api/v1/auth/login", "/api/v1/auth/refresh", "/api/v1/auth/accept-invitation", "/api/v1/auth/logout",
        "/api/v1/auth/verify", "/api/v1/auth/resend-verification", "/api/v1/auth/forgot-password", "/api/v1/auth/reset-password"
    };

    private static readonly string[] SessionWrites = { "/api/v1/auth/select-organization", "/api/v1/auth/logout-all", "/api/v1/auth/change-password" };

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
            .FirstOrDefault(p => p!["name"]!.GetValue<string>() == IdempotencyKeyFilter.HeaderName && p["in"]!.GetValue<string>() == "header");

    [Fact]
    [Trait("Rule", "WD13")]
    public async Task V1Document_SignedInWrites_DeclareTheOptionalIdempotencyKeyHeaderAsUuid()
    {
        // Act
        var operations = await ReadOperationsAsync();

        // Assert
        var signedInWrites = operations.Where(o => WriteMethods.Contains(o.Method) && !PublicWrites.Concat(SessionWrites).Contains(o.Path)).ToList();
        signedInWrites.ShouldNotBeEmpty();
        signedInWrites.Where(o => HeaderOf(o.Operation) is not { } header
                || header["required"]?.GetValue<bool>() == true
                || header["schema"]?["format"]?.GetValue<string>() != "uuid"
                || string.IsNullOrWhiteSpace(header["description"]?.GetValue<string>()))
            .Select(o => $"{o.Method} {o.Path}").ShouldBeEmpty("every signed-in write shows an optional uuid Idempotency-Key header with its purpose");
    }

    [Fact]
    [Trait("Rule", "WD14")]
    public async Task V1Document_PublicWritesSessionActionsAndReads_DoNotDeclareTheIdempotencyKeyHeader()
    {
        // Act
        var operations = await ReadOperationsAsync();

        // Assert
        var notCovered = operations.Where(o => !WriteMethods.Contains(o.Method) || PublicWrites.Concat(SessionWrites).Contains(o.Path)).ToList();
        notCovered.ShouldNotBeEmpty();
        notCovered.Where(o => HeaderOf(o.Operation) is not null)
            .Select(o => $"{o.Method} {o.Path}").ShouldBeEmpty("the key is ignored on public writes, on the three session actions and on reads, so the document must not ask for it");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-8")]
    [Trait("Rule", "W32")]
    public void Startup_NonPositiveIdempotencyRetentionDays_FailsNamingIdempotencyRetentionDays(string retentionDays)
    {
        // Arrange
        var factory = new AuthApiFactory().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Idempotency:RetentionDays"] = retentionDays }));
        });
        _disposables.Add(factory);

        // Act
        var error = StartupFailure.Capture(factory);

        // Assert
        error.ToString().ShouldContain("Idempotency:RetentionDays", Case.Sensitive, "the app must refuse to start with a retention that is not positive, and name the setting to fix");
    }

    [Fact]
    [Trait("Rule", "WD15")]
    public void EveryEndpoint_SignedInWrite_BelongsToAModuleWithAKeyStoreOrIgnoresKeys()
    {
        // Arrange
        var factory = new AuthApiFactory();
        _disposables.Add(factory);
        var storeAssemblies = factory.Services.GetServices<IdempotencyStoreRegistration>().Select(r => r.ControllersAssembly).ToList();

        // Act
        var writes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Select(e => (Endpoint: e, Action: e.Metadata.GetMetadata<ControllerActionDescriptor>(), Methods: e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods))
            .Where(x => x.Action is not null && x.Methods!.Any(m => WriteMethods.Contains(m.ToLowerInvariant())))
            .Where(x => x.Endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null && x.Endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null)
            .Where(x => x.Endpoint.Metadata.GetMetadata<IgnoreIdempotencyKeyAttribute>() is null)
            .ToList();
        var left = writes.Where(x => !storeAssemblies.Contains(x.Action!.ControllerTypeInfo.Assembly))
            .Select(x => $"{string.Join("/", x.Methods!)} {x.Action!.AttributeRouteInfo?.Template}").ToList();

        // Assert
        writes.ShouldNotBeEmpty();
        left.ShouldBeEmpty("a signed-in write in a module with no key store (and not marked to ignore keys) would silently skip the key");
    }
}
