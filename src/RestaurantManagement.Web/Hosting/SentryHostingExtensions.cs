using Sentry;
using Sentry.AspNetCore;

namespace RestaurantManagement.Web.Hosting
{
    public static class SentryHostingExtensions
    {
        private const string DsnKey = "Sentry:Dsn";
        private const string TracesSampleRateKey = "Sentry:TracesSampleRate";
        // Same name as the Identity refresh token header; that constant is internal to Identity.
        private const string RefreshTokenHeader = "X-Refresh-Token";
        private const double DevelopmentTracesSampleRate = 1.0;
        private const double DefaultTracesSampleRate = 0.1;
        private const double MinimumTracesSampleRate = 0.0;
        private const double MaximumTracesSampleRate = 1.0;

        // Configuration is read when the options are first resolved, not here, because
        // configuration sources added after the builder is created (deployment overrides) must count.
        public static WebApplicationBuilder AddSentryTracking(this WebApplicationBuilder builder)
        {
            // Sentry's own binding rejects a bad rate with an error that does not name the setting.
            // Options setups run in registration order, so registering this one first makes it fail first.
            builder.Services.Configure<SentryAspNetCoreOptions>(_ =>
                ReadTracesSampleRate(builder.Configuration, builder.Environment.IsDevelopment()));

            builder.WebHost.UseSentry(options =>
            {
                options.Dsn = builder.Configuration[DsnKey];
                options.Debug = builder.Environment.IsDevelopment();
                options.TracesSampleRate = ReadTracesSampleRate(builder.Configuration, builder.Environment.IsDevelopment());
                options.SetBeforeSend(RemoveRefreshTokenHeader);
                options.SetBeforeSendTransaction(RemoveRefreshTokenHeader);
            });

            return builder;
        }

        // The header carries a long-lived session secret and Sentry does not treat custom headers as sensitive.
        // Errors and traces both carry the request, so both go through here.
        private static SentryEvent? RemoveRefreshTokenHeader(SentryEvent sentryEvent, SentryHint hint)
        {
            RemoveRefreshTokenHeader(sentryEvent.Request);
            return sentryEvent;
        }

        private static SentryTransaction? RemoveRefreshTokenHeader(SentryTransaction transaction, SentryHint hint)
        {
            RemoveRefreshTokenHeader(transaction.Request);
            return transaction;
        }

        private static void RemoveRefreshTokenHeader(SentryRequest request)
        {
            var names = request.Headers.Keys
                .Where(name => string.Equals(name, RefreshTokenHeader, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var name in names)
                request.Headers.Remove(name);
        }

        private static double ReadTracesSampleRate(IConfiguration configuration, bool isDevelopment)
        {
            var defaultRate = isDevelopment ? DevelopmentTracesSampleRate : DefaultTracesSampleRate;
            var rate = configuration.GetValue<double?>(TracesSampleRateKey) ?? defaultRate;

            if (rate < MinimumTracesSampleRate || rate > MaximumTracesSampleRate)
                throw new InvalidOperationException(
                    $"{TracesSampleRateKey} must be between {MinimumTracesSampleRate} and {MaximumTracesSampleRate}.");

            return rate;
        }
    }
}
