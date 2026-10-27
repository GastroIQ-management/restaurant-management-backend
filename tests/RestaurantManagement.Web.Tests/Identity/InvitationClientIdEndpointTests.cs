using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using RestaurantManagement.Modules.Identity.Data;
using RestaurantManagement.Modules.Identity.Entities.Invitations;
using RestaurantManagement.Shared.Services.Email;
using Shouldly;
using static RestaurantManagement.Web.Tests.InvitationWebSupport;
using static RestaurantManagement.Web.Tests.LocalizedResponses;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// <c>POST api/v1/invitations</c> with an ID chosen by the client, on a real PostgreSQL database.
/// Ana is the Owner of Cantina da Nonna; Bia is a Waiter at Batel (no Members access); Tereza owns
/// Trattoria do Sol, which already has an open invitation for Paolo's colleague.
/// </summary>
public class InvitationClientIdEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid NewInvitationId = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e66");
    private static readonly Guid TrattoriaInvitationId = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e77");
    private const string CarlaEmail = "carla@exemplo.com.br";

    private readonly PostgresDatabaseFixture _postgres;

    public InvitationClientIdEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private static string InviteJson(string idPart) => $$"""
        { {{idPart}} "email": "{{BrunoEmail}}", "locations": [ { "locationId": "{{BatelId}}", "roleNames": [ "Waiter" ], "levels": {} } ] }
        """;

    private static Task AddTrattoriaInvitationAsync(TenantApiFactory factory)
        => factory.InOrganizationAsync<IdentityDbContext, bool>(TrattoriaId, async db =>
        {
            db.Set<OrganizationInvitation>().Add(new OrganizationInvitation
            {
                Id = TrattoriaInvitationId,
                OrganizationId = TrattoriaId,
                Email = CarlaEmail,
                InvitedByUserId = TerezaId,
                Language = "en",
                TokenHash = Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await db.SaveChangesAsync();
            return true;
        });

    private static int EmailsSentTo(TenantApiFactory factory, string email)
        => factory.EmailService.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IEmailService.SendEmailAsync))
            .Count(c => (string)c.GetArguments()[0]! == email);

    [Theory]
    [InlineData("")]
    [InlineData("\"id\": null,")]
    [InlineData("\"id\": \"convite\",")]
    [InlineData("\"id\": \"00000000-0000-0000-0000-000000000000\",")]
    [Trait("Rule", "T153")]
    public async Task CreateInvitation_IdMissingNullMalformedOrEmpty_ReturnsBadRequestOnIdAndSendsNothing(string idPart)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.SendAsync(Request("POST", "/api/v1/invitations", "en", InviteJson(idPart)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "an invitation without a usable ID (missing, null, not an ID, all zeros) is refused");
        (await FieldErrorsAsync(response, "Id")).ShouldContain("The Id field is required.", "the error is on the ID field");
        (await InvitationsAsync(factory, CantinaId)).ShouldBeEmpty("the refused invitation is not stored");
        EmailsSentTo(factory, BrunoEmail).ShouldBe(0, "no email is sent for a refused invitation");
    }

    [Fact]
    [Trait("Rule", "T154")]
    public async Task CreateInvitation_RetriedWithTheSameId_ReturnsConflictKeepsTheFirstOpenAndSendsNoSecondEmail()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);
        (await client.PostAsJsonAsync("/api/v1/invitations", InviteBody(BrunoEmail, BatelId, new[] { "Waiter" }, id: NewInvitationId))).EnsureSuccessStatusCode();

        // Act
        var retry = await client.PostAsJsonAsync("/api/v1/invitations", InviteBody(BrunoEmail, BatelId, new[] { "Waiter" }, id: NewInvitationId));

        // Assert
        retry.StatusCode.ShouldBe(HttpStatusCode.Conflict, "a retried create with the same ID is refused with 409, never a 500");
        (await CodeAsync(retry)).ShouldBe("id.already_used", "the refusal carries the reused-ID code");
        var stored = (await InvitationsAsync(factory, CantinaId)).ShouldHaveSingleItem("only the first invitation exists");
        stored.Id.ShouldBe(NewInvitationId, "the first invitation keeps the client's ID");
        stored.ReplacedAt.ShouldBeNull("the refused retry does not replace the first invitation, which stays open");
        EmailsSentTo(factory, BrunoEmail).ShouldBe(1, "the refused retry sends no second email");
    }

    [Fact]
    [Trait("Rule", "T155")]
    public async Task CreateInvitation_IdUsedByAnotherOrganization_AnswersTheSameConflictAndRevealsNothing()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        await AddTrattoriaInvitationAsync(factory);
        var client = factory.ClientFor(AnaId, CantinaId);
        (await client.PostAsJsonAsync("/api/v1/invitations", InviteBody(BrunoEmail, BatelId, new[] { "Waiter" }, id: NewInvitationId))).EnsureSuccessStatusCode();
        var sameOrganization = await client.PostAsJsonAsync("/api/v1/invitations", InviteBody(BrunoEmail, BatelId, new[] { "Waiter" }, id: NewInvitationId));

        // Act
        var otherOrganization = await client.PostAsJsonAsync("/api/v1/invitations", InviteBody(BrunoEmail, BatelId, new[] { "Waiter" }, id: TrattoriaInvitationId));

        // Assert
        otherOrganization.StatusCode.ShouldBe(sameOrganization.StatusCode, "a clash with another organization has the same status as a clash in the own one");
        (await ProblemWithoutTraceIdAsync(otherOrganization)).ShouldBe(await ProblemWithoutTraceIdAsync(sameOrganization),
            "title and code are identical, so nobody can tell whose invitation holds the ID");
        var body = await otherOrganization.Content.ReadAsStringAsync();
        body.ShouldNotContain(CarlaEmail, Case.Insensitive, "the body holds no email of the other organization's invitation");
        body.ShouldNotContain(TrattoriaId.ToString(), Case.Insensitive, "the body holds no ID of the other organization");
        var untouched = (await InvitationsAsync(factory, TrattoriaId)).ShouldHaveSingleItem("the other organization still has its one invitation");
        untouched.ReplacedAt.ShouldBeNull("the other organization's invitation is unchanged");
    }

    [Fact]
    [Trait("Rule", "T156")]
    public async Task CreateInvitation_MemberWithoutMembersManageSendsTakenId_ReturnsForbiddenNotConflict()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        await AddTrattoriaInvitationAsync(factory);
        var client = factory.ClientFor(BiaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/invitations", InviteBody(BrunoEmail, BatelId, new[] { "Waiter" }, id: TrattoriaInvitationId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "the delegation rules are checked before the ID, so a caller without Members Manage never learns whether an ID is taken");
    }

    [Fact]
    [Trait("Rule", "T157")]
    public async Task CreateInvitation_NewId_StoresInvitationUnderThatId()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/invitations", new
        {
            id = NewInvitationId,
            organizationId = TrattoriaId,
            email = BrunoEmail,
            locations = new[] { new { locationId = BatelId, roleNames = new[] { "Waiter" }, levels = new Dictionary<string, string>() } }
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK, "the Owner invites with an ID of the client's choice");
        (await InvitationsAsync(factory, TrattoriaId)).ShouldBeEmpty("an organization named in the body is ignored: the invitation belongs to the token organization");
        (await response.Content.ReadFromJsonAsync<InvitationReply>())!.Id.ShouldBe(NewInvitationId, "the response carries the ID the client chose");
        var stored = (await InvitationsAsync(factory, CantinaId)).ShouldHaveSingleItem();
        stored.Id.ShouldBe(NewInvitationId, "the invitation is stored under the client's ID");
        LatestTokenSentTo(factory, BrunoEmail).ShouldNotContain(NewInvitationId.ToString(), customMessage: "the accept link token is separate from the invitation ID");
    }
}
