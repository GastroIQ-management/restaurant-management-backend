using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RestaurantManagement.Shared.Options;
using RestaurantManagement.Shared.Services.Email;
using Shouldly;

namespace RestaurantManagement.Shared.Tests
{
    /// <summary>
    /// The verification and password reset emails come in English, Portuguese and French; a language
    /// with no text of its own is answered in English. Subjects follow the language too.
    /// </summary>
    public class LocalizedCodeEmailTests
    {
        private const string Recipient = "maria@cantinadanonna.com.br";

        private sealed class SubjectLogger : ILogger<EmailService>
        {
            public List<string> Messages { get; } = new();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => Messages.Add(formatter(state, exception));
        }

        [Theory]
        [InlineData("en", "Verify your email")]
        [InlineData("pt", "Confirme seu e-mail")]
        [InlineData("fr", "Confirmez votre adresse e-mail")]
        [InlineData("de", "Verify your email")]
        [Trait("Rule", "SH48")]
        public void Subject_Verification_FollowsTheLanguage(string language, string expected)
        {
            // Act
            var subject = VerificationEmailTemplate.Subject(language);

            // Assert
            subject.ShouldBe(expected, "the verification subject is in the user's language, English when the language has no text");
        }

        [Theory]
        [InlineData("en", "Reset your password")]
        [InlineData("pt", "Redefina sua senha")]
        [InlineData("fr", "Réinitialisez votre mot de passe")]
        [InlineData("de", "Reset your password")]
        [Trait("Rule", "SH48")]
        public void Subject_PasswordReset_FollowsTheLanguage(string language, string expected)
        {
            // Act
            var subject = PasswordResetEmailTemplate.Subject(language);

            // Assert
            subject.ShouldBe(expected, "the reset subject is in the user's language, English when the language has no text");
        }

        [Theory]
        [InlineData("en", "Enter the code below to finish setting up your account")]
        [InlineData("pt", "Digite o código abaixo para concluir a criação da sua conta")]
        [InlineData("fr", "Saisissez le code ci-dessous pour terminer la création de votre compte")]
        [InlineData("de", "Enter the code below to finish setting up your account")]
        [Trait("Rule", "SH48")]
        public void Build_Verification_IsWrittenInTheLanguage(string language, string sentence)
        {
            // Act
            var body = VerificationEmailTemplate.Build(Recipient, "Maria", "482913", 15, language);

            // Assert
            body.ShouldContain(sentence, customMessage: "the verification email text is in the user's language");
        }

        [Theory]
        [InlineData("en", "Enter the code below to reset your password")]
        [InlineData("pt", "Digite o código abaixo para redefinir sua senha")]
        [InlineData("fr", "Saisissez le code ci-dessous pour réinitialiser votre mot de passe")]
        [InlineData("de", "Enter the code below to reset your password")]
        [Trait("Rule", "SH48")]
        public void Build_PasswordReset_IsWrittenInTheLanguage(string language, string sentence)
        {
            // Act
            var body = PasswordResetEmailTemplate.Build(Recipient, "Maria", "482913", 15, language);

            // Assert
            body.ShouldContain(sentence, customMessage: "the reset email text is in the user's language");
        }

        [Theory]
        [InlineData("pt")]
        [InlineData("fr")]
        [Trait("Rule", "SH49")]
        public void Build_PortugueseAndFrench_ContainNameCodeExpiryAndRecipientWithNoPlaceholderLeft(string language)
        {
            // Act
            var verification = VerificationEmailTemplate.Build(Recipient, "Maria", "482913", 20, language);
            var reset = PasswordResetEmailTemplate.Build(Recipient, "Maria", "482913", 20, language);

            // Assert
            foreach (var body in new[] { verification, reset })
            {
                body.ShouldContain("Maria", customMessage: "the email greets the user by first name");
                body.ShouldContain("482913", customMessage: "the email carries the code");
                body.ShouldContain("20", customMessage: "the email says how many minutes the code lasts");
                body.ShouldContain(Recipient, customMessage: "the email names the address it was sent to");
                body.ShouldNotContain("{{", customMessage: "every placeholder is filled in");
                body.ShouldContain($"<html lang=\"{language}\">", customMessage: "the page declares its language");
            }
        }

        [Theory]
        [InlineData("pt")]
        [InlineData("fr")]
        [Trait("Rule", "SH49")]
        public void Build_PortugueseAndFrenchWithHtmlInName_ShowsTheNameAsTextNeverAsMarkup(string language)
        {
            // Act
            var verification = VerificationEmailTemplate.Build("<i>x</i>@example.com", "<b>Maria</b>", "482913", 15, language);
            var reset = PasswordResetEmailTemplate.Build("<i>x</i>@example.com", "<b>Maria</b>", "482913", 15, language);

            // Assert
            foreach (var body in new[] { verification, reset })
            {
                body.ShouldContain("&lt;b&gt;Maria&lt;/b&gt;", customMessage: "a first name with HTML is shown as text");
                body.ShouldNotContain("<b>Maria</b>", customMessage: "a first name must never become markup");
                body.ShouldNotContain("<i>x</i>", customMessage: "an address with HTML must never become markup");
            }
        }

        [Fact]
        [Trait("Rule", "SH50")]
        public async Task SendVerificationEmailAsync_Portuguese_SendsThePortugueseSubject()
        {
            // Arrange
            var (sut, logger) = DevelopmentEmailService();

            // Act
            await sut.SendVerificationEmailAsync(Recipient, "Maria", "482913", 15, "pt");

            // Assert
            logger.Messages.ShouldContain(m => m.Contains("Confirme seu e-mail"), "the email is sent in the language it was asked for");
        }

        [Fact]
        [Trait("Rule", "SH50")]
        public async Task SendPasswordResetEmailAsync_French_SendsTheFrenchSubject()
        {
            // Arrange
            var (sut, logger) = DevelopmentEmailService();

            // Act
            await sut.SendPasswordResetEmailAsync(Recipient, "Maria", "482913", 15, "fr");

            // Assert
            logger.Messages.ShouldContain(m => m.Contains("Réinitialisez votre mot de passe"), "the email is sent in the language it was asked for");
        }

        private static (EmailService Sut, SubjectLogger Logger) DevelopmentEmailService()
        {
            var logger = new SubjectLogger();
            var environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns(Environments.Development);
            var options = Microsoft.Extensions.Options.Options.Create(new EmailOptions { SmtpHost = string.Empty, FromEmail = "no-reply@cantinadanonna.com.br" });
            return (new EmailService(options, logger, environment), logger);
        }
    }
}
