using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Localization;

/// <summary>The message a refused preferred language answers with reads as real text in English, Portuguese and French.</summary>
public class LanguageMessageKeysTests : IDisposable
{
    private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

    public void Dispose() => CultureInfo.CurrentUICulture = _originalUiCulture;

    [Theory]
    [InlineData("en")]
    [InlineData("pt")]
    [InlineData("fr")]
    [Trait("Rule", "SH47")]
    public void Localizer_UnsupportedLanguageKey_ReturnsRealTextInEachLanguage(string culture)
    {
        // Arrange
        const string key = "LanguageUnsupported_ValidationError";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(options => options.ResourcesPath = "Localization");
        var localizer = services.BuildServiceProvider().GetRequiredService<IStringLocalizer<SharedResource>>();
        CultureInfo.CurrentUICulture = new CultureInfo(culture);

        // Act
        var message = localizer[key];

        // Assert
        message.ResourceNotFound.ShouldBeFalse($"the key {key} must exist in the {culture} catalog");
        message.Value.ShouldNotBe(key, "the client gets a translated sentence, never the key itself");
    }
}
