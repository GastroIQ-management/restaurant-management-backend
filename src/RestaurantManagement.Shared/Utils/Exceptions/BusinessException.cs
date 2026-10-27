namespace RestaurantManagement.Shared.Utils.Exceptions
{
    // Base class for known business/validation errors
    public class BusinessException : Exception
    {
        public int StatusCode { get; }

        // The stable, never-translated code of the refusal.
        public string Code { get; }

        // The status comes from the code, so the two cannot disagree.
        public BusinessException(ErrorCode code, string message)
            : base(message)
        {
            Code = code.Value;
            StatusCode = code.Status;
        }
    }
}
