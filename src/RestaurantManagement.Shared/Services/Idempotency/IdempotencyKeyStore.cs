using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RestaurantManagement.Shared.Data.Idempotency;

namespace RestaurantManagement.Shared.Services.Idempotency
{
    /// <summary>Reads and writes idempotency keys on any context that maps them. Uses only the user and organization it is given.</summary>
    public class IdempotencyKeyStore
    {
        private readonly DbContext _context;
        private readonly IdempotencyOptions _options;
        private readonly TimeProvider _time;

        public IdempotencyKeyStore(DbContext context, IOptions<IdempotencyOptions> options, TimeProvider time)
        {
            _context = context;
            _options = options.Value;
            _time = time;
        }

        private DbSet<IdempotencyKey> Keys => _context.Set<IdempotencyKey>();

        /// <summary>Inserts the key as in progress and saves at once, or reports what the stored row says. An expired or abandoned row is taken over.</summary>
        public async Task<IdempotencyReservation> ReserveAsync(Guid userId, Guid? organizationId, Guid key, string requestHash, CancellationToken cancellationToken = default)
        {
            var now = _time.GetUtcNow().UtcDateTime;
            var stored = await Keys.AsNoTracking()
                .FirstOrDefaultAsync(k => k.UserId == userId && k.OrganizationId == organizationId && k.Key == key, cancellationToken);

            if (stored is not null)
            {
                if (!IsStale(stored, now))
                    return Decide(stored, requestHash);

                await Keys.Where(k => k.Id == stored.Id).ExecuteDeleteAsync(cancellationToken);
            }

            return await InsertAsync(userId, organizationId, key, requestHash, now, cancellationToken);
        }

        /// <summary>Marks the key completed with its answer, in the context's current transaction.</summary>
        public Task CompleteAsync(Guid userId, Guid? organizationId, Guid key, StoredResponse response, CancellationToken cancellationToken = default)
            => Keys.Where(k => k.UserId == userId && k.OrganizationId == organizationId && k.Key == key)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(k => k.State, IdempotencyKeyState.Completed)
                    .SetProperty(k => k.ResponseStatus, response.Status)
                    .SetProperty(k => k.ResponseContentType, response.ContentType)
                    .SetProperty(k => k.ResponseBody, response.Body)
                    .SetProperty(k => k.ResponseLocation, response.Location), cancellationToken);

        /// <summary>Removes the key so the same request can run again.</summary>
        public Task ReleaseAsync(Guid userId, Guid? organizationId, Guid key, CancellationToken cancellationToken = default)
            => Keys.Where(k => k.UserId == userId && k.OrganizationId == organizationId && k.Key == key)
                .ExecuteDeleteAsync(cancellationToken);

        /// <summary>Deletes every key older than the retention time and returns how many.</summary>
        public Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
        {
            var cutoff = _time.GetUtcNow().UtcDateTime.AddDays(-_options.RetentionDays);
            return Keys.Where(k => k.CreatedAt < cutoff).ExecuteDeleteAsync(cancellationToken);
        }

        private bool IsStale(IdempotencyKey stored, DateTime now)
        {
            if (stored.CreatedAt < now.AddDays(-_options.RetentionDays))
                return true;

            return stored.State == IdempotencyKeyState.InProgress
                && stored.CreatedAt < now.AddSeconds(-_options.InProgressTimeoutSeconds);
        }

        private static IdempotencyReservation Decide(IdempotencyKey stored, string requestHash)
        {
            if (stored.RequestHash != requestHash)
                return new IdempotencyReservation(IdempotencyOutcome.Reused, null);

            if (stored.State == IdempotencyKeyState.InProgress)
                return new IdempotencyReservation(IdempotencyOutcome.InProgress, null);

            var response = new StoredResponse(stored.ResponseStatus ?? 200, stored.ResponseContentType,
                stored.ResponseBody ?? Array.Empty<byte>(), stored.ResponseLocation);
            return new IdempotencyReservation(IdempotencyOutcome.Replay, response);
        }

        private async Task<IdempotencyReservation> InsertAsync(Guid userId, Guid? organizationId, Guid key, string requestHash, DateTime now, CancellationToken cancellationToken)
        {
            var row = new IdempotencyKey
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrganizationId = organizationId,
                Key = key,
                RequestHash = requestHash,
                State = IdempotencyKeyState.InProgress,
                CreatedAt = now
            };
            Keys.Add(row);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return new IdempotencyReservation(IdempotencyOutcome.Reserved, null);
            }
            catch (DbUpdateException)
            {
                // A parallel request reserved the same key first, so this one is a repeat still running.
                return new IdempotencyReservation(IdempotencyOutcome.InProgress, null);
            }
            finally
            {
                _context.Entry(row).State = EntityState.Detached;
            }
        }
    }
}
