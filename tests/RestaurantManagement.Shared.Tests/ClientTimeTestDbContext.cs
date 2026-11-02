using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RestaurantManagement.Shared.Data.ClientTime;
using RestaurantManagement.Shared.Data.Entities;
using RestaurantManagement.Shared.Services.ClientTime;
using RestaurantManagement.Shared.Services.Organization;

namespace RestaurantManagement.Shared.Tests;

public class TestPurchase : TenantEntity
{
    public string Supplier { get; set; } = null!;
}

public class TestCashShift : LocationEntity
{
    public string Cashier { get; set; } = null!;
}

public class ClientTimeTestDbContext : DbContext
{
    public ClientTimeTestDbContext(DbContextOptions<ClientTimeTestDbContext> options) : base(options) { }

    public DbSet<TestPurchase> Purchases => Set<TestPurchase>();
    public DbSet<TestCashShift> CashShifts => Set<TestCashShift>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestPurchase>().Property(p => p.Id).ValueGeneratedNever();
        modelBuilder.Entity<TestCashShift>().Property(s => s.Id).ValueGeneratedNever();
    }
}

/// <summary>SQLite in-memory database for the client-time interceptor tests, in the style of <see cref="AuditTestDatabase"/>.</summary>
public sealed class ClientTimeTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public ClientTimeTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var context = CreateContext(null);
        context.Database.EnsureCreated();
    }

    public ClientTimeTestDbContext CreateContext(ClientTimeSaveChangesInterceptor? interceptor)
    {
        var builder = new DbContextOptionsBuilder<ClientTimeTestDbContext>().UseSqlite(_connection);
        if (interceptor is not null)
            builder.AddInterceptors(interceptor);
        return new ClientTimeTestDbContext(builder.Options);
    }

    /// <summary>The interceptor with the accepted times in the holder (or none) and the business-day contract, if any, in the services.</summary>
    public static ClientTimeSaveChangesInterceptor CreateInterceptor(ClientTimes? times, ILocationBusinessDay? businessDay = null)
    {
        var holder = Substitute.For<IClientTimeContext>();
        holder.Times.Returns(times);

        var services = new ServiceCollection();
        if (businessDay is not null)
            services.AddSingleton(businessDay);
        return new ClientTimeSaveChangesInterceptor(holder, services.BuildServiceProvider());
    }

    public void Dispose() => _connection.Dispose();
}
