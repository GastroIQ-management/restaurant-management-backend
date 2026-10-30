using Microsoft.AspNetCore.Mvc.Filters;

namespace RestaurantManagement.Shared.Services.Idempotency
{
    /// <summary>Checks, reserves, replays and saves the <c>Idempotency-Key</c> of a signed-in write, in one transaction with the write.</summary>
    public class IdempotencyKeyFilter : IAsyncResourceFilter
    {
        public const string HeaderName = "Idempotency-Key";

        public Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
            => throw new NotImplementedException();
    }
}
