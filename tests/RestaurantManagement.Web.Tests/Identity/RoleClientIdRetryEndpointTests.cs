using System.Net;
using System.Net.Http.Json;
using Shouldly;
using static RestaurantManagement.Web.Tests.InvitationWebSupport;
using static RestaurantManagement.Web.Tests.LocalizedResponses;
using static RestaurantManagement.Web.Tests.RoleWebSupport;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// A retried <c>POST api/v1/roles</c> (same ID, same name) after the first attempt was stored.
/// Ana is the Owner of Cantina da Nonna.
/// </summary>
public class RoleClientIdRetryEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid RetriedRoleId = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e66");

    private readonly PostgresDatabaseFixture _postgres;

    public RoleClientIdRetryEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    [Fact]
    [Trait("Rule", "T158")]
    public async Task CreateRole_RetriedWithTheSameIdAndName_ReturnsIdConflictAndCreatesNothingNew()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);
        var body = new { id = RetriedRoleId, name = "Marketing", levels = new { Menus = "Manage" } };
        (await client.PostAsJsonAsync("/api/v1/roles", body)).StatusCode.ShouldBe(HttpStatusCode.OK, "the first attempt is stored");

        // Act
        var retry = await client.PostAsJsonAsync("/api/v1/roles", body);

        // Assert
        retry.StatusCode.ShouldBe(HttpStatusCode.Conflict, "the retry is a reused ID (409), not a taken-name error (400)");
        (await CodeAsync(retry)).ShouldBe("id.already_used", "the refusal carries the reused-ID code");
        (await RoleNamesAsync(factory, CantinaId)).Count(n => n == "Marketing").ShouldBe(1, "the retry creates no second role");
        (await RoleLevelsAsync(factory, CantinaId, RetriedRoleId)).ShouldBe(new List<string> { "Menus:Manage" }, "the first role keeps its levels");
    }
}
