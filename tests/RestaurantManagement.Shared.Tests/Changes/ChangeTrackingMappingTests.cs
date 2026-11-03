using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RestaurantManagement.Shared.Data.Changes;
using RestaurantManagement.Shared.Data.Entities;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Changes;

public class TestMenuItem : TenantEntity, IChangeTracked
{
    public string Name { get; set; } = null!;
    public long ChangeStamp { get; set; }
}

public class ChangeTrackingTestDbContext : DbContext
{
    public ChangeTrackingTestDbContext(DbContextOptions<ChangeTrackingTestDbContext> options) : base(options) { }

    public DbSet<TestMenuItem> MenuItems => Set<TestMenuItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestMenuItem>().Property(i => i.Id).ValueGeneratedNever();
        modelBuilder.ApplyChangeTracking(Database, "test_tombstones");
    }
}

/// <summary>The change stamp mapping on SQLite, where no PostgreSQL function or default may reach the schema.</summary>
public class ChangeTrackingMappingTests
{
    private sealed record TextRow(string Value);

    [Fact]
    [Trait("Rule", "SH147")]
    public void ApplyChangeTracking_ModuleTestsOnSqlite_CreatesSchemaStoresRowsAndMapsTheStamp()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ChangeTrackingTestDbContext>().UseSqlite(connection).Options;
        using var context = new ChangeTrackingTestDbContext(options);

        // Act
        context.Database.EnsureCreated();
        context.MenuItems.Add(new TestMenuItem { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Name = "Risoto de cogumelos" });
        context.SaveChanges();

        // Assert
        var tombstoneTables = context.Database.SqlQueryRaw<string>(
            "SELECT name AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = 'test_tombstones'").ToList();
        tombstoneTables.Count.ShouldBe(1, "the tombstone table named in the call is created");
        var stamp = context.Model.FindEntityType(typeof(TestMenuItem))!.FindProperty(nameof(IChangeTracked.ChangeStamp))!;
        stamp.GetColumnName().ShouldBe("change_stamp", "the stamp column is change_stamp");
        stamp.ValueGenerated.ShouldBe(ValueGenerated.OnAddOrUpdate, "only the database writes the stamp, and EF reads it back");
        stamp.IsConcurrencyToken.ShouldBeFalse("the stamp is not a version check");
        stamp.GetDefaultValueSql().ShouldBeNull("no PostgreSQL default may reach a SQLite schema");
    }
}
