namespace RestaurantManagement.Shared.Utils.Exceptions
{
    /// <summary>
    /// A stable error code: fixed English text `subject.reason`, never translated, always answered with one status.
    /// <paramref name="IsRetry"/> marks the codes a client may retry (see the retry rule in the OpenAPI document).
    /// </summary>
    public sealed record ErrorCode(string Value, int Status, bool IsRetry = false);
}
