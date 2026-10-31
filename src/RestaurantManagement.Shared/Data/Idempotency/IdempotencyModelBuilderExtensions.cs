using Microsoft.EntityFrameworkCore;

namespace RestaurantManagement.Shared.Data.Idempotency
{
    public static class IdempotencyModelBuilderExtensions
    {
        private const int RequestHashLength = 64;

        /// <summary>Maps <see cref="IdempotencyKey"/> under the module's own table name.</summary>
        public static ModelBuilder ApplyIdempotencyKeys(this ModelBuilder modelBuilder, string tableName)
        {
            modelBuilder.Entity<IdempotencyKey>(entity =>
            {
                entity.ToTable(tableName);
                entity.HasKey(k => k.Id);
                entity.Property(k => k.Id).ValueGeneratedNever();
                entity.Property(k => k.RequestHash).IsRequired().HasMaxLength(RequestHashLength);
                entity.Property(k => k.State).HasConversion<int>();
                entity.Property(k => k.ResponseContentType).HasMaxLength(200);
                entity.Property(k => k.ResponseLocation).HasMaxLength(2000);

                // A user with no organization yet (creating one) counts as one value, so the key is still unique for them.
                entity.HasIndex(k => new { k.UserId, k.OrganizationId, k.Key })
                    .IsUnique()
                    .AreNullsDistinct(false);
                entity.HasIndex(k => k.CreatedAt);
            });

            return modelBuilder;
        }
    }
}
