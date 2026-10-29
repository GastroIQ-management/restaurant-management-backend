using System.Net;
using System.Net.Http.Json;
using Shouldly;
using static RestaurantManagement.Web.Tests.InvitationWebSupport;

namespace RestaurantManagement.Web.Tests;

/// <summary><c>POST api/v1/invitations</c> refuses an empty location grid and a null location, and survives two requests for one address at once.</summary>
public class InvitationCreateRefusalEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _postgres;

    public InvitationCreateRefusalEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    [Fact]
    [Trait("Rule", "T87")]
    public async Task CreateInvitation_LocationWithNoRolesAndNoLevels_ReturnsBadRequestAndStoresNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/invitations", InviteBody(BrunoEmail, BatelId, Array.Empty<string>()));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "an invitation location must grant something, or a cancel could not find it");
        (await InvitationsAsync(factory, CantinaId)).ShouldBeEmpty("nothing is stored");
    }

    [Fact]
    [Trait("Rule", "T88")]
    public async Task CreateInvitation_NullLocationEntry_ReturnsBadRequestAndStoresNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/invitations", new { id = Guid.NewGuid(), email = BrunoEmail, locations = new object?[] { null } });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a null location is refused, not a server error");
        (await InvitationsAsync(factory, CantinaId)).ShouldBeEmpty("nothing is stored");
    }

    [Fact]
    [Trait("Rule", "T89")]
    public async Task CreateInvitation_SameEmailAtTheSameMoment_NeverFailsWithAServerErrorAndLeavesOneOpenInvitation()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        // Each request has its own ID: the race is about the open invitation, not about a reused ID.
        // Act
        var responses = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => factory.ClientFor(AnaId, CantinaId).PostAsJsonAsync("/api/v1/invitations", InviteBody(BrunoEmail, BatelId, new[] { "Waiter" }))));

        // Assert
        responses.ShouldAllBe(r => r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.Conflict, "the loser of the race gets a refusal, not a server error");
        responses.ShouldContain(r => r.IsSuccessStatusCode, "one request succeeds");
        (await InvitationsAsync(factory, CantinaId)).Count(i => i.UsedAt == null && i.CancelledAt == null && i.ReplacedAt == null)
            .ShouldBe(1, "only one open invitation exists for the address");
    }

    [Fact]
    [Trait("Rule", "T145")]
    public async Task CreateInvitation_SameEmailAtTheSameMoment_EveryLoserAnswersTheLastingConflictCode()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        // Each request has its own ID: the race is about the open invitation, not about a reused ID.
        // Act
        var responses = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => factory.ClientFor(AnaId, CantinaId).PostAsJsonAsync("/api/v1/invitations", InviteBody(BrunoEmail, BatelId, new[] { "Waiter" }))));

        // Assert
        var losers = responses.Where(r => !r.IsSuccessStatusCode).ToList();
        losers.ShouldAllBe(r => r.StatusCode == HttpStatusCode.Conflict, "the loser of the race gets a 409");
        var codes = await Task.WhenAll(losers.Select(LocalizedResponses.CodeAsync));
        codes.ShouldAllBe(code => code == "invitation.already_open", "the 409 is a lasting conflict, never a retry code and not validation.failed");
    }
}
