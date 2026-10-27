using Microsoft.AspNetCore.Http;

namespace RestaurantManagement.Shared.Utils.Exceptions
{
    /// <summary>The one writer of error answers: RFC 9457 Problem Details with a stable code.</summary>
    public static class ProblemResponse
    {
        // STUB (T1): status = code.Status; writes nothing once the response has started.
        public static Task WriteAsync(HttpContext context, ErrorCode code, string title, string? detail = null)
            => throw new NotImplementedException();
    }
}
