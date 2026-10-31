using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using RestaurantManagement.Shared.Data.Idempotency;
using RestaurantManagement.Shared.Services.Idempotency;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>A key table mapped by a module-like context: the rules only PostgreSQL enforces.</summary>
public class PostgresIdempotencyDbContext : DbContext
{
    public PostgresIdempotencyDbContext(DbContextOptions<PostgresIdempotencyDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyIdempotencyKeys("pg_idempotency_keys");
}

/// <summary>The key store on a real PostgreSQL database: one row per user, organization and key, even for two requests at the same moment.</summary>
public class IdempotencyKeyPostgresTests : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly Guid AnaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid Key = Guid.Parse("01933b2e-7c10-7a55-9d3e-5b1f0c2a4e11");

    private readonly PostgresDatabaseFixture _postgres;

    public IdempotencyKeyPostgresTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private async Task<Func<PostgresIdempotencyDbContext>> CreateDatabaseAsync()
    {
        var connectionString = await _postgres.CreateDatabaseAsync();
        PostgresIdempotencyDbContext Create() => new(new DbContextOptionsBuilder<PostgresIdempotencyDbContext>().UseNpgsql(connectionString).Options);
        using (var setup = Create())
            await setup.Database.EnsureCreatedAsync();
        return Create;
    }

    private static IdempotencyKey Row(Guid? organizationId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = AnaId,
        OrganizationId = organizationId,
        Key = Key,
        RequestHash = "hash-of-the-same-request",
        State = IdempotencyKeyState.InProgress,
        CreatedAt = DateTime.UtcNow
    };

    // Failure first
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Rule", "SH86")]
    public async Task SaveChanges_SameUserOrganizationAndKeyTwice_IsRefusedByTheDatabaseAlsoWithNoOrganization(bool withOrganization)
    {
        // Arrange
        var create = await CreateDatabaseAsync();
        var organizationId = withOrganization ? CantinaId : (Guid?)null;
        using (var first = create())
        {
            first.Set<IdempotencyKey>().Add(Row(organizationId));
            await first.SaveChangesAsync();
        }

        // Act
        using var second = create();
        second.Set<IdempotencyKey>().Add(Row(organizationId));
        var exception = await Should.ThrowAsync<DbUpdateException>(() => second.SaveChangesAsync());

        // Assert
        (exception.InnerException as PostgresException)?.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation,
            "a user has at most one row per key inside an organization, and no organization counts as one value");
    }

    [Fact]
    [Trait("Rule", "SH87")]
    public async Task ReserveAsync_FiveRequestsAtTheSameMoment_OneRunsAndTheOthersAreInProgress()
    {
        // Arrange
        var create = await CreateDatabaseAsync();
        var attempts = Enumerable.Range(0, 5).Select(_ =>
        {
            var store = new IdempotencyKeyStore(create(), Options.Create(new IdempotencyOptions
            {
                RetentionDays = 8, InProgressTimeoutSeconds = 120, CleanupEnabled = true, CleanupIntervalMinutes = 60
            }), TimeProvider.System);
            return store.ReserveAsync(AnaId, CantinaId, Key, "hash-of-the-same-request");
        });

        // Act
        var outcomes = (await Task.WhenAll(attempts)).Select(r => r.Outcome).ToList();

        // Assert
        outcomes.Count(o => o == IdempotencyOutcome.Reserved).ShouldBe(1, "exactly one of the simultaneous requests may run");
        outcomes.Count(o => o == IdempotencyOutcome.InProgress).ShouldBe(4, "the others are told the first is still running, never an error");
    }
}
