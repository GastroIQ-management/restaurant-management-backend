using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using RestaurantManagement.Shared.Data.Entities;
using RestaurantManagement.Shared.Services.ClientTime;
using RestaurantManagement.Shared.Services.Organization;

namespace RestaurantManagement.Shared.Data.ClientTime
{
    /// <summary>Stamps the accepted client times (and the business day of a new location row) on every saved record.</summary>
    public sealed class ClientTimeSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IClientTimeContext _clientTime;
        private readonly IServiceProvider _services;

        public ClientTimeSaveChangesInterceptor(IClientTimeContext clientTime, IServiceProvider services)
        {
            _clientTime = clientTime;
            _services = services;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context is not null && _clientTime.Times is { } times)
            {
                var entries = Changed(eventData.Context);
                if (entries.Any(IsAddedLocation))
                    throw new InvalidOperationException("A location record with client times must be saved with SaveChangesAsync: its business day is read asynchronously.");

                foreach (var entry in entries)
                    Stamp(entry.Entity, times);
            }

            return result;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is null || _clientTime.Times is not { } times)
                return result;

            foreach (var entry in Changed(eventData.Context))
            {
                Stamp(entry.Entity, times);
                if (IsAddedLocation(entry))
                    await SetBusinessDayAsync((LocationEntity)entry.Entity, times);
            }

            return result;
        }

        private static List<EntityEntry<BaseEntity>> Changed(DbContext context)
            => context.ChangeTracker.Entries<BaseEntity>().Where(e => e.State is EntityState.Added or EntityState.Modified).ToList();

        private static bool IsAddedLocation(EntityEntry<BaseEntity> entry)
            => entry.State == EntityState.Added && entry.Entity is LocationEntity;

        private static void Stamp(BaseEntity entity, ClientTimes times)
        {
            entity.ClientActionTime = times.ActionTimeUtc;
            entity.ClientActionOffsetMinutes = times.ActionOffsetMinutes;
            entity.CorrectedActionTime = times.CorrectedActionTime;
            entity.ClockError = times.ClockError;
            entity.ReceivedAt = times.ReceivedAt;
        }

        // Resolved here, not in the constructor: the Organization context carries this interceptor and its contract implementation uses that context.
        private async Task SetBusinessDayAsync(LocationEntity entity, ClientTimes times)
        {
            var contract = _services.GetRequiredService<ILocationBusinessDay>();
            entity.BusinessDay = await contract.GetBusinessDayAsync(entity.LocationId, times.CorrectedActionTime);
        }
    }
}
