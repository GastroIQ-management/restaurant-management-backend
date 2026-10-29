using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>The OpenAPI document tells generated clients that every create request carries a required ID.</summary>
public class ClientIdOpenApiTests : IDisposable
{
    private readonly AuthApiFactory _factory;

    public ClientIdOpenApiTests()
        => _factory = new AuthApiFactory();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("CreateMenuRequest")]
    [InlineData("CreateMenuCategoryRequest")]
    [InlineData("CreateLocationRequest")]
    [InlineData("CreateOrganizationRequest")]
    [InlineData("CreateRoleRequest")]
    [InlineData("CreateInvitationRequest")]
    [Trait("Rule", "WD11")]
    public async Task V1Document_CreateRequest_ShowsIdAsRequiredUuid(string schemaName)
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Development));
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        // Act
        var document = JsonNode.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"))!;

        // Assert
        var schema = document["components"]!["schemas"]![schemaName]!;
        schema["properties"]!["id"]!["type"]!.GetValue<string>().ShouldBe("string", "the ID is sent as text");
        schema["properties"]!["id"]!["format"]!.GetValue<string>().ShouldBe("uuid", "the ID is a UUID");
        (schema["required"]?.AsArray() ?? new JsonArray()).Select(n => n!.GetValue<string>()).ShouldContain("id", "generated clients must send the ID on every create");
    }
}
