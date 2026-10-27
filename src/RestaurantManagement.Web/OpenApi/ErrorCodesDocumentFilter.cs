using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using RestaurantManagement.Shared.Utils.Exceptions;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace RestaurantManagement.Web.OpenApi
{
    /// <summary>
    /// Adds the one Problem Details schema to the OpenAPI document, lists every catalog code in
    /// its `code` enum and declares it as the 4XX and 5XX answer of every operation.
    /// </summary>
    public class ErrorCodesDocumentFilter(params Assembly[] catalogAssemblies) : IDocumentFilter
    {
        public const string SchemaName = "ApiProblemDetails";
        private const string ContentType = "application/problem+json";

        public void Apply(OpenApiDocument document, DocumentFilterContext context)
        {
            var codes = ErrorCodeCatalogs.Read(catalogAssemblies);
            document.Components.Schemas[SchemaName] = BuildSchema(codes);

            var answer = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = SchemaName }
                }
            };

            foreach (var operation in document.Paths.Values.SelectMany(p => p.Operations.Values))
            {
                operation.Responses["4XX"] = BuildResponse("Client error", answer);
                operation.Responses["5XX"] = BuildResponse("Server error", answer);
            }
        }

        private static OpenApiResponse BuildResponse(string description, OpenApiMediaType answer) => new()
        {
            Description = description,
            Content = { [ContentType] = answer }
        };

        private static OpenApiSchema BuildSchema(IReadOnlyList<ErrorCode> codes)
        {
            var retryCodes = codes.Where(c => c.IsRetry).Select(c => c.Value).ToList();
            var codeSchema = new OpenApiSchema
            {
                Type = "string",
                Enum = codes.Select(c => (IOpenApiAny)new OpenApiString(c.Value)).ToList()
            };

            return new OpenApiSchema
            {
                Type = "object",
                Description = "Error answer (RFC 9457 Problem Details). Retry on network errors, 5xx and 429, "
                    + "and on the code request.in_progress; every other code is permanent or needs the user's "
                    + $"attention. Retry codes: {string.Join(", ", retryCodes)}.",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["type"] = new() { Type = "string", Nullable = true },
                    ["title"] = new() { Type = "string", Nullable = true },
                    ["status"] = new() { Type = "integer", Format = "int32" },
                    ["detail"] = new() { Type = "string", Nullable = true },
                    ["code"] = codeSchema,
                    ["traceId"] = new() { Type = "string", Nullable = true }
                }
            };
        }
    }
}
