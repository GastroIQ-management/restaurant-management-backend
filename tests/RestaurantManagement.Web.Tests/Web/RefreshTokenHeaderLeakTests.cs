using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RestaurantManagement.Modules.Identity.Services.Sessions;
using RestaurantManagement.Shared.Utils.Exceptions;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The native refresh token (X-Refresh-Token header) never reaches error reports or logs. The Sentry test runs the
/// app's own Sentry settings through a real Sentry client whose transport only records what would be sent.
/// </summary>
public class RefreshTokenHeaderLeakTests
{
    private const string Secret = "bmF0aXZlLWFwcC1sZWFrLWNoZWNrLXJlZnJlc2gtdG9rZW4";

    private sealed class RecordingTransport : ITransport
    {
        public StringBuilder Sent { get; } = new();

        public async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            await envelope.SerializeAsync(stream, null, cancellationToken);
            Sent.Append(Encoding.UTF8.GetString(stream.ToArray()));
        }
    }

    private sealed class LogRecorder : ILoggerProvider
    {
        public List<string> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new Recorded(this);

        public void Dispose() { }

        private sealed class Recorded(LogRecorder owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (owner.Entries)
                    owner.Entries.Add(formatter(state, exception) + " " + exception);
            }
        }
    }

    [Theory]
    [InlineData("X-Refresh-Token")]
    [InlineData("x-refresh-token")]
    [Trait("Rule", "T173")]
    [Trait("Rule", "W33")]
    public async Task SentryEvent_RequestCarriesRefreshTokenHeader_LeavesWithoutTheTokenAndKeepsOtherHeaders(string headerName)
    {
        // Arrange
        using var factory = new AuthApiFactory();
        var options = factory.Services.GetRequiredService<IOptions<SentryAspNetCoreOptions>>().Value;
        var transport = new RecordingTransport();
        options.Dsn = "https://0123456789abcdef@o0.ingest.sentry.io/1";
        options.Transport = transport;
        using var client = new SentryClient(options);
        var sentryEvent = new SentryEvent(new InvalidOperationException("kitchen printer offline"));
        sentryEvent.Request.Headers[headerName] = Secret;
        sentryEvent.Request.Headers["Accept-Language"] = "pt-BR,pt;q=0.9";

        // Act
        client.CaptureEvent(sentryEvent);
        await client.FlushAsync(TimeSpan.FromSeconds(10));

        // Assert
        transport.Sent.Length.ShouldBeGreaterThan(0, "the event is sent");
        transport.Sent.ToString().ShouldNotContain(Secret, customMessage: "the refresh token header value must never reach Sentry, whatever the route or the header's letter case");
        transport.Sent.ToString().ShouldContain("pt-BR,pt;q=0.9", customMessage: "other headers are kept so errors stay easy to investigate");
    }

    [Theory]
    [InlineData("X-Refresh-Token")]
    [InlineData("x-refresh-token")]
    [Trait("Rule", "T173")]
    [Trait("Rule", "W33")]
    public async Task SentryTransaction_RequestCarriesRefreshTokenHeader_LeavesWithoutTheTokenAndKeepsOtherHeaders(string headerName)
    {
        // Arrange
        using var factory = new AuthApiFactory();
        var options = factory.Services.GetRequiredService<IOptions<SentryAspNetCoreOptions>>().Value;
        var transport = new RecordingTransport();
        options.Dsn = "https://0123456789abcdef@o0.ingest.sentry.io/1";
        options.Transport = transport;
        options.TracesSampleRate = 1.0;
        using var sdk = SentrySdk.Init(options);
        var transaction = SentrySdk.StartTransaction("POST /api/v1/auth/refresh", "http.server");
        transaction.Request.Headers[headerName] = Secret;
        transaction.Request.Headers["Accept-Language"] = "pt-BR,pt;q=0.9";

        // Act
        transaction.Finish();
        await SentrySdk.FlushAsync(TimeSpan.FromSeconds(10));

        // Assert
        transport.Sent.Length.ShouldBeGreaterThan(0, "the transaction is sent");
        transport.Sent.ToString().ShouldNotContain(Secret, customMessage: "the refresh token header value must never reach Sentry traces, whatever the letter case");
        transport.Sent.ToString().ShouldContain("pt-BR,pt;q=0.9", customMessage: "other headers are kept so traces stay easy to investigate");
    }

    // Weak guard: with the session service substituted this only proves the controller and middleware never log or echo the header token
    [Fact]
    [Trait("Rule", "T174")]
    [Trait("Rule", "W34")]
    public async Task Refresh_NativeTokenRefused_NeitherLogsNorAnswerContainTheToken()
    {
        // Arrange
        var logs = new LogRecorder();
        using var factory = new AuthApiFactory();
        factory.SessionService.RefreshTokenAsync(Arg.Any<string?>())
            .Throws(new BusinessException(SessionErrorCodes.InvalidOrExpiredSession, "InvalidOrExpiredToken"));
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddProvider(logs);
        }));
        var client = app.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.TryAddWithoutValidation("Origin", "https://localhost");
        request.Headers.Add("X-Refresh-Token", Secret);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "an unusable token is refused");
        (await response.Content.ReadAsStringAsync()).ShouldNotContain(Secret, customMessage: "the refusal never echoes the token");
        logs.Entries.Count.ShouldBeGreaterThan(0, "the log capture works");
        logs.Entries.ShouldNotContain(e => e.Contains(Secret), "no log entry holds the refresh token");
    }
}
