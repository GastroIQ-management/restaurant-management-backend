namespace RestaurantManagement.Shared.Services.Organization
{
    /// <summary>Answers the business day of a location of the token organization for a moment.</summary>
    public interface ILocationBusinessDay
    {
        /// <summary>The business day, or null for an unknown location or another organization's location.</summary>
        Task<DateOnly?> GetBusinessDayAsync(Guid locationId, DateTime utcMoment);
    }
}
