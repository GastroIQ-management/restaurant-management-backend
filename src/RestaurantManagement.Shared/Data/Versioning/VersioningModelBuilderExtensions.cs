using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace RestaurantManagement.Shared.Data.Versioning
{
    public static class VersioningModelBuilderExtensions
    {
        private const string NpgsqlProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";

        /// <summary>
        /// Maps <see cref="IVersionedEntity.Version"/> of every versioned entity: `xmin` as a row version on PostgreSQL, a plain concurrency token on other providers.
        /// </summary>
        public static ModelBuilder ApplyVersionTokens(this ModelBuilder modelBuilder, DatabaseFacade database)
        {
            var onPostgres = database.ProviderName == NpgsqlProvider;

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.BaseType is not null || !typeof(IVersionedEntity).IsAssignableFrom(entityType.ClrType))
                    continue;

                var property = modelBuilder.Entity(entityType.ClrType).Property(nameof(IVersionedEntity.Version));
                if (onPostgres)
                    property.IsRowVersion();
                else
                    property.IsConcurrencyToken();
            }

            return modelBuilder;
        }
    }
}
