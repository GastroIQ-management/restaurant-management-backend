using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Shared.Middleware
{
    /// <summary>
    /// Refuses a write (POST, PUT, PATCH, DELETE) that does not carry a valid X-App-Version header.
    /// The value is only tagged on Sentry events; it is never stored or used for access.
    /// </summary>
    public sealed partial class AppVersionMiddleware
    {
        public const string HeaderName = "X-App-Version";

        public const int MaxLength = 32;

        // ASCII digits only: \d would also accept non-ASCII digits. Shared with the OpenAPI document so both state one rule.
        public const string PatternBody = @"[0-9]+\.[0-9]+\.[0-9]+([-+][0-9A-Za-z.+-]+)?";

        private readonly RequestDelegate _next;

        public AppVersionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IStringLocalizer<SharedResource> localizer)
        {
            if (!IsWrite(context.Request.Method))
            {
                await _next(context);
                return;
            }

            var value = context.Request.Headers[HeaderName].ToString();
            if (string.IsNullOrEmpty(value))
                throw new BusinessException(ErrorCodes.AppVersionRequired, localizer["AppVersionRequired"]);

            if (value.Length > MaxLength || !SemanticVersion().IsMatch(value))
                throw new BusinessException(ErrorCodes.AppVersionInvalid, localizer["AppVersionInvalid"]);

            SentrySdk.ConfigureScope(scope => scope.SetTag("app_version", value));
            await _next(context);
        }

        private static bool IsWrite(string method)
            => HttpMethods.IsPost(method) || HttpMethods.IsPut(method)
               || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);

        [GeneratedRegex("^" + PatternBody + @"\z")]
        private static partial Regex SemanticVersion();
    }
}
