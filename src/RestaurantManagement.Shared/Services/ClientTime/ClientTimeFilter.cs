using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace RestaurantManagement.Shared.Services.ClientTime
{
    /// <summary>Validates the device-time headers of a signed-in write and fills <see cref="IClientTimeContext"/>.</summary>
    public class ClientTimeFilter : IAsyncResourceFilter
    {
        private static readonly string[] WriteMethods = [HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete];

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var http = context.HttpContext;
            if (IsSignedInWrite(http))
            {
                var services = http.RequestServices;
                var receivedAt = (services.GetService<TimeProvider>() ?? TimeProvider.System).GetUtcNow();
                var validator = new ClientTimeValidator(services.GetRequiredService<IStringLocalizer<SharedResource>>());
                var times = validator.Validate(http.Request.Headers[ClientTimeHeaders.ActionTime], http.Request.Headers[ClientTimeHeaders.SentAt], receivedAt);
                services.GetRequiredService<IClientTimeContext>().Set(times);
            }

            await next();
        }

        private static bool IsSignedInWrite(HttpContext http)
        {
            if (!WriteMethods.Contains(http.Request.Method, StringComparer.OrdinalIgnoreCase))
                return false;

            var metadata = http.GetEndpoint()?.Metadata;
            return metadata?.GetMetadata<IAuthorizeData>() is not null && metadata.GetMetadata<IAllowAnonymous>() is null;
        }
    }
}
