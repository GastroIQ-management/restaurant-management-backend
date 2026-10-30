using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using RestaurantManagement.Modules.Identity.Data;
using RestaurantManagement.Modules.Identity.Services.Sessions;
using RestaurantManagement.Shared.Data.Idempotency;
using RestaurantManagement.Shared.Services.Idempotency;
using RestaurantManagement.Shared.Services.Email;
using Shouldly;
using static RestaurantManagement.Web.Tests.InvitationWebSupport;

namespace RestaurantManagement.Web.Tests;

/// <summary>Identity writes with an Idempotency-Key, on a real PostgreSQL database (world of InvitationWebSupport).</summary>
public class IdentityIdempotencyEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid Key = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e11");

    private readonly PostgresDatabaseFixture _postgres;

    public IdentityIdempotencyEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private static HttpRequestMessage Post(string url, string? json = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (json is not null)
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Headers.Add(IdempotencyKeyFilter.HeaderName, Key.ToString());
        return request;
    }

    // Failure first
    [Fact]
    [Trait("Rule", "T165")]
    public async Task LogoutAll_SameKeyTwice_RunsBothTimesAndStoresNoKey()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);

        // Act
        var first = await client.SendAsync(Post("/api/v1/auth/logout-all"));
        var second = await client.SendAsync(Post("/api/v1/auth/logout-all"));

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        second.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await factory.SessionService.Received(2).LogoutAllAsync(AnaId);
        (await factory.InOrganizationAsync<IdentityDbContext, int>(CantinaId, db => db.Set<IdempotencyKey>().CountAsync()))
            .ShouldBe(0, "a session action ignores the key, so no token or cookie answer is stored or replayed");
    }

    [Fact]
    [Trait("Rule", "T166")]
    public async Task CreateInvitation_SameKeyTwice_StoresOneInvitationSendsOneEmailAndAnswersTheSame()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        var client = factory.ClientFor(AnaId, CantinaId);
        var body = System.Text.Json.JsonSerializer.Serialize(InviteBody(BrunoEmail, BatelId, new[] { "Waiter" }));
        var first = await client.SendAsync(Post("/api/v1/invitations", body));

        // Act
        var second = await client.SendAsync(Post("/api/v1/invitations", body));

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(first.StatusCode, "the replay keeps the status");
        (await second.Content.ReadAsStringAsync()).ShouldBe(await first.Content.ReadAsStringAsync(), "the replay returns the saved body");
        (await InvitationsAsync(factory, CantinaId)).ShouldHaveSingleItem("the invitation was stored once");
        factory.EmailService.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IEmailService.SendEmailAsync))
            .ShouldBe(1, "the email was sent once");
    }
}
