namespace RestaurantManagement.Shared.Services.Changes
{
    /// <summary>
    /// What an opaque "changes since" cursor carries: the feed, the organization and location it is bound to, when it was
    /// issued, the lower bound to read from, the bound to hand out next, and the last position of an unfinished paging run.
    /// </summary>
    public sealed record ChangeCursor(
        string Feed,
        Guid OrganizationId,
        Guid? LocationId,
        DateTimeOffset IssuedAt,
        long LowerBound,
        long NextBound,
        long? LastStamp,
        Guid? LastId);
}
