using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using RestaurantManagement.Shared.Middleware;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RestaurantManagement.Web.OpenApi;

/// <summary>Declares the required X-App-Version header on every write operation, so generated clients send it.</summary>
public class AppVersionHeaderOperationFilter : IOperationFilter
{
    private static readonly string[] WriteMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod;
        if (method is null || !WriteMethods.Contains(method, StringComparer.OrdinalIgnoreCase))
            return;

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = AppVersionMiddleware.HeaderName,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Version of the client app sending the write, so the server can tell which app version produced a change.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                MaxLength = AppVersionMiddleware.MaxLength,
                Pattern = "^" + AppVersionMiddleware.PatternBody + "$"
            }
        });
    }
}
