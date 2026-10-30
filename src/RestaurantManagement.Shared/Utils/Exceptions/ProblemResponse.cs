using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RestaurantManagement.Shared.Utils.Exceptions
{
    /// <summary>The one writer of error answers: RFC 9457 Problem Details with a stable code.</summary>
    public static class ProblemResponse
    {
        public static Task WriteAsync(HttpContext context, ErrorCode code, string title, string? detail = null, object? current = null)
        {
            // The last-resort path must never throw: a started response cannot be rewritten.
            if (context.Response.HasStarted)
            {
                return Task.CompletedTask;
            }

            var problem = new ProblemDetails { Status = code.Status, Title = title, Detail = detail };
            problem.Extensions["code"] = code.Value;
            problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
            if (current is not null)
            {
                problem.Extensions["current"] = current;
            }

            context.Response.StatusCode = code.Status;
            return context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }
    }
}
