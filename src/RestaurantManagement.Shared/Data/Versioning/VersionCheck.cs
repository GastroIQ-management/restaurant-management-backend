using Microsoft.EntityFrameworkCore;

namespace RestaurantManagement.Shared.Data.Versioning
{
    public static class VersionCheck
    {
        /// <summary>
        /// Compares the version the client sent with the loaded entity's version. Different: throws the version conflict carrying <paramref name="current"/>.
        /// Equal: the sent version becomes the original value, so a later save that loses a race fails.
        /// </summary>
        public static void Require(DbContext context, IVersionedEntity entity, uint sentVersion, Func<object> current)
            => throw new NotImplementedException();

        /// <summary>
        /// Saves; when the row changed since it was read (a lost race), throws the version conflict carrying the record from <paramref name="current"/>.
        /// </summary>
        public static Task SaveAsync(DbContext context, Func<Task<object>> current, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
