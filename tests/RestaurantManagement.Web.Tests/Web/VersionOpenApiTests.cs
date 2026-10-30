using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The OpenAPI document (served in Development) shows the `version` of every editable record and the
/// `version` query parameter of every delete, so the frontend's generated client sends it back.
/// </summary>
public class VersionOpenApiTests : IDisposable
{
    private const string SwaggerDocumentUrl = "/swagger/v1/swagger.json";

    private static readonly string[] SchemasWithVersion =
    {
        "MenuResponse", "EditMenuRequest", "MenuCategoryResponse", "UpdateMenuCategoryRequest",
        "LocationResponse", "EditLocationRequest", "OrganizationResponse", "EditOrganizationRequest",
        "RoleResponse", "UpdateRoleLevelsRequest", "RenameRoleRequest"
    };

    private static readonly string[] RequestsRequiringVersion =
    {
        "EditMenuRequest", "UpdateMenuCategoryRequest", "EditLocationRequest", "EditOrganizationRequest", "UpdateRoleLevelsRequest", "RenameRoleRequest"
    };

    private static readonly string[] DeletePathsWithVersion =
    {
        "/api/v1/menus/{menuId}", "/api/v1/menu-categories/{id}", "/api/v1/locations/{locationId}"
    };

    private readonly List<IDisposable> _disposables = new();

    public void Dispose() => _disposables.ForEach(d => d.Dispose());

    [Fact]
    [Trait("Rule", "WD12")]
    public async Task SwaggerDocument_InDevelopment_ShowsVersionOnEditableRecordsAndOnEveryDelete()
    {
        // Arrange
        var factory = new AuthApiFactory().WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Development));
        _disposables.Add(factory);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        // Act
        var document = JsonNode.Parse(await client.GetStringAsync(SwaggerDocumentUrl))!;

        // Assert
        var schemas = document["components"]!["schemas"]!;
        var schemasMissingVersion = SchemasWithVersion.Where(name => schemas[name]?["properties"]?["version"] is null).ToList();
        schemasMissingVersion.ShouldBeEmpty("every editable record's response and update request shows its version");
        var requestsNotRequiringVersion = RequestsRequiringVersion.Where(name =>
            schemas[name]?["required"]?.AsArray().Any(r => r?.GetValue<string>() == "version") != true).ToList();
        requestsNotRequiringVersion.ShouldBeEmpty("every update request marks the version as required");
        var paths = document["paths"]!;
        var deletesMissingVersion = DeletePathsWithVersion.Where(path =>
            paths[path]?["delete"]?["parameters"]?.AsArray().Any(p => p?["name"]?.GetValue<string>() == "version" && p["in"]?.GetValue<string>() == "query" && p["required"]?.GetValue<bool>() == true) != true).ToList();
        deletesMissingVersion.ShouldBeEmpty("every delete shows the version query parameter as required");
    }
}
