using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RestaurantManagement.Shared.Services.Idempotency
{
    /// <summary>Purges expired keys from every registered store at start and then every interval.</summary>
    public class IdempotencyKeyCleanupWorker : BackgroundService
    {
        public IdempotencyKeyCleanupWorker(IServiceScopeFactory scopeFactory, IOptions<IdempotencyOptions> options, ILogger<IdempotencyKeyCleanupWorker> logger)
        {
        }

        /// <summary>One purge over every registered store; a failure is logged, not thrown. Returns the rows deleted.</summary>
        public Task<int> PurgeOnceAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => throw new NotImplementedException();
    }
}
