using MsOptions = Microsoft.Extensions.Options.Options;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using RestaurantManagement.Shared.Data.Idempotency;
using RestaurantManagement.Shared.Services.Idempotency;

namespace RestaurantManagement.Shared.Tests.Idempotency;

/// <summary>A kitchen note: the "write" the transaction tests apply next to a key.</summary>
public class TestKitchenNote
{
    public Guid Id { get; set; }
    public string Text { get; set; } = null!;
}

/// <summary>A module-like context that maps the key table the way a module does.</summary>
public class IdempotencyTestDbContext : DbContext
{
    public IdempotencyTestDbContext(DbContextOptions<IdempotencyTestDbContext> options) : base(options)
    {
    }

    public DbSet<TestKitchenNote> Notes => Set<TestKitchenNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestKitchenNote>().Property(n => n.Id).ValueGeneratedNever();
        modelBuilder.ApplyIdempotencyKeys("test_idempotency_keys");
    }
}

/// <summary>A second module-like context, so a purge over "every registered store" has more than one.</summary>
public class OtherIdempotencyTestDbContext : DbContext
{
    public OtherIdempotencyTestDbContext(DbContextOptions<OtherIdempotencyTestDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyIdempotencyKeys("other_idempotency_keys");
}

/// <summary>Makes the commit of a transaction fail, the way a lost connection would.</summary>
public sealed class FailingCommitInterceptor : DbTransactionInterceptor
{
    public override ValueTask<InterceptionResult> TransactionCommittingAsync(
        System.Data.Common.DbTransaction transaction, TransactionEventData eventData, InterceptionResult result,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("The commit failed.");
}

/// <summary>SQLite in-memory database kept open for the lifetime of the instance.</summary>
public sealed class IdempotencyTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _otherTableCreated;

    public IdempotencyTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public IdempotencyTestDbContext CreateContext(IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<IdempotencyTestDbContext>().UseSqlite(_connection);
        if (interceptor is not null)
            builder.AddInterceptors(interceptor);
        return new IdempotencyTestDbContext(builder.Options);
    }

    public OtherIdempotencyTestDbContext CreateOtherContext()
    {
        var other = new OtherIdempotencyTestDbContext(new DbContextOptionsBuilder<OtherIdempotencyTestDbContext>().UseSqlite(_connection).Options);
        if (!_otherTableCreated)
        {
            other.GetService<IRelationalDatabaseCreator>().CreateTables();
            _otherTableCreated = true;
        }

        return other;
    }

    public IdempotencyKeyStore CreateStore(DateTimeOffset now, IdempotencyOptions? options = null)
        => new(CreateContext(), MsOptions.Create(options ?? DefaultOptions()), new FixedTimeProvider(now));

    public static IdempotencyOptions DefaultOptions()
        => new() { RetentionDays = 8, InProgressTimeoutSeconds = 120, CleanupEnabled = true, CleanupIntervalMinutes = 60 };

    public void Dispose() => _connection.Dispose();
}
