using System.Collections.Concurrent;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// One PostgreSQL container for the whole test run, started by the first class that needs it;
/// every test gets its own database. Testcontainers removes the container when the run ends.
/// </summary>
public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    // Durability off: a test database never has to survive a crash.
    private static readonly PostgreSqlContainer Container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithCommand("-c", "fsync=off", "-c", "synchronous_commit=off", "-c", "full_page_writes=off",
            "-c", "max_connections=500")
        .Build();

    private static readonly Lazy<Task> Started = new(() => Container.StartAsync());

    private static readonly ConcurrentDictionary<string, Lazy<Task>> Templates = new();

    public Task InitializeAsync() => Started.Value;

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>A new empty database.</summary>
    public Task<string> CreateDatabaseAsync() => CreateDatabaseAsync(template: null);

    /// <summary>
    /// A new database copied from the template <paramref name="templateName"/>, which
    /// <paramref name="build"/> fills once per run (for example by running every migration).
    /// </summary>
    public async Task<string> CreateDatabaseFromTemplateAsync(string templateName, Func<string, Task> build)
    {
        await Templates.GetOrAdd(templateName, name => new Lazy<Task>(() => BuildTemplateAsync(name, build))).Value;
        return await CreateDatabaseAsync(templateName);
    }

    private static async Task<string> CreateDatabaseAsync(string? template)
    {
        var databaseName = "test_" + Guid.NewGuid().ToString("N");
        await ExecuteAsync(template is null
            ? $"CREATE DATABASE {databaseName}"
            : $"CREATE DATABASE {databaseName} TEMPLATE {template}");
        return ConnectionStringFor(databaseName);
    }

    private static async Task BuildTemplateAsync(string templateName, Func<string, Task> build)
    {
        await ExecuteAsync($"CREATE DATABASE {templateName}");
        await build(ConnectionStringFor(templateName));
        // PostgreSQL copies a template only while no one is connected to it; pooled connections may linger.
        await ExecuteAsync(
            $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{templateName}' AND pid <> pg_backend_pid()");
    }

    private static async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(Container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string ConnectionStringFor(string databaseName)
        => new NpgsqlConnectionStringBuilder(Container.GetConnectionString()) { Database = databaseName }.ConnectionString;
}
