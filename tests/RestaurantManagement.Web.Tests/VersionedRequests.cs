using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Helpers for tests that edit or delete a record: read the version a client holds, send it back,
/// and read the answer. A response carries `version`; a change sends it in the body, a delete in
/// the `version` query parameter.
/// </summary>
internal static class VersionedRequests
{
    /// <summary>The `version` of the record, read from a GET answer (a single record, or a list searched by <paramref name="idInList"/>).</summary>
    public static async Task<uint> ReadVersionAsync(this HttpClient client, string url, Guid? idInList = null)
    {
        var body = await client.GetFromJsonAsync<JsonElement>(url);
        var record = idInList is null ? body : body.EnumerateArray().Single(e => e.GetProperty("id").GetGuid() == idInList);
        var version = record.GetProperty("version").GetUInt32();
        version.ShouldBeGreaterThan(0u, "a stored record always has a version (PostgreSQL never numbers a row 0)");
        return version;
    }

    public static Task<HttpResponseMessage> DeleteAtVersionAsync(this HttpClient client, string url, uint version)
        => client.DeleteAsync($"{url}?version={version}");

    public static async Task<JsonElement> ReadBodyAsync(this HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>();

    /// <summary>The names of the fields a 400 answer complains about, in lower case.</summary>
    public static async Task<List<string>> ReadErrorFieldsAsync(this HttpResponseMessage response)
    {
        var body = await response.ReadBodyAsync();
        return body.GetProperty("errors").EnumerateObject().Select(p => p.Name.ToLowerInvariant()).ToList();
    }
}
