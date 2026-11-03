using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RestaurantManagement.Shared.Data.Changes
{
    public static class ChangeTrackingModelBuilderExtensions
    {
        private const string NpgsqlProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";
        private const string StampColumn = "change_stamp";
        private const int EntityTypeLength = 100;

        /// <summary>Maps the change stamp of every <see cref="IChangeTracked"/> entity and, when a table name is given, the tombstone table.</summary>
        public static ModelBuilder ApplyChangeTracking(this ModelBuilder modelBuilder, DatabaseFacade database, string? tombstoneTable = null)
        {
            var onPostgres = database.ProviderName == NpgsqlProvider;

            foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
            {
                if (entityType.BaseType is not null || !typeof(IChangeTracked).IsAssignableFrom(entityType.ClrType))
                    continue;

                var entity = modelBuilder.Entity(entityType.ClrType);
                MapStamp(entity.Property(nameof(IChangeTracked.ChangeStamp)), onPostgres);
                if (entityType.FindProperty("OrganizationId") is not null)
                    entity.HasIndex("OrganizationId", nameof(IChangeTracked.ChangeStamp));
            }

            if (tombstoneTable is not null)
                MapTombstones(modelBuilder, tombstoneTable, onPostgres);

            return modelBuilder;
        }

        private static void MapTombstones(ModelBuilder modelBuilder, string table, bool onPostgres)
        {
            modelBuilder.Entity<ChangeTombstone>(entity =>
            {
                entity.ToTable(table);
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Id).ValueGeneratedNever();
                entity.Property(t => t.EntityType).IsRequired().HasMaxLength(EntityTypeLength);
                entity.Property(t => t.ChangeStamp).HasColumnName(StampColumn);
                if (!onPostgres)
                    entity.Property(t => t.ChangeStamp).HasDefaultValue(0L);
                entity.HasIndex(t => new { t.OrganizationId, t.LocationId, t.EntityType, t.ChangeStamp });
            });
        }

        // Only the database writes the stamp. SQLite (module unit tests) has no trigger, so it gets a constant default to let inserts omit it.
        private static void MapStamp(PropertyBuilder property, bool onPostgres)
        {
            property.HasColumnName(StampColumn).IsConcurrencyToken(false);
            if (!onPostgres)
                property.HasDefaultValue(0L);
            property.ValueGeneratedOnAddOrUpdate();
        }
    }
}
