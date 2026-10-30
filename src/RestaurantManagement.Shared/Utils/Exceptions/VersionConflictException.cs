namespace RestaurantManagement.Shared.Utils.Exceptions
{
    /// <summary>
    /// A change was sent with an old version of the record. Answered as 409 `version.conflict`; <see cref="Current"/> is the record as stored now (the caller's normal response model).
    /// </summary>
    public class VersionConflictException : Exception
    {
        public object Current { get; }

        public VersionConflictException(object current)
            : base("The record was changed since it was read.")
        {
            Current = current;
        }
    }
}
