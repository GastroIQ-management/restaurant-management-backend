using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using RestaurantManagement.Shared;
using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Web.Hosting
{
    public static class ProblemDetailsHostingExtensions
    {
        private static readonly int[] FallbackStatuses = [400, 401, 403, 404, 405, 409, 415, 429, 500];

        /// <summary>
        /// Every error the framework makes (bodiless result, unknown route, wrong method or content type)
        /// becomes Problem Details with the fallback code of its status and a title in the caller's language.
        /// </summary>
        public static IServiceCollection AddFallbackProblemDetails(this IServiceCollection services)
        {
            services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            {
                var problem = context.ProblemDetails;
                var status = problem.Status ?? context.HttpContext.Response.StatusCode;
                if (problem.Extensions.ContainsKey("code") || !FallbackStatuses.Contains(status))
                {
                    return;
                }

                var fallback = ErrorCodes.Fallback(status);
                problem.Status = status;
                problem.Extensions["code"] = fallback.Value;
                problem.Title = Localize(context.HttpContext, FallbackTitleKey(fallback));
            });
            return services;
        }

        /// <summary>Answers an error of the host (token, permission, rate limit) with its code and a localized title.</summary>
        public static Task WriteProblemAsync(this HttpContext context, ErrorCode code, string titleKey)
            => ProblemResponse.WriteAsync(context, code, Localize(context, titleKey));

        private static string Localize(HttpContext context, string key)
            => context.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>()[key].Value;

        // "http.method_not_allowed" -> "HttpMethodNotAllowedTitle"; a 500 reuses the one unexpected-error title
        private static string FallbackTitleKey(ErrorCode code)
            => code.Status == 500 ? "UnexpectedError" : string.Concat(code.Value.Split('.', '_').Select(part => char.ToUpperInvariant(part[0]) + part[1..])) + "Title";
    }
}
