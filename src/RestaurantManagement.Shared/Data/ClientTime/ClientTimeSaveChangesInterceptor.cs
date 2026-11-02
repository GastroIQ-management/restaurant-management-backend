using Microsoft.EntityFrameworkCore.Diagnostics;
using RestaurantManagement.Shared.Services.ClientTime;

namespace RestaurantManagement.Shared.Data.ClientTime
{
    /// <summary>Stamps the accepted client times (and the business day of a new location row) on every saved record.</summary>
    public sealed class ClientTimeSaveChangesInterceptor : SaveChangesInterceptor
    {
        public ClientTimeSaveChangesInterceptor(IClientTimeContext clientTime, IServiceProvider services)
        {
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            => throw new NotImplementedException();

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
