using System.ComponentModel.DataAnnotations;
using RestaurantManagement.Modules.Identity.Models.Languages;
using Shouldly;
using Xunit;

namespace RestaurantManagement.Modules.Identity.Tests
{
    /// <summary>Only English, Portuguese and French can be chosen as the language of one's emails.</summary>
    public class UpdateLanguageRequestValidationTests
    {
        private static List<ValidationResult> Validate(string? language)
        {
            var request = new UpdateLanguageRequest { Language = language! };
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
            return results;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("de")]
        [InlineData("es")]
        [InlineData("pt-BR")]
        [InlineData("PT")]
        [Trait("Rule", "T136")]
        public void Validate_MissingOrUnsupportedLanguage_ReturnsErrorOnLanguage(string? language)
        {
            // Act
            var results = Validate(language);

            // Assert
            results.ShouldContain(r => r.MemberNames.Contains("Language"), "only en, pt and fr are accepted as a preferred language");
        }

        [Theory]
        [InlineData("en")]
        [InlineData("pt")]
        [InlineData("fr")]
        [Trait("Rule", "T136")]
        public void Validate_SupportedLanguage_HasNoErrors(string language)
        {
            // Act
            var results = Validate(language);

            // Assert
            results.ShouldBeEmpty("English, Portuguese and French are the supported languages");
        }
    }
}
