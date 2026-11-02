using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Organization.Data;
using RestaurantManagement.Shared.Services.Organization;

namespace RestaurantManagement.Modules.Organization.Services
{
    public class LocationBusinessDay : ILocationBusinessDay
    {
        private static readonly TimeOnly DefaultCutoff = new(4, 0);

        private readonly OrganizationDbContext _orgDb;
        private readonly IBusinessDayCalculator _calculator;

        public LocationBusinessDay(OrganizationDbContext orgDb, IBusinessDayCalculator calculator)
        {
            _orgDb = orgDb;
            _calculator = calculator;
        }

        public async Task<DateOnly?> GetBusinessDayAsync(Guid locationId, DateTime utcMoment)
        {
            // No tracking: the call can run inside a save of this same context
            var location = await _orgDb.Locations
                .AsNoTracking()
                .Where(l => l.Id == locationId)
                .Select(l => new { l.OrganizationId, l.TimeZone })
                .FirstOrDefaultAsync();
            if (location == null)
                return null;

            var cutoff = await _orgDb.OrganizationSettings
                .AsNoTracking()
                .Where(s => s.OrganizationId == location.OrganizationId)
                .Select(s => (TimeOnly?)s.BusinessDayCutoff)
                .FirstOrDefaultAsync();

            return _calculator.GetBusinessDate(utcMoment, location.TimeZone, cutoff ?? DefaultCutoff);
        }
    }
}
