using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace RestaurantManagement.Shared.Data.Versioning
{
    public static class VersioningModelBuilderExtensions
    {
        /// <summary>
        /// Maps <see cref="IVersionedEntity.Version"/> of every versioned entity: `xmin` as a row version on PostgreSQL, a plain concurrency token on other providers.
        /// </summary>
        public static ModelBuilder ApplyVersionTokens(this ModelBuilder modelBuilder, DatabaseFacade database)
            => throw new NotImplementedException();
    }
}
