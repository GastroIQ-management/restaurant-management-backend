using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using RestaurantManagement.Shared;
using RestaurantManagement.Shared.Services.ClientTime;
using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;

namespace RestaurantManagement.Shared.Tests;

/// <summary>
/// The two device-time headers of a signed-in write: reading, clock error and the 5-minute / 7-day bounds.
/// The server received the write at 2026-11-02 15:00 UTC.
/// </summary>
public class ClientTimeValidatorTests
{
    private static readonly DateTimeOffset Received = new(2026, 11, 2, 15, 0, 0, TimeSpan.Zero);

    private static ClientTimeValidator CreateSut()
    {
        var localizer = Substitute.For<IStringLocalizer<SharedResource>>();
        localizer[Arg.Any<string>()].Returns(call => new LocalizedString(call.Arg<string>(), call.Arg<string>()));
        return new ClientTimeValidator(localizer);
    }

    private static string Iso(DateTimeOffset moment) => moment.ToString("O", CultureInfo.InvariantCulture);

    // Outcome 6: failure first
    [Theory]
    [InlineData(null, "2026-11-02T15:00:00Z")]
    [InlineData("2026-11-02T14:00:00Z", null)]
    [Trait("Rule", "SH108")]
    public void Validate_ActionTimeOrSentAtMissing_Throws400Required(string? actionTime, string? sentAt)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var thrown = Should.Throw<BusinessException>(() => sut.Validate(actionTime, sentAt, Received));

