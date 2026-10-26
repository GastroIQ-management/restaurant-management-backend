using System.Globalization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantManagement.Modules.Identity.Models.Registration;
using RestaurantManagement.Modules.Identity.Services.Registration;
using RestaurantManagement.Shared.Services.Email;
using Xunit;

namespace RestaurantManagement.Modules.Identity.Tests
{
    /// <summary>
    /// A new account has chosen no language yet, so its verification email follows the language of the
    /// registration request (the request culture, set from Accept-Language), English when unsupported.
    /// </summary>
    public class RegistrationEmailLanguageTests : IDisposable
    {
        private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

        public void Dispose() => CultureInfo.CurrentUICulture = _originalUiCulture;

        [Theory]
        [InlineData("pt", "pt")]
        [InlineData("fr", "fr")]
        [InlineData("en", "en")]
        [InlineData("es", "en")]
        [Trait("Rule", "R16")]
        public async Task RegisterAsync_RequestCulture_SendsVerificationEmailInThatLanguage(string culture, string expectedLanguage)
        {
            // Arrange
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            using var database = new IdentityTestDatabase();
            var emailService = Substitute.For<IEmailService>();
            using var context = database.CreateContext();
            var sut = new RegistrationService(context, TestSecurityOptions.Create(verificationCodeExpiryMinutes: 15), TestLocalizer.Create(), emailService,
                Substitute.For<ILogger<RegistrationService>>(), TestVerificationCodeHasher.Create(), TestPasswordHasher.Create());
            var request = new RegisterRequest { Email = "giulia@cantinadanonna.com.br", Password = "Password1!", FirstName = "Giulia", LastName = "Rossi" };

            // Act
            await sut.RegisterAsync(request);

            // Assert
            await emailService.Received(1).SendVerificationEmailAsync("giulia@cantinadanonna.com.br", "Giulia", Arg.Any<string>(), 15, expectedLanguage);
        }
    }
}
