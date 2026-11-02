using RestaurantManagement.Modules.Identity.Services.NativeApps;
using Shouldly;

namespace RestaurantManagement.Modules.Identity.Tests
{
    public class NativeAppOptionsValidatorTests
    {
        // Failure first: a web origin must never be configurable as native
        [Theory]
        [InlineData("https://app.cantinadanonna.com.br")]
        [InlineData("http://localhost")]
        [InlineData("https://localhost:8100")]
        [InlineData("HTTPS://localhost")]
        [InlineData("https://localhost/")]
        [InlineData("capacitor://localhost/")]
        [InlineData("")]
        [Trait("Rule", "T169")]
        public void Validate_OriginIsNotOneOfTheTwoNativeOrigins_FailsNamingTheSetting(string origin)
        {
            // Arrange
            var sut = new NativeAppOptionsValidator();

            // Act
            var result = sut.Validate(null, new NativeAppOptions { Origins = new List<string> { "capacitor://localhost", origin } });

            // Assert
            result.Failed.ShouldBeTrue("a web origin can never be configured as native, so the app refuses to start");
            result.FailureMessage.ShouldContain("NativeApp:Origins", customMessage: "the failure names the setting so the operator knows what to fix");
        }

        [Theory]
        [InlineData("https://localhost")]
        [InlineData("capacitor://localhost")]
        [Trait("Rule", "T170")]
        public void Validate_EachNativeOrigin_Succeeds(string origin)
        {
            // Arrange
            var sut = new NativeAppOptionsValidator();

            // Act
            var result = sut.Validate(null, new NativeAppOptions { Origins = new List<string> { origin } });

            // Assert
            result.Succeeded.ShouldBeTrue("the two native app origins are the only accepted values");
        }

        [Fact]
        [Trait("Rule", "T170")]
        public void Validate_EmptyList_SucceedsAndTurnsNativeModeOff()
        {
            // Arrange
            var sut = new NativeAppOptionsValidator();

            // Act
            var result = sut.Validate(null, new NativeAppOptions { Origins = new List<string>() });

            // Assert
            result.Succeeded.ShouldBeTrue("an empty list is allowed and means no request is native");
        }
    }
}
