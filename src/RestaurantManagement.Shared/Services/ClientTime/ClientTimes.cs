namespace RestaurantManagement.Shared.Services.ClientTime
{
    /// <summary>The accepted times of one signed-in write. All moments are UTC.</summary>
    /// <param name="ActionTimeUtc">When the user acted, as the device said.</param>
    /// <param name="ActionOffsetMinutes">The UTC offset the device sent with the action time.</param>
    /// <param name="CorrectedActionTime">The action time shifted by the device clock error.</param>
    /// <param name="ClockError">Device clock at send time minus server time; positive means the device is fast.</param>
    /// <param name="ReceivedAt">When the server received the write.</param>
    public sealed record ClientTimes(DateTime ActionTimeUtc, int ActionOffsetMinutes, DateTime CorrectedActionTime, TimeSpan ClockError, DateTime ReceivedAt);
}
