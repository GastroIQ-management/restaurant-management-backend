using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Shared.Data.Versioning;
using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>A versioned row is the only place `xmin` (the PostgreSQL row number that changes on every update) can be proved: SQLite has none.</summary>
public class RowVersionPostgresTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid RisottoId = Guid.Parse("0b3f6c1e-5d2a-4e8b-9a47-1c6d8e2f7a10");

    private readonly PostgresDatabaseFixture _postgres;

    public RowVersionPostgresTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private sealed class PostgresDish : IVersionedEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int PriceInCents { get; set; }
        public uint Version { get; set; }
    }

    private sealed class PostgresVersionDbContext : DbContext
    {
        public PostgresVersionDbContext(DbContextOptions<PostgresVersionDbContext> options) : base(options)
        {
        }

        public DbSet<PostgresDish> Dishes => Set<PostgresDish>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyVersionTokens(Database);
    }

    private async Task<Func<PostgresVersionDbContext>> CreateDatabaseWithRisottoAsync()
    {
        var connectionString = await _postgres.CreateDatabaseAsync();
        PostgresVersionDbContext Create() => new(new DbContextOptionsBuilder<PostgresVersionDbContext>().UseNpgsql(connectionString).Options);

        await using var context = Create();
        await context.Database.EnsureCreatedAsync();
        context.Dishes.Add(new PostgresDish { Id = RisottoId, Name = "Risotto de cogumelos", PriceInCents = 5800 });
        await context.SaveChangesAsync();
        return Create;
    }

    [Fact]
    [Trait("Rule", "SH78")]
    public async Task SaveChanges_VersionedRowUpdated_VersionChangesAndNoVersionColumnIsStored()
    {
        // Arrange
        var create = await CreateDatabaseWithRisottoAsync();
        await using var context = create();
        var dish = await context.Dishes.SingleAsync();
        var versionRead = dish.Version;

        // Act
        dish.PriceInCents = 6200;
        await context.SaveChangesAsync();

        // Assert
        dish.Version.ShouldNotBe(versionRead, "every update changes the version, read back after the save");
        await using var reader = create();
        var columns = await reader.Database.SqlQueryRaw<string>("SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_name = 'Dishes'").ToListAsync();
        columns.Select(c => c.ToLowerInvariant()).ShouldNotContain("version", "the version is the system column xmin, never a stored column");
    }

    [Fact]
    [Trait("Rule", "SH79")]
    public async Task SaveAsync_RowUpdatedByAnotherContextAfterTheRead_ThrowsVersionConflict()
    {
        // Arrange
        var create = await CreateDatabaseWithRisottoAsync();
        await using var slow = create();
        var slowDish = await slow.Dishes.SingleAsync();
        VersionCheck.Require(slow, slowDish, slowDish.Version, () => new object());
        await using (var fast = create())
        {
            var fastDish = await fast.Dishes.SingleAsync();
            fastDish.PriceInCents = 6000;
            await fast.SaveChangesAsync();
        }

        slowDish.PriceInCents = 6200;

        // Act
        var act = () => VersionCheck.SaveAsync(slow, () => Task.FromResult<object>(new object()));

        // Assert
        await Should.ThrowAsync<VersionConflictException>(act, "a save with the old version must fail, never overwrite the newer change");
        await using var reader = create();
        (await reader.Dishes.SingleAsync()).PriceInCents.ShouldBe(6000, "the first change stays");
    }
}
