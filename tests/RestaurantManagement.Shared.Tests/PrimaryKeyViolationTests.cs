using Microsoft.EntityFrameworkCore;
using Npgsql;
using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;

namespace RestaurantManagement.Shared.Tests;

/// <summary>Tells a primary key clash (a reused record ID) from every other database error.</summary>
public class PrimaryKeyViolationTests
{
    private static DbUpdateException Failure(Exception? inner)
        => new("An error occurred while saving the entity changes.", inner);

    private static PostgresException Postgres(string sqlState, string constraintName)
        => new("duplicate key value violates unique constraint", "ERROR", "ERROR", sqlState, constraintName: constraintName);

    [Theory]
    [InlineData("pk_menus")]
    [InlineData("pk_organization_invitations")]
    [Trait("Rule", "SH71")]
    public void Matches_UniqueViolationOnAPrimaryKey_ReturnsTrue(string constraintName)
    {
        // Arrange
        var exception = Failure(Postgres(PostgresErrorCodes.UniqueViolation, constraintName));

        // Act
        var result = PrimaryKeyViolation.Matches(exception);

        // Assert
        result.ShouldBeTrue("a unique violation on a primary key constraint is a reused record ID");
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, "ix_locations_organization_id_code")]
    [InlineData(PostgresErrorCodes.UniqueViolation, "ix_organization_roles_organization_id_name")]
    [InlineData(PostgresErrorCodes.UniqueViolation, "ix_users_email")]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, "pk_menus")]
    [Trait("Rule", "SH71")]
    public void Matches_AnotherUniqueIndexOrAnotherPostgresError_ReturnsFalse(string sqlState, string constraintName)
    {
        // Arrange
        var exception = Failure(Postgres(sqlState, constraintName));

        // Act
        var result = PrimaryKeyViolation.Matches(exception);

        // Assert
        result.ShouldBeFalse("a taken location code, role name or email is not a reused record ID");
    }

    [Fact]
    [Trait("Rule", "SH71")]
    public void Matches_ErrorThatIsNotFromPostgreSql_ReturnsFalse()
    {
        // Arrange
        var exception = Failure(new InvalidOperationException("The database is not reachable."));

        // Act
        var result = PrimaryKeyViolation.Matches(exception);

        // Assert
        result.ShouldBeFalse("only a PostgreSQL primary key violation counts");
    }

    [Fact]
    [Trait("Rule", "SH71")]
    public void Matches_ErrorWithNoInnerException_ReturnsFalse()
    {
        // Arrange
        var exception = Failure(null);

        // Act
        var result = PrimaryKeyViolation.Matches(exception);

        // Assert
        result.ShouldBeFalse("a save error with no database error behind it is not a reused record ID");
    }
}
