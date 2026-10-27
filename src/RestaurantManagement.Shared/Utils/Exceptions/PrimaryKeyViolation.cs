using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace RestaurantManagement.Shared.Utils.Exceptions
{
    /// <summary>Tells a primary key clash (a reused record ID) from every other database error.</summary>
    public static class PrimaryKeyViolation
    {
        public static bool Matches(DbUpdateException exception)
            => exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: { } constraint
            } && constraint.StartsWith("pk_", StringComparison.Ordinal);
    }
}
