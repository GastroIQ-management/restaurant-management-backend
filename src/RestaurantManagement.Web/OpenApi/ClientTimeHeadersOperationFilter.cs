using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using RestaurantManagement.Shared.Services.ClientTime;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RestaurantManagement.Web.OpenApi;

/// <summary>Declares the two required device-time headers on every signed-in write, so generated clients send them.</summary>
public class ClientTimeHeadersOperationFilter : IOperationFilter
{
    private static readonly string[] WriteMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod;
        if (method is null || !WriteMethods.Contains(method, StringComparer.OrdinalIgnoreCase))
            return;

        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (!metadata.OfType<IAuthorizeData>().Any() || metadata.OfType<IAllowAnonymous>().Any())
            return;

        operation.Parameters ??= [];
        operation.Parameters.Add(Header(ClientTimeHeaders.ActionTime,
            "When the user acted on the device, as an RFC 3339 date and time with an offset (for example 2026-10-30T14:05:00-03:00)."));
        operation.Parameters.Add(Header(ClientTimeHeaders.SentAt,
            "The device clock when the request is sent, as an RFC 3339 date and time with an offset. The server uses it to correct the action time for a wrong device clock."));
    }

    private static OpenApiParameter Header(string name, string description) => new()
    {
        Name = name,
        In = ParameterLocation.Header,
        Required = true,
        Description = description,
        Schema = new OpenApiSchema { Type = "string", Format = "date-time", MaxLength = ClientTimeValidator.MaxLength }
    };
}
