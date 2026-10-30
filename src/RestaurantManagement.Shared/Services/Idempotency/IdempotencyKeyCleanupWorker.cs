using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RestaurantManagement.Shared.Services.Idempotency
{
    /// <summary>Purges expired keys from every registered store at start and then every interval.</summary>
    public class IdempotencyKeyCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IdempotencyOptions _options;
        private readonly ILogger<IdempotencyKeyCleanupWorker> _logger;

        public IdempotencyKeyCleanupWorker(IServiceScopeFactory scopeFactory, IOptions<IdempotencyOptions> options, ILogger<IdempotencyKeyCleanupWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>One purge over every registered store; a failure is logged, not thrown. Returns the rows deleted.</summary>
        public async Task<int> PurgeOnceAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var provider = scope.ServiceProvider;
                var time = provider.GetRequiredService<TimeProvider>();
                var deleted = 0;
                foreach (var registration in provider.GetServices<IdempotencyStoreRegistration>())
                {
                    var context = (DbContext)provider.GetRequiredService(registration.ContextType);
                    deleted += await new IdempotencyKeyStore(context, Microsoft.Extensions.Options.Options.Create(_options), time).PurgeExpiredAsync(cancellationToken);
                }

                _logger.LogInformation("Idempotency key cleanup deleted {Count} expired keys", deleted);
                return deleted;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // A failed run must not stop the host; the next interval retries.
                _logger.LogError(ex, "Idempotency key cleanup failed");
                return 0;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.CleanupEnabled)
                return;

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.CleanupIntervalMinutes));
            do
            {
                await PurgeOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
