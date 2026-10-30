using Microsoft.EntityFrameworkCore;

namespace RestaurantManagement.Shared.Data.Idempotency
{
    public static class IdempotencyModelBuilderExtensions
    {
        /// <summary>Maps <see cref="IdempotencyKey"/> under the module's own table name.</summary>
        public static ModelBuilder ApplyIdempotencyKeys(this ModelBuilder modelBuilder, string tableName)
            => throw new NotImplementedException();
    }
}
