using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>The body of a "changes since" read.</summary>
public sealed record ChangesBody<T>(List<T> Changed, List<Guid> Deleted, string Cursor, bool HasMore);

/// <summary>Helpers for the "changes since" reads of menus, links, categories and locations.</summary>
internal static class ChangesRequests
{
    public static Task<HttpResponseMessage> GetChangesAsync(this HttpClient client, string route, string? cursor = null, int? pageSize = null)
    {
        var query = new List<string>();
        if (cursor is not null)
            query.Add($"cursor={Uri.EscapeDataString(cursor)}");
        if (pageSize is not null)
            query.Add($"pageSize={pageSize}");
        return client.GetAsync(query.Count == 0 ? route : $"{route}?{string.Join('&', query)}");
    }

    /// <summary>Reads one page and checks the answer is 200.</summary>
    public static async Task<ChangesBody<T>> ReadChangesAsync<T>(this HttpClient client, string route, string? cursor = null, int? pageSize = null)
    {
        var response = await client.GetChangesAsync(route, cursor, pageSize);
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK, "the read is allowed");
        return (await response.Content.ReadFromJsonAsync<ChangesBody<T>>(new JsonSerializerOptions(JsonSerializerDefaults.Web)))!;
    }

    /// <summary>The fixed `code` of a Problem Details answer.</summary>
    public static async Task<string> ReadCodeAsync(this HttpResponseMessage response)
        => (await response.ReadBodyAsync()).GetProperty("code").GetString()!;
}
