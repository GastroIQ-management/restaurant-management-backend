using RestaurantManagement.Shared.Services.Idempotency;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Idempotency;

/// <summary>The <c>Idempotency</c> settings: defaults and the refusal of a value that is not positive.</summary>
public class IdempotencyOptionsValidatorTests
{
    private readonly IdempotencyOptionsValidator _validator = new();

    // Failure first
    [Theory]
    [InlineData("RetentionDays", 0, "Idempotency:RetentionDays")]
    [InlineData("RetentionDays", -8, "Idempotency:RetentionDays")]
    [InlineData("InProgressTimeoutSeconds", 0, "Idempotency:InProgressTimeoutSeconds")]
    [InlineData("CleanupIntervalMinutes", -1, "Idempotency:CleanupIntervalMinutes")]
    [Trait("Rule", "SH89")]
    public void Validate_SettingNotPositive_FailsNamingTheSetting(string property, int value, string expectedName)
    {
        // Arrange
        var options = IdempotencyTestDatabase.DefaultOptions();
        typeof(IdempotencyOptions).GetProperty(property)!.SetValue(options, value);

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue("a retention, timeout or interval that is not positive is refused at startup");
        result.FailureMessage.ShouldContain(expectedName, Case.Sensitive, "the error names the setting to fix");
    }

    [Fact]
    [Trait("Rule", "SH88")]
    public void Options_NothingConfigured_KeepsKeysEightDaysAndAcceptsThem()
    {
        // Act
        var options = new IdempotencyOptions();

        // Assert
        options.RetentionDays.ShouldBe(8, "keys outlive the 7-day offline limit");
        options.InProgressTimeoutSeconds.ShouldBe(120, "an abandoned key is released after 120 seconds");
        options.CleanupEnabled.ShouldBeTrue("expired keys are purged by default");
        options.CleanupIntervalMinutes.ShouldBe(60, "the purge runs every hour");
        _validator.Validate(null, options).Succeeded.ShouldBeTrue("the defaults are valid");
    }
}
