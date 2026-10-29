using System.Net;
using System.Net.Http.Json;
using RestaurantManagement.Shared.Services.AuditLog;
using Shouldly;
using static RestaurantManagement.Web.Tests.InvitationWebSupport;
using static RestaurantManagement.Web.Tests.LocalizedResponses;
using static RestaurantManagement.Web.Tests.RoleWebSupport;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// <c>POST api/v1/roles</c> with an ID chosen by the client, on a real PostgreSQL database.
/// Ana is the Owner of Cantina da Nonna; Bia is a Waiter at Batel (no Members access); Trattoria do Sol
/// has its own role Sommelier. Both organizations have the five default roles.
/// </summary>
public class RoleClientIdEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid NewRoleId = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e55");

    private readonly PostgresDatabaseFixture _postgres;

    public RoleClientIdEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    [Theory]
    [InlineData("")]
    [InlineData("\"id\": null,")]
    [InlineData("\"id\": \"marketing\",")]
    [InlineData("\"id\": \"00000000-0000-0000-0000-000000000000\",")]
    [Trait("Rule", "T148")]
    public async Task CreateRole_IdMissingNullMalformedOrEmpty_ReturnsBadRequestOnIdAndStoresNothing(string idPart)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.SendAsync(Request("POST", "/api/v1/roles", "en", "{ " + idPart + " \"name\": \"Marketing\", \"levels\": { \"Menus\": \"Manage\" } }"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a role without a usable ID (missing, null, not an ID, all zeros) is refused");
        (await FieldErrorsAsync(response, "Id")).ShouldContain("The Id field is required.", "the error is on the ID field");
        (await RoleNamesAsync(factory, CantinaId)).ShouldNotContain("Marketing", "the refused role is not saved");
    }

    [Fact]
    [Trait("Rule", "T149")]
    public async Task CreateRole_IdUsedInSameOrganization_ReturnsConflictAndStoresNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var waiterId = await RoleIdAsync(factory, CantinaId, "Waiter");
        var levelsBefore = await RoleLevelsAsync(factory, CantinaId, waiterId);
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/roles", new { id = waiterId, name = "Marketing", levels = new { Menus = "Manage" } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, "a role ID already used in the organization is refused with 409");
        (await CodeAsync(response)).ShouldBe("id.already_used", "the refusal carries the reused-ID code");
        (await RoleNamesAsync(factory, CantinaId)).ShouldNotContain("Marketing", "the refused role is not saved");
        (await RoleLevelsAsync(factory, CantinaId, waiterId)).ShouldBe(levelsBefore, "the existing role keeps its levels: no level row of the refused role is stored");
        (await AuditEntriesAsync(factory, CantinaId, AuditActions.RoleCreated)).ShouldBeEmpty("a refused create writes no audit entry");
    }

    [Fact]
    [Trait("Rule", "T150")]
    public async Task CreateRole_IdUsedByAnotherOrganization_AnswersTheSameConflictAndRevealsNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var waiterId = await RoleIdAsync(factory, CantinaId, "Waiter");
        var sommelierId = await RoleIdAsync(factory, TrattoriaId, "Sommelier");
        var client = factory.ClientFor(AnaId, CantinaId);
        var sameOrganization = await client.PostAsJsonAsync("/api/v1/roles", new { id = waiterId, name = "Marketing" });

        // Act
        var otherOrganization = await client.PostAsJsonAsync("/api/v1/roles", new { id = sommelierId, name = "Marketing" });

        // Assert
        otherOrganization.StatusCode.ShouldBe(sameOrganization.StatusCode, "a clash with another organization has the same status as a clash in the own one");
        (await ProblemWithoutTraceIdAsync(otherOrganization)).ShouldBe(await ProblemWithoutTraceIdAsync(sameOrganization),
            "title and code are identical, so nobody can tell whose role holds the ID");
        var body = await otherOrganization.Content.ReadAsStringAsync();
        body.ShouldNotContain("Sommelier", Case.Insensitive, "the body holds no name of the other organization's role");
        body.ShouldNotContain(TrattoriaId.ToString(), Case.Insensitive, "the body holds no ID of the other organization");
        (await RoleNamesAsync(factory, TrattoriaId)).ShouldContain("Sommelier", "the other organization's role is unchanged");
    }

    [Fact]
    [Trait("Rule", "T151")]
    public async Task CreateRole_MemberWithoutMembersManageSendsTakenId_ReturnsForbiddenNotConflict()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var waiterId = await RoleIdAsync(factory, CantinaId, "Waiter");
        var client = factory.ClientFor(BiaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/roles", new { id = waiterId, name = "Marketing" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "the role rules are checked before the ID, so a caller without Members Manage never learns whether an ID is taken");
    }

    [Fact]
    [Trait("Rule", "T152")]
    public async Task CreateRole_NewId_StoresRoleUnderThatId()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/roles", new { id = NewRoleId, organizationId = TrattoriaId, name = "Marketing", levels = new { Menus = "Manage" } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "the Owner creates a role with an ID of the client's choice");
        (await response.Content.ReadFromJsonAsync<RoleReply>())!.Id.ShouldBe(NewRoleId, "the response carries the ID the client chose");
        (await RoleIdAsync(factory, CantinaId, "Marketing")).ShouldBe(NewRoleId, "the role is stored under the client's ID in the token organization, whatever organization the body names");
        (await RoleNamesAsync(factory, TrattoriaId)).ShouldNotContain("Marketing", "the organization named in the body gets nothing");
        (await RoleLevelsAsync(factory, CantinaId, NewRoleId)).ShouldBe(new List<string> { "Menus:Manage" }, "the role's levels are stored with it");
    }
}
