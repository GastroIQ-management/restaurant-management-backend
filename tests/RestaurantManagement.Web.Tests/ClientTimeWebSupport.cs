using System.Globalization;
using System.Text;
using RestaurantManagement.Shared.Services.ClientTime;

namespace RestaurantManagement.Web.Tests;

/// <summary>Builds requests that carry their own device-time headers instead of the client defaults.</summary>
internal static class ClientTimeWebSupport
{
    public static string Iso(DateTimeOffset moment) => moment.ToString("O", CultureInfo.InvariantCulture);

    public static HttpRequestMessage Post(string url, string json, string actionTime, string sentAt, Guid? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        request.Headers.Add(ClientTimeHeaders.ActionTime, actionTime);
        request.Headers.Add(ClientTimeHeaders.SentAt, sentAt);
        if (idempotencyKey is { } key)
            request.Headers.Add("Idempotency-Key", key.ToString());
        return request;
    }

    /// <summary>A request whose device clock is right: it was sent now, for an action made <paramref name="actedAgo"/> earlier.</summary>
    public static HttpRequestMessage PostActedAgo(string url, string json, TimeSpan actedAgo, Guid? idempotencyKey = null)
    {
        var now = DateTimeOffset.UtcNow;
        return Post(url, json, Iso(now - actedAgo), Iso(now), idempotencyKey);
    }
}
