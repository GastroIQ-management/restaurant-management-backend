using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RestaurantManagement.Web.OpenApi;

/// <summary>
/// Marks `version` as required on every update request. Requests carry it as a nullable field (some are
/// checked in the controller rather than with [Required]), so the schema would otherwise show it as optional.
/// </summary>
public class VersionRequiredSchemaFilter : ISchemaFilter
{
    private const string VersionField = "version";

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Only update requests hold a nullable version; responses always carry one.
        if (context.Type.GetProperty("Version")?.PropertyType != typeof(uint?) || !schema.Properties.ContainsKey(VersionField))
            return;

        schema.Required.Add(VersionField);
    }
}
