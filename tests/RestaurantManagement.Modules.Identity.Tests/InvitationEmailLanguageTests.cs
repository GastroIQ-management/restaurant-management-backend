using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Identity.Entities.Users;
using Shouldly;
using static RestaurantManagement.Modules.Identity.Tests.InvitationHarness;
using static RestaurantManagement.Modules.Identity.Tests.MemberAccessTestSupport;

namespace RestaurantManagement.Modules.Identity.Tests
{
    /// <summary>
    /// The language of the invitation email: the invited person's own language when they already have an
    /// account with one, otherwise the language of the inviter's request. Ana Souza (Owner) invites Bruno.
    /// </summary>
    public class InvitationEmailLanguageTests
    {
        private const string BrunoEmail = "bruno@exemplo.com.br";

        private static async Task GiveBrunoAnAccountAsync(InvitationHarness harness, string? preferredLanguage, string email = BrunoEmail)
        {
            using var context = harness.Database.CreateContext();
            context.Users.Add(new User
            {
                Email = email,
                PasswordHash = "hash",
                FirstName = "Bruno",
                LastName = "Lima",
                IsVerified = true,
                PreferredLanguage = preferredLanguage
            });
            await context.SaveChangesAsync();
        }

        private static Task InviteBrunoAsync(InvitationHarness harness, string requestLanguage)
            => harness.As(AnaId).CreateAsync(Invite(BrunoEmail, (BatelId, new[] { "Waiter" })), requestLanguage);

        private static async Task<string> StoredLanguageAsync(InvitationHarness harness)
        {
            using var context = harness.Database.CreateContext();
            return (await context.OrganizationInvitations.SingleAsync()).Language;
        }

        [Fact]
        [Trait("Rule", "T139")]
        public async Task CreateAsync_FrenchRequest_EmailIsInFrench()
        {
            // Arrange
            using var harness = await InvitationHarness.CreateAsync();

            // Act
            await InviteBrunoAsync(harness, "fr");

            // Assert
            var email = harness.SentEmails().ShouldHaveSingleItem();
            email.Subject.ShouldBe($"Vous êtes invité à rejoindre Cantina da Nonna sur {ProductName}", "a French request gets the subject in French");
            email.Html.ShouldContain("lang=\"fr\"", customMessage: "the body is the French page");
            email.Html.ShouldContain("Waiter à Batel", customMessage: "the roles are listed per location with the French connector");
            email.Html.ShouldContain($"{AcceptPageUrl}?token=", customMessage: "the French email has the same accept button");
            (await StoredLanguageAsync(harness)).ShouldBe("fr", "the invitation keeps the language it was sent in");
        }

        [Theory]
        [InlineData("pt", "en", "pt")]
        [InlineData("fr", "pt", "fr")]
        [InlineData("en", "fr", "en")]
        [Trait("Rule", "T140")]
        public async Task CreateAsync_InvitedPersonHasAnAccountWithALanguage_UsesTheirLanguageNotTheInviters(string accountLanguage, string requestLanguage, string expected)
        {
            // Arrange
            using var harness = await InvitationHarness.CreateAsync();
            await GiveBrunoAnAccountAsync(harness, accountLanguage);

            // Act
            await InviteBrunoAsync(harness, requestLanguage);

            // Assert
            (await StoredLanguageAsync(harness)).ShouldBe(expected, "an existing account's own language wins over the inviter's request language");
            harness.SentEmails().ShouldHaveSingleItem().Html.ShouldContain($"lang=\"{expected}\"", customMessage: "the email is written in the account's language");
        }

        [Theory]
        [InlineData("pt")]
        [InlineData("fr")]
        [Trait("Rule", "T141")]
        public async Task CreateAsync_InvitedPersonHasAnAccountWithNoLanguage_UsesTheInvitersRequestLanguage(string requestLanguage)
        {
            // Arrange
            using var harness = await InvitationHarness.CreateAsync();
            await GiveBrunoAnAccountAsync(harness, preferredLanguage: null);

            // Act
            await InviteBrunoAsync(harness, requestLanguage);

            // Assert
            (await StoredLanguageAsync(harness)).ShouldBe(requestLanguage, "with no language on the account the inviter's request language is used");
        }

        [Fact]
        [Trait("Rule", "T141")]
        public async Task CreateAsync_InvitedPersonHasNoAccount_UsesTheInvitersRequestLanguage()
        {
            // Arrange
            using var harness = await InvitationHarness.CreateAsync();

            // Act
            await InviteBrunoAsync(harness, "pt");

            // Assert
            (await StoredLanguageAsync(harness)).ShouldBe("pt", "a person with no account gets the inviter's request language");
        }

        [Fact]
        [Trait("Rule", "T140")]
        public async Task CreateAsync_AccountEmailInCapitalLetters_StillUsesTheAccountLanguage()
        {
            // Arrange
            using var harness = await InvitationHarness.CreateAsync();
            await GiveBrunoAnAccountAsync(harness, "pt", email: "Bruno@Exemplo.com.br");

            // Act
            await InviteBrunoAsync(harness, "en");

            // Assert
            (await StoredLanguageAsync(harness)).ShouldBe("pt", "addresses are compared without regard to capital letters");
        }
    }
}
