using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantManagement.Modules.Identity.Data;
using RestaurantManagement.Modules.Identity.Entities.Users;
using Shouldly;
using Xunit;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// PUT api/users/me/language (signed in, no organization needed) end to end on a real PostgreSQL
/// database. The user comes only from the access token. Maria and João are both verified users.
/// </summary>
public class UserLanguageEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string LanguageUrl = "/api/users/me/language";

    private static readonly Guid MariaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid JoaoId = Guid.Parse("22222222-bbbb-4bbb-8bbb-00000000000b");

    private readonly PostgresDatabaseFixture _postgres;

    public UserLanguageEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private static async Task SeedUserAsync(TenantApiFactory factory, Guid userId, string email, string firstName)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Massa@Fresca2026"),
            FirstName = firstName,
            LastName = "Souza",
            IsVerified = true
        });
        await db.SaveChangesAsync();
    }

    private static async Task<string?> ReadLanguageAsync(TenantApiFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.AsNoTracking().SingleAsync(u => u.Id == userId);
        return user.PreferredLanguage;
    }

    private static HttpRequestMessage RequestAsMaria(TenantApiFactory factory, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, LanguageUrl) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestAccessTokens.Create(factory.Services, MariaId, organizationId: null));
        return request;
    }

    [Fact]
    [Trait("Rule", "T137")]
    public async Task UpdateLanguage_NoAccessToken_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = await TenantApiFactory.CreateAsync(_postgres);
        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(LanguageUrl, new { language = "pt" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "choosing a language requires being signed in");
    }

    [Fact]
    [Trait("Rule", "T136")]
    public async Task UpdateLanguage_UnsupportedLanguage_ReturnsBadRequestAndStoresNothing()
    {
        // Arrange
        await using var factory = await TenantApiFactory.CreateAsync(_postgres);
        await SeedUserAsync(factory, MariaId, "maria@cantinadanonna.com.br", "Maria");
        var client = factory.CreateClient();

        // Act
        var response = await client.SendAsync(RequestAsMaria(factory, new { language = "de" }));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, "a language other than en, pt or fr is refused");
        (await ReadLanguageAsync(factory, MariaId)).ShouldBeNull("a refused language leaves the user without a stored language");
    }

    [Fact]
    [Trait("Rule", "T135")]
    public async Task UpdateLanguage_SupportedLanguage_StoresItAndReturnsNoContent()
    {
        // Arrange
        await using var factory = await TenantApiFactory.CreateAsync(_postgres);
        await SeedUserAsync(factory, MariaId, "maria@cantinadanonna.com.br", "Maria");
        var client = factory.CreateClient();

        // Act
        var response = await client.SendAsync(RequestAsMaria(factory, new { language = "fr" }));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, "a valid choice is saved with no body");
        (await ReadLanguageAsync(factory, MariaId)).ShouldBe("fr", "the language is stored on the signed-in user");
    }

    [Fact]
    [Trait("Rule", "T138")]
    public async Task UpdateLanguage_BodyNamesAnotherUser_ChangesOnlyTheCaller()
    {
        // Arrange
        await using var factory = await TenantApiFactory.CreateAsync(_postgres);
        await SeedUserAsync(factory, MariaId, "maria@cantinadanonna.com.br", "Maria");
        await SeedUserAsync(factory, JoaoId, "joao@cantinadanonna.com.br", "João");
        var client = factory.CreateClient();

        // Act
        var response = await client.SendAsync(RequestAsMaria(factory, new { userId = JoaoId, language = "pt" }));

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue("the caller's own choice is accepted");
        (await ReadLanguageAsync(factory, MariaId)).ShouldBe("pt", "the signed-in caller's language is the one that changes");
        (await ReadLanguageAsync(factory, JoaoId)).ShouldBeNull("a user ID in the request body must be ignored, so another user's language stays unchanged");
    }
}
