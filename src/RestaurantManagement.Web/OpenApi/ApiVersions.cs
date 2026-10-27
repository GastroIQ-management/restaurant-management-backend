namespace RestaurantManagement.Web.OpenApi;

/// <summary>The API versions the host serves, each with its own OpenAPI document.</summary>
public static class ApiVersions
{
    public static IReadOnlyList<string> Supported { get; } = ["v1"];

    /// <summary>True when the route (for example "api/v1/menus", with or without a leading slash) belongs to the version (for example "v1").</summary>
    public static bool IncludesRoute(string version, string route)
        => route.TrimStart('/').StartsWith($"api/{version}/", StringComparison.Ordinal);
}
