using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Identity.Data;
using Shouldly;
using Xunit;

namespace RestaurantManagement.Web.Tests;

/// <summary>A user's preferred language lives in its own nullable column; no one has chosen one yet.</summary>
public class UserLanguageColumnTests : IClassFixture<PostgresDatabaseFixture>
{
    private const string LanguageColumnSql = """
        SELECT count(*)::text
        FROM information_schema.columns
        WHERE table_name = 'users' AND column_name = 'preferred_language' AND is_nullable = 'YES'
        """;

    private readonly PostgresDatabaseFixture _fixture;

    public UserLanguageColumnTests(PostgresDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    [Trait("Rule", "D34")]
    public async Task Migrate_CleanDatabase_UsersHasANullablePreferredLanguageColumn()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(await _fixture.CreateDatabaseAsync())
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var context = new IdentityDbContext(options);
        await context.Database.MigrateAsync();

        // Act
        var connection = context.Database.GetDbConnection();
        if (connection.State == System.Data.ConnectionState.Closed)
            await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = LanguageColumnSql;
        var columnCount = (string)(await command.ExecuteScalarAsync())!;

        // Assert
        columnCount.ShouldBe("1", "the preferred language is optional: a user who never chose one has no value stored");
    }
}
