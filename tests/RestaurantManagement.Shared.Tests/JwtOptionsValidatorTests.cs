using FluentAssertions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using RestaurantManagement.Shared.Options;

namespace RestaurantManagement.Shared.Tests
{
    public class JwtOptionsValidatorTests
    {
        private const string StrongKey = "kitchen-signing-key-with-32-chars!";

        private static JwtOptionsValidator CreateSut(string environmentName)
        {
            var environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns(environmentName);
            return new JwtOptionsValidator(environment);
        }

        private static JwtOptions CreateOptions(string key = StrongKey, int expireMinutes = 15, int inactivityDays = 7, int absoluteDays = 30)
        {
            return new JwtOptions
            {
                Key = key,
                ExpireMinutes = expireMinutes,
                SessionInactivityDays = inactivityDays,
                SessionAbsoluteDays = absoluteDays
            };
        }

        // Scenario 1
        [Fact]
        [Trait("Rule", "SH21")]
        public void Validate_EmptyKey_FailsNamingJwtKey()
        {
            // Arrange
            var sut = CreateSut(Environments.Development);

            // Act
            var result = sut.Validate(null, CreateOptions(key: string.Empty));

            // Assert
            result.Failed.Should().BeTrue("a missing, short or placeholder signing key is refused at startup");
            result.FailureMessage.Should().Contain("Jwt:Key", "the failure names the signing key setting");
        }

        // Scenario 2
        [Fact]
        [Trait("Rule", "SH21")]
        public void Validate_KeyOf31Bytes_FailsNamingJwtKey()
        {
            // Arrange
            var sut = CreateSut(Environments.Development);
            var thirtyOneByteKey = new string('k', 31);

            // Act
            var result = sut.Validate(null, CreateOptions(key: thirtyOneByteKey));

            // Assert
            result.Failed.Should().BeTrue("a missing, short or placeholder signing key is refused at startup");
            result.FailureMessage.Should().Contain("Jwt:Key", "the failure names the signing key setting");
        }

        // Scenario 3
        [Fact]
        [Trait("Rule", "SH21")]
        public void Validate_PlaceholderKeyInProduction_FailsNamingJwtKey()
        {
            // Arrange
            var sut = CreateSut(Environments.Production);

            // Act
            var result = sut.Validate(null, CreateOptions(key: JwtOptions.PlaceholderKey));

            // Assert
            result.Failed.Should().BeTrue("a missing, short or placeholder signing key is refused at startup");
            result.FailureMessage.Should().Contain("Jwt:Key", "the failure names the signing key setting");
        }

        // Scenario 4
        [Fact]
        [Trait("Rule", "SH21")]
        public void Validate_PlaceholderKeyInStaging_FailsNamingJwtKey()
        {
            // Arrange
            var sut = CreateSut(Environments.Staging);

            // Act
            var result = sut.Validate(null, CreateOptions(key: JwtOptions.PlaceholderKey));

            // Assert
            result.Failed.Should().BeTrue("a missing, short or placeholder signing key is refused at startup");
            result.FailureMessage.Should().Contain("Jwt:Key", "the failure names the signing key setting");
        }

        // Scenario 5
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        [Trait("Rule", "SH21")]
        public void Validate_NonPositiveExpireMinutes_FailsNamingJwtExpireMinutes(int expireMinutes)
        {
            // Arrange
            var sut = CreateSut(Environments.Production);

            // Act
            var result = sut.Validate(null, CreateOptions(expireMinutes: expireMinutes));

            // Assert
            result.Failed.Should().BeTrue("a token lifetime of zero or less is refused at startup");
            result.FailureMessage.Should().Contain("Jwt:ExpireMinutes", "the failure names the token lifetime setting");
        }

        // Scenario 6
        [Fact]
        [Trait("Rule", "SH21")]
        public void Validate_PlaceholderKeyInDevelopment_Succeeds()
        {
            // Arrange
            var sut = CreateSut(Environments.Development);

            // Act
            var result = sut.Validate(null, CreateOptions(key: JwtOptions.PlaceholderKey));

            // Assert
            result.Succeeded.Should().BeTrue("the placeholder key is allowed in development only");
        }

