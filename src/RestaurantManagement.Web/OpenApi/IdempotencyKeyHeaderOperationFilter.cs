using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using RestaurantManagement.Shared.Services.Idempotency;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RestaurantManagement.Web.OpenApi;

/// <summary>Declares the optional Idempotency-Key header on every signed-in write that does not ignore keys.</summary>
public class IdempotencyKeyHeaderOperationFilter : IOperationFilter
{
    private static readonly string[] WriteMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod;
        if (method is null || !WriteMethods.Contains(method, StringComparer.OrdinalIgnoreCase))
            return;

        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (!metadata.OfType<IAuthorizeData>().Any()
            || metadata.OfType<IAllowAnonymous>().Any()
            || metadata.OfType<IgnoreIdempotencyKeyAttribute>().Any())
            return;

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = IdempotencyKeyFilter.HeaderName,
            In = ParameterLocation.Header,
            Required = false,
            Description = "A new random UUID (Universally Unique Identifier) per intended write. Sending the same key again with the same request returns the first answer instead of writing twice.",
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        });
    }
}
