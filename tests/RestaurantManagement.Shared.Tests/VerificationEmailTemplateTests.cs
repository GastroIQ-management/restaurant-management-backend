using RestaurantManagement.Shared.Services.Email;
using Shouldly;

namespace RestaurantManagement.Shared.Tests
{
    public class VerificationEmailTemplateTests
    {
        private const string ValidEmail = "a@b.com";
        private const string ValidFirstName = "Giulia";
        private const string ValidCode = "123456";
        private const int ValidExpiryMinutes = 15;

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [Trait("Rule", "SH24")]
        public void Build_FirstNameIsBlank_UsesGenericGreeting(string? firstName)
        {
            // Act
            var body = VerificationEmailTemplate.Build(ValidEmail, firstName!, ValidCode, ValidExpiryMinutes, "en");

            // Assert
            body.ShouldContain("Hi there,", customMessage: "a blank first name gets the generic greeting");
            body.ShouldNotContain("Hi ,", customMessage: "the greeting never shows an empty name");
        }

        [Fact]
        [Trait("Rule", "SH24")]
        public void Build_NonDefaultExpiryMinutes_StatesConfiguredMinutes()
        {
            // Act
            var body = VerificationEmailTemplate.Build(ValidEmail, ValidFirstName, ValidCode, 30, "en");

            // Assert
            body.ShouldContain("expires in 30 minutes", customMessage: "the email states the configured code lifetime");
            body.ShouldNotContain("15 minutes", customMessage: "the default lifetime is not shown when another one is configured");
        }

        [Fact]
        [Trait("Rule", "SH24")]
        public void Build_WithCode_BodyContainsTheCode()
        {
            // Act
            var body = VerificationEmailTemplate.Build(ValidEmail, ValidFirstName, "482913", ValidExpiryMinutes, "en");

            // Assert
            body.ShouldContain("482913", customMessage: "the email carries the code the user must type");
        }

        [Fact]
        [Trait("Rule", "SH24")]
        public void Build_WithValidInput_LeavesNoUnsubstitutedTokens()
        {
            // Act
            var body = VerificationEmailTemplate.Build(ValidEmail, ValidFirstName, ValidCode, ValidExpiryMinutes, "en", "https://cdn.gastroiq.com/logo.png");

            // Assert
            body.ShouldNotContain("{{", customMessage: "the email never shows unfilled template placeholders");
        }

        [Fact]
        [Trait("Rule", "SH24")]
        public void Build_WithRecipient_FooterContainsRecipientAddress()
        {
            // Act
            var body = VerificationEmailTemplate.Build("diner@example.com", ValidFirstName, ValidCode, ValidExpiryMinutes, "en");

            // Assert
            body.ShouldContain("Sent to diner@example.com because an account was created with this address.", customMessage: "the footer says which address the email was sent to");
        }

        [Fact]
        [Trait("Rule", "SH24")]
        public void Build_WithFirstName_GreetingContainsFirstName()
        {
            // Act
            var body = VerificationEmailTemplate.Build(ValidEmail, "Giulia", ValidCode, ValidExpiryMinutes, "en");

            // Assert
            body.ShouldContain("Hi Giulia,", customMessage: "the greeting uses the user first name");
        }

        [Fact]
        [Trait("Rule", "SH24")]
        public void Build_WithValidInput_ContainsAllDesignContentElements()
        {
            // Act
            var body = VerificationEmailTemplate.Build(ValidEmail, ValidFirstName, ValidCode, ValidExpiryMinutes, "en");

            // Assert
            body.ShouldContain("Confirm your email");
            body.ShouldContain("Use this code to confirm your email. It expires in 15 minutes.");
            body.ShouldContain("Enter the code below in GastroIQ to finish setting up your account.");
            body.ShouldContain("The code expires in 15 minutes.");
            body.ShouldContain(ValidCode);
            body.ShouldContain("Didn't create an account?");
            body.ShouldContain("The GastroIQ team");
            body.ShouldContain("Questions? Just reply to this email.");
            body.ShouldContain($"© {DateTime.UtcNow.Year} GastroIQ");
        }

        [Fact]
        [Trait("Rule", "SH24")]
        public void Build_WithLogoUrl_LogoImageUsesIt()
        {
            // Act
            var body = VerificationEmailTemplate.Build(ValidEmail, ValidFirstName, ValidCode, ValidExpiryMinutes, "en", "https://cdn.gastroiq.com/logo.png");

            // Assert
            body.ShouldContain("src=\"https://cdn.gastroiq.com/logo.png\"", customMessage: "the logo comes from the configured address");
        }

        [Theory]
        [InlineData("en", "Your GastroIQ code: 482913")]
        [InlineData("pt", "Seu código do GastroIQ: 482913")]
        [InlineData("fr", "Votre code GastroIQ : 482913")]
        [Trait("Rule", "SH24")]
        public void Subject_WithCode_ShowsTheCode(string language, string expected)
        {
            // Act
            var subject = VerificationEmailTemplate.Subject(language, "482913");

            // Assert
            subject.ShouldBe(expected, "the subject carries the code so it can be read without opening the email");
        }

        [Fact]
        [Trait("Rule", "SH26")]
        public void Build_NameAddressAndLogoWithHtmlCharacters_AreShownAsTextNeverAsMarkup()
        {
            // Arrange
            const string firstName = "<script>alert(1)</script>Ana";
            const string recipient = "ana+\"x\"@cantina.com.br";

            // Act
            var body = VerificationEmailTemplate.Build(recipient, firstName, ValidCode, ValidExpiryMinutes, "en", "x\" onerror=\"alert(1)");

            // Assert
            body.ShouldContain("&lt;script&gt;", customMessage: "a name with HTML must be shown as text");
            body.ShouldNotContain("<script>", customMessage: "a name must never become markup in the email");
            body.ShouldContain("&quot;", customMessage: "an address with quotes must be shown as text");
            body.ShouldNotContain(recipient, customMessage: "the raw address with quotes must not appear in the email");
            body.ShouldNotContain("onerror=\"alert", customMessage: "a logo address cannot break out of its attribute");
        }

        [Fact]
        [Trait("Rule", "SH26")]
        public void Build_NameLooksLikeAPlaceholder_IsNotReplacedAgain()
        {
            // Act
            var body = VerificationEmailTemplate.Build(ValidEmail, "{{heading}}", ValidCode, ValidExpiryMinutes, "en");

            // Assert
            body.ShouldContain("Hi {{heading}},", customMessage: "a name is shown as typed and never filled in as a placeholder");
        }
    }
}
