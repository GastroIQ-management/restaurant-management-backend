using System.Net;
using System.Text.Json.Nodes;
using RestaurantManagement.Shared.Auth;
using RestaurantManagement.Shared.Services.AuditLog;
using Shouldly;
using static RestaurantManagement.Web.Tests.InvitationWebSupport;
using static RestaurantManagement.Web.Tests.RoleWebSupport;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Role rename and levels update need the version the caller read (lost-update check) on a real
/// PostgreSQL database. Ana is the Owner of Cantina da Nonna; Maria is an Admin without Cashier, so
/// the role rules refuse her on the Cashier role; Trattoria do Sol has its own Sommelier role. A
/// role's version stands for the role and its levels.
/// </summary>
public class RoleVersionEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _postgres;

    public RoleVersionEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private static string WithVersion(string json, uint version)
    {
        var body = JsonNode.Parse(json)!;
        body["version"] = version;
        return body.ToJsonString();
    }

    private static async Task<int> EditAuditEntriesAsync(TenantApiFactory factory)
        => (await AuditEntriesAsync(factory, CantinaId, AuditActions.RoleRenamed)).Count
            + (await AuditEntriesAsync(factory, CantinaId, AuditActions.RoleLevelsChanged)).Count;

    [Theory]
    [InlineData("levels", """{"levels":{"Orders":"Read"}}""")]
    [InlineData("name", """{"name":"Server"}""")]
    [Trait("Rule", "T159")]
    public async Task EditRole_VersionMissing_ReturnsBadRequestOnTheVersionFieldAndChangesNothing(string route, string body)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var waiterId = await RoleIdAsync(factory, CantinaId, "Waiter");
        var levelsBefore = await RoleLevelsAsync(factory, CantinaId, waiterId);

        // Act
        var response = await factory.ClientFor(AnaId, CantinaId).PutAsync($"/api/v1/roles/{waiterId}/{route}", Json(body));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "an edit that does not say which version it read is refused");
        (await response.ReadErrorFieldsAsync()).ShouldContain("version", "the answer names the version field");
        (await RoleNamesAsync(factory, CantinaId)).ShouldContain("Waiter", "the name is unchanged");
        (await RoleLevelsAsync(factory, CantinaId, waiterId)).ShouldBe(levelsBefore, ignoreOrder: false, customMessage: "the levels are unchanged");
    }

    [Theory]
    [InlineData("levels", """{"levels":{"Orders":"Manage"}}""")]
    [InlineData("name", """{"name":"Server"}""")]
    [InlineData("name", """{"name":"Waiter"}""")]
    [Trait("Rule", "T160")]
    public async Task EditRole_VersionOfAnEarlierRead_ReturnsConflictWithTheCurrentRoleAndAppliesNothing(string route, string staleBody)
    {
        // Arrange: a first levels update with the version both editors read changes the role's version
        await using var factory = await CreateFactoryAsync(_postgres);
        var waiterId = await RoleIdAsync(factory, CantinaId, "Waiter");
        var owner = factory.ClientFor(AnaId, CantinaId);
        var roleUrl = $"/api/v1/roles/{waiterId}";
        var staleVersion = await owner.ReadVersionAsync(roleUrl);
        (await owner.PutAsync($"{roleUrl}/levels", Json(WithVersion("""{"levels":{"Orders":"Read"}}""", staleVersion)))).EnsureSuccessStatusCode();
        var currentVersion = await owner.ReadVersionAsync(roleUrl);
        var levelsBefore = await RoleLevelsAsync(factory, CantinaId, waiterId);
        var auditBefore = await EditAuditEntriesAsync(factory);

        // Act
        var response = await owner.PutAsync($"{roleUrl}/{route}", Json(WithVersion(staleBody, staleVersion)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, "an edit sent with an old version must never be applied, a same-name rename included");
        var body = await response.ReadBodyAsync();
        body.GetProperty("code").GetString().ShouldBe("version.conflict", "the refusal has its own stable code");
        var current = body.GetProperty("current");
        current.GetProperty("levels").GetProperty("Orders").GetString().ShouldBe("Read", "the answer carries the role with its levels as stored now");
        current.GetProperty("version").GetUInt32().ShouldBe(currentVersion, "the carried role has its current version");
        (await RoleNamesAsync(factory, CantinaId)).ShouldContain("Waiter", "the name is unchanged");
        (await RoleLevelsAsync(factory, CantinaId, waiterId)).ShouldBe(levelsBefore, ignoreOrder: false, customMessage: "the levels are unchanged");
        (await EditAuditEntriesAsync(factory)).ShouldBe(auditBefore, "a refused edit writes no audit entry");
    }

    [Theory]
    [InlineData("levels", """{"levels":{"Orders":"Read"}}""")]
    [InlineData("name", """{"name":"Server"}""")]
    [Trait("Rule", "T161")]
    public async Task EditRole_CurrentVersion_SavesAndReturnsANewVersion(string route, string body)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var waiterId = await RoleIdAsync(factory, CantinaId, "Waiter");
        var owner = factory.ClientFor(AnaId, CantinaId);
        var version = await owner.ReadVersionAsync($"/api/v1/roles/{waiterId}");

        // Act
        var response = await owner.PutAsync($"/api/v1/roles/{waiterId}/{route}", Json(WithVersion(body, version)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "the current version may edit the role");
        var newVersion = (await response.ReadBodyAsync()).GetProperty("version").GetUInt32();
        newVersion.ShouldNotBe(version, "a change to the levels alone still gives the role a new version");
        (await owner.ReadVersionAsync($"/api/v1/roles/{waiterId}")).ShouldBe(newVersion, "the version returned is the stored one");
    }

    [Theory]
    [InlineData("levels", """{"levels":{"Orders":"Read"}}""")]
    [InlineData("name", """{"name":"Server"}""")]
    [Trait("Rule", "T162")]
    public async Task EditRole_RoleOfAnotherOrganizationWithItsRealVersion_ReturnsNotFoundAndRevealsNothing(string route, string body)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var sommelierId = await RoleIdAsync(factory, TrattoriaId, "Sommelier");
        var realVersion = await factory.ClientFor(TerezaId, TrattoriaId).ReadVersionAsync($"/api/v1/roles/{sommelierId}");

        // Act
        var response = await factory.ClientFor(AnaId, CantinaId).PutAsync($"/api/v1/roles/{sommelierId}/{route}", Json(WithVersion(body, realVersion)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "another organization's role is not found, even with its real version, never a version conflict");
        var answer = await response.ReadBodyAsync();
        answer.GetProperty("code").GetString().ShouldBe("role.not_found", "the answer is the ordinary not-found");
        answer.TryGetProperty("current", out _).ShouldBeFalse("nothing about the other organization's role is in the body");
        answer.GetRawText().ShouldNotContain("Sommelier", customMessage: "the other organization's role name never reaches the caller");
    }

    [Theory]
    [InlineData("levels", """{"levels":{"Orders":"Read"}}""")]
    [InlineData("name", """{"name":"Till"}""")]
    [Trait("Rule", "T163")]
    public async Task EditRole_AdminTheRoleRulesRefuseSendingAStaleVersion_ReturnsForbiddenNotConflict(string route, string body)
    {
        // Arrange: Maria holds no Cashier level, so the role rules refuse her on the Cashier role
        await using var factory = await CreateFactoryAsync(_postgres);
        await MakeMariaAdminAtBothLocationsAsync(factory);
        var cashierId = await RoleIdAsync(factory, CantinaId, "Cashier");
        var roleUrl = $"/api/v1/roles/{cashierId}";
        var owner = factory.ClientFor(AnaId, CantinaId);
        var staleVersion = await owner.ReadVersionAsync(roleUrl);
        (await owner.PutAsync($"{roleUrl}/name", Json(WithVersion("""{"name":"Caixa"}""", staleVersion)))).EnsureSuccessStatusCode();

        // Act
        var response = await factory.ClientFor(MariaId, CantinaId).PutAsync($"{roleUrl}/{route}", Json(WithVersion(body, staleVersion)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "the role rules come before the version check");
        (await response.ReadBodyAsync()).TryGetProperty("current", out _).ShouldBeFalse("a caller the rules refuse is never handed the current role");
    }

    [Fact]
    [Trait("Rule", "T164")]
    public async Task RoleResponses_ListAndGet_CarryTheSameVersion()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var waiterId = await RoleIdAsync(factory, CantinaId, "Waiter");
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var fromList = await client.ReadVersionAsync("/api/v1/roles", waiterId);
        var fromGet = await client.ReadVersionAsync($"/api/v1/roles/{waiterId}");

        // Assert
        fromGet.ShouldBe(fromList, "the list and the single role carry the same stored version");
    }
}
