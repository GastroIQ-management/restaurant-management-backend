using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace RestaurantManagement.Shared.Data.Changes
{
    public static class ChangeTrackingModelBuilderExtensions
    {
        /// <summary>Maps the change stamp of every <see cref="IChangeTracked"/> entity and, when a table name is given, the tombstone table.</summary>
        public static ModelBuilder ApplyChangeTracking(this ModelBuilder modelBuilder, DatabaseFacade database, string? tombstoneTable = null)
            => throw new NotImplementedException();
    }
}
