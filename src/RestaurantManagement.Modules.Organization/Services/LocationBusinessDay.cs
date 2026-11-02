using RestaurantManagement.Modules.Organization.Data;
using RestaurantManagement.Shared.Services.Organization;

namespace RestaurantManagement.Modules.Organization.Services
{
    public class LocationBusinessDay : ILocationBusinessDay
    {
        public LocationBusinessDay(OrganizationDbContext orgDb, IBusinessDayCalculator calculator)
        {
        }

        public Task<DateOnly?> GetBusinessDayAsync(Guid locationId, DateTime utcMoment)
            => throw new NotImplementedException();
    }
}
