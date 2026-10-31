using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Localization;

/// <summary>The titles of the three idempotency refusals read as real text in English, Portuguese and French.</summary>
public class IdempotencyMessageTests : IDisposable
{
    private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

    public void Dispose() => CultureInfo.CurrentUICulture = _originalUiCulture;

    [Theory]
    [InlineData("en", "IdempotencyKeyInvalid", "The Idempotency-Key header must be a valid UUID.")]
    [InlineData("pt", "IdempotencyKeyInvalid", "O cabeçalho Idempotency-Key deve ser um UUID válido.")]
    [InlineData("fr", "IdempotencyKeyInvalid", "L'en-tête Idempotency-Key doit être un UUID valide.")]
    [InlineData("en", "IdempotencyKeyReused", "This Idempotency-Key was already used for a different request.")]
    [InlineData("pt", "IdempotencyKeyReused", "Esta Idempotency-Key já foi usada em outra requisição.")]
    [InlineData("fr", "IdempotencyKeyReused", "Cette clé Idempotency-Key a déjà été utilisée pour une autre requête.")]
    [InlineData("en", "RequestInProgress", "This request is still being processed. Try again shortly.")]
    [InlineData("pt", "RequestInProgress", "Esta requisição ainda está em processamento. Tente novamente em instantes.")]
    [InlineData("fr", "RequestInProgress", "Cette requête est encore en cours de traitement. Réessayez dans un instant.")]
    [Trait("Rule", "SH92")]
    public void Localizer_IdempotencyTitle_ReturnsRealTextInEachLanguage(string culture, string key, string expected)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(options => options.ResourcesPath = "Localization");
        var localizer = services.BuildServiceProvider().GetRequiredService<IStringLocalizer<SharedResource>>();
        CultureInfo.CurrentUICulture = new CultureInfo(culture);

        // Act
        var message = localizer[key];

        // Assert
        message.ResourceNotFound.ShouldBeFalse("the key exists in the catalog");
        message.Value.ShouldBe(expected, "the title reads as the agreed text in the asked language");
    }
}
