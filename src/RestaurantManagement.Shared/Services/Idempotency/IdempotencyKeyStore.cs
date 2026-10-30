using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace RestaurantManagement.Shared.Services.Idempotency
{
    /// <summary>Reads and writes idempotency keys on any context that maps them. Uses only the user and organization it is given.</summary>
    public class IdempotencyKeyStore
    {
        public IdempotencyKeyStore(DbContext context, IOptions<IdempotencyOptions> options, TimeProvider time)
        {
        }

        /// <summary>Inserts the key as in progress and saves at once, or reports what the stored row says. An expired or abandoned row is taken over.</summary>
        public Task<IdempotencyReservation> ReserveAsync(Guid userId, Guid? organizationId, Guid key, string requestHash, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        /// <summary>Marks the key completed with its answer, in the context's current transaction.</summary>
        public Task CompleteAsync(Guid userId, Guid? organizationId, Guid key, StoredResponse response, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        /// <summary>Removes the key so the same request can run again.</summary>
        public Task ReleaseAsync(Guid userId, Guid? organizationId, Guid key, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        /// <summary>Deletes every key older than the retention time and returns how many.</summary>
        public Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
