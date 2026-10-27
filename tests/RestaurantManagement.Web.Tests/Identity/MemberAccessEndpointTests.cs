using System.Net;
using System.Net.Http.Json;
using RestaurantManagement.Shared.Auth;
using RestaurantManagement.Shared.Services.AuditLog;
using Shouldly;
using static RestaurantManagement.Web.Tests.InvitationWebSupport;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// <c>PUT api/v1/members/{userId}/access/{locationId}</c> on a real PostgreSQL database. Ana (Owner) edits Bia, a
/// Waiter at Batel; Maria is an Admin with Members = Manage at Batel and no Cashier level.
/// </summary>
public class MemberAccessEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _postgres;

    public MemberAccessEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private static string AccessUrl(Guid userId, Guid locationId) => $"/api/v1/members/{userId}/access/{locationId}";

    [Fact]
    [Trait("Rule", "T51")]
    public async Task SetAccess_OwnerChangesAWaiterToCashierAtBatel_UpdatesGridAndLabelAndWritesOneAuditEntry()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PutAsJsonAsync(AccessUrl(BiaId, BatelId), new { roleNames = new[] { "Cashier" } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await CellsAsync(factory, CantinaId, BiaId, BatelId)).ShouldBe(Cells(CashierLevels), "Bia now has the Cashier levels at Batel");
        var entries = await AuditEntriesAsync(factory, CantinaId, AuditActions.MemberAccessChanged);
        var entry = entries.ShouldHaveSingleItem("one grid change writes exactly one audit entry");
        entry.Before.ShouldContain("Waiter", customMessage: "the entry records the role before the change");
        entry.After.ShouldContain("Cashier", customMessage: "the entry records the role after the change");
    }

    [Fact]
    [Trait("Rule", "T52")]
    public async Task SetAccess_MenusLoweredToNone_TheMembersNextMenuReadWithTheSameTokenIsForbidden()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var bia = factory.ClientFor(BiaId, CantinaId);
        var before = await bia.GetAsync($"/api/v1/menus/location/{BatelId}");
        var ana = factory.ClientFor(AnaId, CantinaId);

        // Act
        var change = await ana.PutAsJsonAsync(AccessUrl(BiaId, BatelId),
            new { roleNames = new[] { "Waiter" }, levels = new Dictionary<string, string> { ["Menus"] = "None" } });
        var after = await bia.GetAsync($"/api/v1/menus/location/{BatelId}");

        // Assert
        before.StatusCode.ShouldBe(HttpStatusCode.OK);
        change.StatusCode.ShouldBe(HttpStatusCode.OK);
        after.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "a lowered level applies on the next request, with no new login");
    }

    [Fact]
    [Trait("Rule", "T53")]
    public async Task SetAccess_AdminWithoutCashierGivesBiaTheCashierPreset_ReturnsForbiddenAndStoresNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(MariaId, CantinaId);

        // Act
        var response = await client.PutAsJsonAsync(AccessUrl(BiaId, BatelId), new { roleNames = new[] { "Cashier" } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "an Admin cannot grant a level above her own at that location");
        (await CellsAsync(factory, CantinaId, BiaId, BatelId)).ShouldBe(Cells(WaiterLevels), "the refused change leaves Bia's grid as it was");
    }

    [Fact]
    [Trait("Rule", "T54")]
    public async Task SetAccess_MemberOfAnotherOrganization_ReturnsNotFoundAndLeavesTheirGridUnchanged()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PutAsJsonAsync(AccessUrl(PaoloId, BatelId), new { roleNames = new[] { "Waiter" } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, "another organization's member is answered as if they did not exist");
        (await CellsAsync(factory, TrattoriaId, PaoloId, BatelId)).ShouldBeEmpty("nothing is stored for the other organization's member");
        (await CellsAsync(factory, TrattoriaId, PaoloId, CentroId)).ShouldBe(Cells((AccessModule.Menus, AccessLevel.Read)), "their grid is unchanged");
    }

    [Theory]
    [InlineData("Dessert", "Manage")]
    [InlineData("Menus", "Admin")]
    [Trait("Rule", "T55")]
    public async Task SetAccess_UnknownModuleOrLevelName_ReturnsBadRequestAndStoresNothing(string module, string level)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PutAsJsonAsync(AccessUrl(BiaId, BatelId),
            new { roleNames = new[] { "Waiter" }, levels = new Dictionary<string, string> { [module] = level } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a module or level that does not exist is a bad request");
        (await CellsAsync(factory, CantinaId, BiaId, BatelId)).ShouldBe(Cells(WaiterLevels), "nothing is stored for a bad request");
    }

    private const string MissingId = "99999999-9999-4999-8999-999999999999";

    public static TheoryData<string, string, string, string, string> OtherOrganizationAndMissingCases => new()
    {
        { AccessUrl(PaoloId, BatelId), "Waiter", AccessUrl(Guid.Parse(MissingId), BatelId), "Waiter", "member.not_found" },
        { AccessUrl(BiaId, CentroId), "Waiter", AccessUrl(BiaId, Guid.Parse(MissingId)), "Waiter", "location.not_found" },
        { AccessUrl(BiaId, BatelId), "Sommelier", AccessUrl(BiaId, BatelId), "Maitre", "role.not_found" }
    };

    [Theory]
    [MemberData(nameof(OtherOrganizationAndMissingCases))]
    [Trait("Rule", "T142")]
    public async Task SetAccess_MemberLocationOrRoleOfAnotherOrganization_AnswersTheSameStatusTitleAndCodeAsAMissingOne(
        string otherOrganizationUrl, string otherOrganizationRole, string missingUrl, string missingRole, string expectedCode)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var otherOrganization = await client.PutAsJsonAsync(otherOrganizationUrl, new { roleNames = new[] { otherOrganizationRole } });
        var missing = await client.PutAsJsonAsync(missingUrl, new { roleNames = new[] { missingRole } });

        // Assert
        otherOrganization.StatusCode.ShouldBe(HttpStatusCode.NotFound, "another organization's data is answered as if it did not exist");
        (await LocalizedResponses.CodeAsync(otherOrganization)).ShouldBe(expectedCode, "the code names what was not found");
        (await LocalizedResponses.CodeAsync(missing)).ShouldBe(expectedCode, "another organization's data has the same code as missing data, so nobody can tell it exists");
        (await LocalizedResponses.MessageAsync(otherOrganization)).ShouldBe(await LocalizedResponses.MessageAsync(missing), "the title is the same too");
    }

    [Fact]
    [Trait("Rule", "T143")]
    public async Task SetAccess_WaiterWithoutMembersManage_AnswersAccessDeniedCode()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(BiaId, CantinaId);

        // Act
        var response = await client.PutAsJsonAsync(AccessUrl(BiaId, BatelId), new { roleNames = new[] { "Cashier" } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "a waiter has no Members level");
        (await LocalizedResponses.CodeAsync(response)).ShouldBe("access.denied", "the refusal carries the access denied code");
    }
}
