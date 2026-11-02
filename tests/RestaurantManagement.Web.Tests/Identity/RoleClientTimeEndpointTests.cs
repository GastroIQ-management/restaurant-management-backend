using System.Net;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Identity.Data;
using RestaurantManagement.Shared.Services.ClientTime;
using Shouldly;
using static RestaurantManagement.Web.Tests.ClientTimeWebSupport;
using static RestaurantManagement.Web.Tests.InvitationWebSupport;
using static RestaurantManagement.Web.Tests.LocalizedResponses;
using static RestaurantManagement.Web.Tests.RoleWebSupport;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Identity writes carry the client times. Ana is the Owner of Cantina da Nonna. Session actions
/// (<c>select-organization</c>, <c>logout-all</c>, <c>change-password</c>) are signed-in writes too.
/// </summary>
public class RoleClientTimeEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string MarketingBody = """{"id":"01933b2e-7c10-7a55-9d3e-5b1f0c2a4ec1","name":"Marketing","levels":{"Menus":"Manage","Reports":"Read"}}""";

    private readonly PostgresDatabaseFixture _postgres;

    public RoleClientTimeEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    // Failure first: spec Outcome 6 for a session action
    [Theory]
    [InlineData("/api/v1/auth/select-organization")]
    [InlineData("/api/v1/auth/logout-all")]
    [InlineData("/api/v1/auth/change-password")]
    [Trait("Rule", "S13")]
    public async Task SessionAction_NoDeviceTimeHeaders_ReturnsBadRequest(string url)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);
        client.DefaultRequestHeaders.Remove(ClientTimeHeaders.ActionTime);
        client.DefaultRequestHeaders.Remove(ClientTimeHeaders.SentAt);

        // Act
        var response = await client.PostAsync(url, Json("{}"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a session action is a signed-in write and needs both device times");
        (await CodeAsync(response)).ShouldBe("client_time.required", "the refusal carries the required code");
    }

    // Outcome 8
    [Fact]
    [Trait("Rule", "T168")]
    public async Task CreateRole_SignedInWriteThreeHoursOld_StoresTheTimesOnTheNewRow()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.SendAsync(PostActedAgo("/api/v1/roles", MarketingBody, TimeSpan.FromHours(3)));

        // Assert
        response.EnsureSuccessStatusCode();
        var role = await factory.InOrganizationAsync<IdentityDbContext, RestaurantManagement.Modules.Identity.Entities.Roles.OrganizationRole>(CantinaId,
            db => db.OrganizationRoles.AsNoTracking().SingleAsync(r => r.Name == "Marketing"));
        role.CorrectedActionTime.ShouldNotBeNull("the new role stores the corrected action time").ShouldBe(DateTime.UtcNow.AddHours(-3), TimeSpan.FromMinutes(1), "it is when the user acted");
        role.ClockError.ShouldNotBeNull("the new role stores the clock error");
        role.ReceivedAt.ShouldNotBeNull("the new role stores the receive time");
    }
}
