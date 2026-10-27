namespace RestaurantManagement.Shared.Utils.Exceptions
{
    // Base class for known business/validation errors
    public class BusinessException : Exception
    {
        public int StatusCode { get; }

        // The stable, never-translated code of the refusal.
        public string Code { get; }

        // Stays until T9 removes it; answers only the fallback code of its status.
        public BusinessException(string message, int statusCode = 400)
            : base(message)
        {
            StatusCode = statusCode;
            Code = ErrorCodes.Fallback(statusCode).Value;
        }

        // The status comes from the code, so the two cannot disagree.
        public BusinessException(ErrorCode code, string message)
            : base(message)
        {
            Code = code.Value;
            StatusCode = code.Status;
        }
    }
}
