using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Identity.Data;
using Shouldly;
using Xunit;
using static RestaurantManagement.Web.Tests.InvitationWebSupport;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// POST api/invitations picks the language of the email: the invited person's account language when they
/// have one, otherwise the Accept-Language of the inviter (English, Portuguese or French).
/// </summary>
public class InvitationEmailLanguageEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _postgres;

    public InvitationEmailLanguageEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private static async Task<HttpResponseMessage> InviteAsync(TenantApiFactory factory, string email, string acceptLanguage)
    {
        var client = factory.ClientFor(AnaId, CantinaId);
        client.DefaultRequestHeaders.Add("Accept-Language", acceptLanguage);
        return await client.PostAsJsonAsync("/api/invitations", InviteBody(email, BatelId, new[] { "Waiter" }));
    }

    [Theory]
    [InlineData("fr", "fr")]
    [InlineData("fr-CA", "fr")]
    [InlineData("pt-BR", "pt")]
    [InlineData("en-US", "en")]
    [InlineData("de", "en")]
    [Trait("Rule", "T139")]
    public async Task CreateInvitation_AcceptLanguageOfInviter_StoresTheSupportedLanguage(string acceptLanguage, string expected)
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);

        // Act
        var response = await InviteAsync(factory, BrunoEmail, acceptLanguage);

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        (await InvitationsAsync(factory, CantinaId)).ShouldHaveSingleItem().Language
            .ShouldBe(expected, "the inviter's Accept-Language picks en, pt or fr; anything else is English");
    }

    [Fact]
    [Trait("Rule", "T140")]
    public async Task CreateInvitation_InvitedPersonHasAnAccountWithALanguage_UsesTheAccountLanguage()
    {
        // Arrange
        await using var factory = await CreateFactoryAsync(_postgres);
        await factory.InOrganizationAsync<IdentityDbContext, bool>(CantinaId, async db =>
        {
            (await db.Users.SingleAsync(u => u.Id == RitaId)).PreferredLanguage = "pt";
            await db.SaveChangesAsync();
            return true;
        });

        // Act
        var response = await InviteAsync(factory, RitaEmail, "fr");

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        (await InvitationsAsync(factory, CantinaId)).ShouldHaveSingleItem().Language
            .ShouldBe("pt", "Rita's own language wins over the inviter's Accept-Language");
    }
}
