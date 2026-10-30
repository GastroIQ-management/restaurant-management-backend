using MsOptions = Microsoft.Extensions.Options.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RestaurantManagement.Shared.Data.Idempotency;
using RestaurantManagement.Shared.Services.Idempotency;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Idempotency;

/// <summary>The purge job removes expired keys from every module's store when it starts.</summary>
public class IdempotencyKeyCleanupWorkerTests : IDisposable
{
    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");

    private readonly IdempotencyTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    [Trait("Rule", "SH90")]
    public async Task StartAsync_ExpiredKeysInTwoStores_DeletesThemFromBothAndKeepsFreshOnes()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var old = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e31");
        var fresh = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e32");
        await _database.CreateStore(now.AddDays(-9)).ReserveAsync(AnaId, CantinaId, old, "hash-old");
        await _database.CreateStore(now).ReserveAsync(AnaId, CantinaId, fresh, "hash-fresh");
        using (var other = _database.CreateOtherContext())
        {
            var otherStore = new IdempotencyKeyStore(other, MsOptions.Create(IdempotencyTestDatabase.DefaultOptions()), new FixedTimeProvider(now.AddDays(-9)));
            await otherStore.ReserveAsync(AnaId, CantinaId, old, "hash-old");
        }

        var services = new ServiceCollection();
        services.AddSingleton(MsOptions.Create(IdempotencyTestDatabase.DefaultOptions()));
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped(_ => _database.CreateContext());
        services.AddScoped(_ => _database.CreateOtherContext());
        services.AddIdempotencyKeyStore<IdempotencyTestDbContext>(typeof(IdempotencyKeyCleanupWorkerTests).Assembly);
        services.AddIdempotencyKeyStore<OtherIdempotencyTestDbContext>(typeof(IdempotencyKey).Assembly);
        using var provider = services.BuildServiceProvider();
        var worker = new IdempotencyKeyCleanupWorker(provider.GetRequiredService<IServiceScopeFactory>(),
            MsOptions.Create(IdempotencyTestDatabase.DefaultOptions()), NullLogger<IdempotencyKeyCleanupWorker>.Instance);

        // Act
        var deleted = await worker.PurgeOnceAsync(CancellationToken.None);

        // Assert
        deleted.ShouldBe(2, "the expired key is deleted from each of the two registered stores");
        using var check = _database.CreateContext();
        (await check.Set<IdempotencyKey>().Select(k => k.RequestHash).ToListAsync()).ShouldHaveSingleItem("a key inside the retention time stays").ShouldBe("hash-fresh");
    }
}
