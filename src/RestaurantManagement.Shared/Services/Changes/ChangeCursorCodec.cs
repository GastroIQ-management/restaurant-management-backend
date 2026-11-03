namespace RestaurantManagement.Shared.Services.Changes
{
    /// <summary>Encodes and decodes the opaque change cursor and refuses one that is invalid, bound elsewhere or expired.</summary>
    public sealed class ChangeCursorCodec
    {
        /// <summary>How long a cursor (and a tombstone) stays usable.</summary>
        public static readonly TimeSpan Retention = TimeSpan.FromDays(14);

        public ChangeCursorCodec(TimeProvider timeProvider)
        {
        }

        public string Encode(ChangeCursor cursor) => throw new NotImplementedException();

        public ChangeCursor Decode(string text, string feed, Guid organizationId, Guid? locationId) => throw new NotImplementedException();
    }
}
