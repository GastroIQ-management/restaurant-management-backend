using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Shared.Data.Versioning;

namespace RestaurantManagement.Shared.Tests.Data;

/// <summary>A versioned row: a dish and its price.</summary>
public class TestVersionedDish : IVersionedEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int PriceInCents { get; set; }
    public uint Version { get; set; }
}

/// <summary>A module-like DbContext that maps versions the way the modules do.</summary>
public class VersionedTestDbContext : DbContext
{
    public VersionedTestDbContext(DbContextOptions<VersionedTestDbContext> options) : base(options)
    {
    }

    public DbSet<TestVersionedDish> Dishes => Set<TestVersionedDish>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyVersionTokens(Database);
    }
}

/// <summary>SQLite in-memory database, where `xmin` does not exist, kept open for the lifetime of the instance.</summary>
public sealed class VersionedTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public VersionedTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public VersionedTestDbContext CreateContext()
        => new(new DbContextOptionsBuilder<VersionedTestDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
