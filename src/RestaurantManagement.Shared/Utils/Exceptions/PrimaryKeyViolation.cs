using Microsoft.EntityFrameworkCore;

namespace RestaurantManagement.Shared.Utils.Exceptions
{
    /// <summary>Tells a primary key clash (a reused record ID) from every other database error.</summary>
    public static class PrimaryKeyViolation
    {
        public static bool Matches(DbUpdateException exception) => throw new NotImplementedException();
    }
}
