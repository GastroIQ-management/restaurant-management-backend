using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Identity.Entities.Users;
using RestaurantManagement.Modules.Identity.Models.Languages;
using RestaurantManagement.Modules.Identity.Services.Languages;
using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;
using Xunit;

namespace RestaurantManagement.Modules.Identity.Tests
{
    /// <summary>
    /// A signed-in user choosing the language of their emails. The user comes only from the token, so the
    /// service takes the user ID separately from the request. Maria is active and verified; Pedro is
    /// inactive; Lucia is unverified.
    /// </summary>
    public class UserLanguageServiceTests
    {
        private static readonly Guid MariaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
        private static readonly Guid PedroId = Guid.Parse("33333333-cccc-4ccc-8ccc-00000000000c");
        private static readonly Guid LuciaId = Guid.Parse("55555555-eeee-4eee-8eee-00000000000e");
        private static readonly Guid JoaoId = Guid.Parse("22222222-bbbb-4bbb-8bbb-00000000000b");

        private static async Task<IdentityTestDatabase> CreateDatabaseAsync()
        {
            var database = new IdentityTestDatabase();
            using var context = database.CreateContext();
            context.Users.AddRange(
                NewUser(MariaId, "maria@cantinadanonna.com.br", "Maria", isActive: true, isVerified: true),
                NewUser(PedroId, "pedro@cantinadanonna.com.br", "Pedro", isActive: false, isVerified: true),
                NewUser(LuciaId, "lucia@cantinadanonna.com.br", "Lucia", isActive: true, isVerified: false),
                NewUser(JoaoId, "joao@cantinadanonna.com.br", "João", isActive: true, isVerified: true));
            await context.SaveChangesAsync();
            return database;
        }

        private static User NewUser(Guid id, string email, string firstName, bool isActive, bool isVerified) => new()
        {
            Id = id,
            Email = email,
            PasswordHash = "hash",
            FirstName = firstName,
            LastName = "Souza",
            IsActive = isActive,
            IsVerified = isVerified
        };

        private static async Task UpdateAsync(IdentityTestDatabase database, Guid userId, string language)
        {
            using var context = database.CreateContext();
            var sut = new UserLanguageService(context, TestLocalizer.Create());
            await sut.UpdateAsync(userId, new UpdateLanguageRequest { Language = language });
        }

        private static async Task<string?> ReadLanguageAsync(IdentityTestDatabase database, Guid userId)
        {
            using var context = database.CreateContext();
            return (await context.Users.AsNoTracking().SingleAsync(u => u.Id == userId)).PreferredLanguage;
        }

        [Theory]
        [InlineData("99999999-9999-4999-8999-999999999999")]   // no such user
        [InlineData("33333333-cccc-4ccc-8ccc-00000000000c")]   // Pedro, inactive
        [InlineData("55555555-eeee-4eee-8eee-00000000000e")]   // Lucia, email not verified
        [Trait("Rule", "T134")]
        public async Task UpdateAsync_UnknownInactiveOrUnverifiedUser_RejectedAsInvalidSession(string userId)
        {
            // Arrange
            using var database = await CreateDatabaseAsync();

            // Act
            var rejection = await Should.ThrowAsync<BusinessException>(() => UpdateAsync(database, Guid.Parse(userId), "pt"));

            // Assert
            rejection.StatusCode.ShouldBe(401, "a missing, deactivated or unverified user cannot choose a language and gets the invalid-session 401");
        }

        [Theory]
        [InlineData("en")]
        [InlineData("pt")]
        [InlineData("fr")]
        [Trait("Rule", "T135")]
        public async Task UpdateAsync_SupportedLanguage_StoresItForThatUserOnly(string language)
        {
            // Arrange
            using var database = await CreateDatabaseAsync();

            // Act
            await UpdateAsync(database, MariaId, language);

            // Assert
            (await ReadLanguageAsync(database, MariaId)).ShouldBe(language, "the chosen language is stored on the signed-in user");
            (await ReadLanguageAsync(database, JoaoId)).ShouldBeNull("another user's language is never changed");
        }

        [Fact]
        [Trait("Rule", "T135")]
        public async Task UpdateAsync_LanguageChangedTwice_KeepsTheLatest()
        {
            // Arrange
            using var database = await CreateDatabaseAsync();
            await UpdateAsync(database, MariaId, "pt");

            // Act
            await UpdateAsync(database, MariaId, "fr");

            // Assert
            (await ReadLanguageAsync(database, MariaId)).ShouldBe("fr", "the language can be changed again");
        }
    }
}
