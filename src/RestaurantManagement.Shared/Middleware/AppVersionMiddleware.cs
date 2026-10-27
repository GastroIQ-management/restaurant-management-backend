using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace RestaurantManagement.Shared.Middleware
{
    /// <summary>
    /// Refuses a write (POST, PUT, PATCH, DELETE) that does not carry a valid X-App-Version header.
    /// </summary>
    public sealed class AppVersionMiddleware
    {
        public const string HeaderName = "X-App-Version";

        private readonly RequestDelegate _next;

        public AppVersionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context, IStringLocalizer<SharedResource> localizer)
            => throw new NotImplementedException();
    }
}
