using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RestaurantManagement.Web.OpenApi;

/// <summary>Declares the optional X-Refresh-Token header on the three operations that read the refresh token.</summary>
public class RefreshTokenHeaderOperationFilter : IOperationFilter
{
    // Same name as Identity's RefreshTokenTransport.HeaderName, which is internal to that module.
    private const string HeaderName = "X-Refresh-Token";

    private static readonly string[] Routes =
        ["api/v1/auth/refresh", "api/v1/auth/select-organization", "api/v1/auth/logout"];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var description = context.ApiDescription;
        if (!string.Equals(description.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
            || !Routes.Contains(description.RelativePath, StringComparer.OrdinalIgnoreCase))
            return;

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HeaderName,
            In = ParameterLocation.Header,
            Required = false,
            Description = "For native apps only: the refresh token. Web clients send it in the session cookie instead.",
            Schema = new OpenApiSchema { Type = "string" }
        });
    }
}
