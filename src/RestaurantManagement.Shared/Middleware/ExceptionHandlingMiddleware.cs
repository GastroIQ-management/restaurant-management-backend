using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Sentry;
using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Shared.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (VersionConflictException conflict)
            {
                _logger.LogWarning("Version conflict");

                var localizer = context.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
                await ProblemResponse.WriteAsync(context, ErrorCodes.VersionConflict, localizer["VersionConflict"].Value, current: conflict.Current);
            }
            catch (BusinessException bex)
            {
                // ✅ Business error: skip Sentry
                _logger.LogWarning(bex, "Business exception");

                await ProblemResponse.WriteAsync(context, new ErrorCode(bex.Code, bex.StatusCode), bex.Message);
            }
            catch (Exception ex)
            {
                // ✅ Unexpected error: send to Sentry
                SentrySdk.CaptureException(ex);
                _logger.LogError(ex, "Unhandled exception");

                // The last-resort handler must not throw, so fall back to English without a localizer.
                var localizer = context.RequestServices?.GetService<IStringLocalizer<SharedResource>>();
                var title = localizer?["UnexpectedError"].Value ?? "An unexpected error occurred.";

                await ProblemResponse.WriteAsync(context, ErrorCodes.ServerUnexpectedError, title, _env.IsDevelopment() ? ex.Message : null);
            }
        }

    }
}