        // Assert
        thrown.Code.ShouldBe("client_time.required", "a missing header is refused with its own code");
        thrown.StatusCode.ShouldBe(400, "a missing device time is a Bad Request");
    }

    // Outcome 6
    [Theory]
    [InlineData("yesterday evening", "2026-11-02T15:00:00Z")]
    [InlineData("2026-11-02T10:00:00", "2026-11-02T15:00:00Z")]
    [InlineData("2026-11-02", "2026-11-02T15:00:00Z")]
    [InlineData("2026-11-02T10:00:00Z", "ontem")]
    [InlineData("2026-11-02T10:00:00Z", "2026-11-02T15:00:00")]
    [InlineData("2026-11-02T10:00:00.0000000000000000000000000000000000000000000000000000000000000Z", "2026-11-02T15:00:00Z")]
    [Trait("Rule", "SH109")]
    public void Validate_ValueUnreadableWithoutOffsetOrTooLong_Throws400Invalid(string actionTime, string sentAt)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var thrown = Should.Throw<BusinessException>(() => sut.Validate(actionTime, sentAt, Received));

        // Assert
        thrown.Code.ShouldBe("client_time.invalid", "garbage, a time with no offset or an over-long value is refused");
        thrown.StatusCode.ShouldBe(400, "an unreadable device time is a Bad Request");
    }

    // Outcome 6
    [Fact]
    [Trait("Rule", "SH109")]
    public void Validate_HeaderSentWithSeveralValues_Throws400Invalid()
    {
        // Arrange
        var sut = CreateSut();
        var twoValues = new StringValues(new[] { Iso(Received.AddHours(-2)), Iso(Received.AddHours(-1)) });

        // Act
        var thrown = Should.Throw<BusinessException>(() => sut.Validate(twoValues, Iso(Received), Received));

        // Assert
        thrown.Code.ShouldBe("client_time.invalid", "the header must carry exactly one value");
    }

    // Outcome 4: failure
    [Theory]
    [InlineData(360, "client_time.in_future")]
    [InlineData(301, "client_time.in_future")]
    [InlineData(-604860, "client_time.too_old")]
    [InlineData(-604801, "client_time.too_old")]
    [Trait("Rule", "SH110")]
    public void Validate_CorrectedTimeBeyondTheBounds_Throws422(int secondsFromReceived, string expectedCode)
    {
        // Arrange
        var sut = CreateSut();
        var acted = Received.AddSeconds(secondsFromReceived);

        // Act
        var thrown = Should.Throw<BusinessException>(() => sut.Validate(Iso(acted), Iso(Received), Received));

        // Assert
        thrown.Code.ShouldBe(expectedCode, "more than 5 minutes ahead or older than 7 days is refused");
        thrown.StatusCode.ShouldBe(422, "a time that is understood but out of bounds is Unprocessable Content");
    }

    [Theory]
    [InlineData("9999-12-31T23:59:59Z", "0001-01-01T00:00:00Z", "client_time.in_future")]
    [InlineData("0001-01-01T00:00:00Z", "9999-12-31T23:59:59Z", "client_time.too_old")]
    [Trait("Rule", "SH136")]
    public void Validate_BothHeadersReadableButFarApart_Throws422InsteadOfOverflowing(string actionTime, string sentAt, string expectedCode)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var thrown = Should.Throw<BusinessException>(() => sut.Validate(actionTime, sentAt, Received));

        // Assert
        thrown.Code.ShouldBe(expectedCode, "extreme but readable dates are refused like any other out-of-bounds time");
        thrown.StatusCode.ShouldBe(422, "extreme dates must never become a server error");
    }

    // Outcomes 4 and 5: edge, bounds are inclusive
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(-10079)]
    [InlineData(-10080)]
    [Trait("Rule", "SH111")]
    public void Validate_CorrectedTimeInsideTheBounds_IsAccepted(int minutesFromReceived)
    {
        // Arrange
        var sut = CreateSut();
        var acted = Received.AddMinutes(minutesFromReceived);

        // Act
        var times = sut.Validate(Iso(acted), Iso(Received), Received);

        // Assert
        times.CorrectedActionTime.ShouldBe(acted.UtcDateTime, "a time up to 5 minutes ahead or exactly 7 days old is accepted");
    }

    // Outcome 1
    [Fact]
    [Trait("Rule", "SH112")]
    public void Validate_ActionFiveHoursOld_KeepsTheActionTimeAndRecordsTheReceivedTime()
    {
        // Arrange
        var sut = CreateSut();
        var acted = Received.AddHours(-5);

        // Act
        var times = sut.Validate(Iso(acted), Iso(Received), Received);

        // Assert
        times.CorrectedActionTime.ShouldBe(acted.UtcDateTime, "a device with the right clock keeps the action time as sent");
        times.ReceivedAt.ShouldBe(Received.UtcDateTime, "the server receive time is stored next to the action time");
        times.ClockError.ShouldBe(TimeSpan.Zero, "a right clock has no error");
    }

    // Outcome 2
    [Theory]
    [InlineData(10)]
    [InlineData(-10)]
    [Trait("Rule", "SH113")]
    public void Validate_DeviceClockOffByTenMinutes_ShiftsTheActionTimeByTheError(int minutesFast)
    {
        // Arrange
        var sut = CreateSut();
        var deviceSentAt = Received.AddMinutes(minutesFast);
        var deviceActionTime = deviceSentAt.AddHours(-5);

        // Act
        var times = sut.Validate(Iso(deviceActionTime), Iso(deviceSentAt), Received);

        // Assert
        times.ClockError.ShouldBe(TimeSpan.FromMinutes(minutesFast), "the error is the device clock minus the server clock; positive means fast");
        times.CorrectedActionTime.ShouldBe(deviceActionTime.UtcDateTime.AddMinutes(-minutesFast), "the corrected time is the device time minus the error");
        times.ActionTimeUtc.ShouldBe(deviceActionTime.UtcDateTime, "the device time as sent is kept next to the corrected one");
    }

    // Outcome 2 and 4: edge, the bounds apply to the corrected time
    [Fact]
    [Trait("Rule", "SH114")]
    public void Validate_FastClockMakesTheRawTimeLookTenMinutesAhead_UsesTheCorrectedTimeForTheBounds()
    {
        // Arrange
        var sut = CreateSut();
        var deviceNow = Received.AddMinutes(10);

        // Act
        var times = sut.Validate(Iso(deviceNow), Iso(deviceNow), Received);

        // Assert
        times.CorrectedActionTime.ShouldBe(Received.UtcDateTime, "an action made right now on a fast clock is accepted once corrected");
    }

    // Edge: offsets
    [Theory]
    [InlineData("2026-11-02T11:30:00-03:00", -180)]
    [InlineData("2026-11-02T14:30:00Z", 0)]
    [InlineData("2026-11-02T20:00:00+05:30", 330)]
    [Trait("Rule", "SH115")]
    public void Validate_ActionTimeWithAnOffset_StoresTheInstantInUtcAndTheOffsetInMinutes(string actionTime, int expectedOffsetMinutes)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var times = sut.Validate(actionTime, Iso(Received), Received);

        // Assert
        times.ActionTimeUtc.ShouldBe(new DateTime(2026, 11, 2, 14, 30, 0, DateTimeKind.Utc), "the same moment in universal time whatever the offset");
        times.ActionOffsetMinutes.ShouldBe(expectedOffsetMinutes, "the device's offset is kept");
    }
}
