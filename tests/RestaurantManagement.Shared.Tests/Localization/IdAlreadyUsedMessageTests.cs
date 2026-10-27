using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Localization;

/// <summary>The title of the "ID already used" refusal reads as real text in English, Portuguese and French and names no record.</summary>
public class IdAlreadyUsedMessageTests : IDisposable
{
    private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

    public void Dispose() => CultureInfo.CurrentUICulture = _originalUiCulture;

    [Theory]
    [InlineData("en", "This ID is already used.")]
    [InlineData("pt", "Este ID já está em uso.")]
    [InlineData("fr", "Cet identifiant est déjà utilisé.")]
    [Trait("Rule", "SH72")]
    public void Localizer_IdAlreadyUsed_ReturnsRealTextInEachLanguage(string culture, string expected)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(options => options.ResourcesPath = "Localization");
        var localizer = services.BuildServiceProvider().GetRequiredService<IStringLocalizer<SharedResource>>();
        CultureInfo.CurrentUICulture = new CultureInfo(culture);

        // Act
        var message = localizer["IdAlreadyUsed"];

        // Assert
        message.ResourceNotFound.ShouldBeFalse("the key exists in the catalog");
        message.Value.ShouldBe(expected, "the message reads as the agreed text and names no record, table or organization");
    }
}
