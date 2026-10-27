namespace RestaurantManagement.Shared.Utils.Exceptions
{
    // Base class for known business/validation errors
    public class BusinessException : Exception
    {
        public int StatusCode { get; }

        // The stable, never-translated code of the refusal. STUB: filled by T1.
        public string Code { get; } = string.Empty;

        // Stays until T9 removes it; answers only the fallback code of its status.
        public BusinessException(string message, int statusCode = 400)
            : base(message)
        {
            StatusCode = statusCode;
        }

        // STUB (T1): the status comes from the code.
        public BusinessException(ErrorCode code, string message)
            : base(message)
        {
            throw new NotImplementedException();
        }
    }
}