        // Scenario 7
        [Fact]
        [Trait("Rule", "SH21")]
        public void Validate_KeyOfExactly32Bytes_Succeeds()
        {
            // Arrange
            var sut = CreateSut(Environments.Production);
            var thirtyTwoByteKey = new string('k', 32);

            // Act
            var result = sut.Validate(null, CreateOptions(key: thirtyTwoByteKey));

            // Assert
            result.Succeeded.Should().BeTrue("a signing key of exactly 32 bytes is accepted");
        }

        // Scenario 8
        [Fact]
        [Trait("Rule", "SH21")]
        public void Validate_Key17MultiByteCharsTotaling34Bytes_Succeeds()
        {
            // Arrange
            var sut = CreateSut(Environments.Production);
            var seventeenAccentedChars = new string('é', 17);

            // Act
            var result = sut.Validate(null, CreateOptions(key: seventeenAccentedChars));

            // Assert
            result.Succeeded.Should().BeTrue("the key length is counted in bytes, so 17 two-byte characters are accepted");
        }

        // Scenario 9
        [Fact]
        [Trait("Rule", "SH21")]
        public void Validate_StrongKeyAndPositiveExpiryInProduction_Succeeds()
        {
            // Arrange
            var sut = CreateSut(Environments.Production);

            // Act
            var result = sut.Validate(null, CreateOptions());

            // Assert
            result.Succeeded.Should().BeTrue("a strong key with a positive token lifetime is accepted in production");
        }

        [Theory]
        [InlineData(0, 30, "Jwt:SessionInactivityDays")]
        [InlineData(-1, 30, "Jwt:SessionInactivityDays")]
        [InlineData(7, 0, "Jwt:SessionAbsoluteDays")]
        [InlineData(7, -1, "Jwt:SessionAbsoluteDays")]
        [Trait("Rule", "SH51")]
        public void Validate_NonPositiveSessionLimit_FailsNamingTheSetting(int inactivityDays, int absoluteDays, string setting)
        {
            // Arrange
            var sut = CreateSut(Environments.Production);

            // Act
            var result = sut.Validate(null, CreateOptions(inactivityDays: inactivityDays, absoluteDays: absoluteDays));

            // Assert
            result.Failed.Should().BeTrue("a session limit of zero or less is refused at startup");
            result.FailureMessage.Should().Contain(setting, "the failure names the session limit to fix");
        }

        [Fact]
        [Trait("Rule", "SH51")]
        public void Validate_InactivityLongerThanAbsoluteLimit_FailsNamingJwtSessionInactivityDays()
        {
            // Arrange
            var sut = CreateSut(Environments.Production);

            // Act
            var result = sut.Validate(null, CreateOptions(inactivityDays: 31, absoluteDays: 30));

            // Assert
            result.Failed.Should().BeTrue("a session cannot stay idle longer than it may live in total");
            result.FailureMessage.Should().Contain("Jwt:SessionInactivityDays", "the failure names the inactivity limit");
        }

        [Fact]
        [Trait("Rule", "SH51")]
        public void Validate_InactivityEqualToAbsoluteLimit_Succeeds()
        {
            // Arrange
            var sut = CreateSut(Environments.Production);

            // Act
            var result = sut.Validate(null, CreateOptions(inactivityDays: 30, absoluteDays: 30));

            // Assert
            result.Succeeded.Should().BeTrue("equal inactivity and absolute limits are accepted");
        }

        [Fact]
        [Trait("Rule", "SH51")]
        public void Validate_SevenDaysInactivityAndThirtyDaysAbsolute_Succeeds()
        {
            // Arrange
            var sut = CreateSut(Environments.Production);

            // Act
            var result = sut.Validate(null, CreateOptions(inactivityDays: 7, absoluteDays: 30));

            // Assert
            result.Succeeded.Should().BeTrue("the shipped limits of 7 and 30 days are accepted");
        }
    }
}
