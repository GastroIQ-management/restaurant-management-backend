using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Changes;

/// <summary>The titles of the "changes since" refusals read as real text in English, Portuguese and French.</summary>
public class ChangesMessageTests : IDisposable
{
    private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

    public void Dispose() => CultureInfo.CurrentUICulture = _originalUiCulture;

    [Theory]
    [InlineData("en", "ChangeCursorInvalid")]
    [InlineData("pt", "ChangeCursorInvalid")]
    [InlineData("fr", "ChangeCursorInvalid")]
    [InlineData("en", "ChangeCursorExpired")]
    [InlineData("pt", "ChangeCursorExpired")]
    [InlineData("fr", "ChangeCursorExpired")]
    [InlineData("en", "LocationRequired")]
    [InlineData("pt", "LocationRequired")]
    [InlineData("fr", "LocationRequired")]
    [Trait("Rule", "SH143")]
    public void Localizer_ChangesMessageKey_ReturnsRealTextInEachLanguage(string culture, string key)
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
        message.Value.ShouldNotBe(key, "the message is real text, not the key echoed back");
    }
}
