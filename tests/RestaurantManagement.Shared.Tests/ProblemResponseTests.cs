using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;

namespace RestaurantManagement.Shared.Tests;

/// <summary>The one Shared writer of error answers.</summary>
public class ProblemResponseTests
{
    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted => true;
        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
    }

    [Fact]
    [Trait("Rule", "SH54")]
    public async Task WriteAsync_ResponseAlreadyStarted_WritesNothingAndDoesNotThrow()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var feature = new StartedResponseFeature();
        context.Features.Set<IHttpResponseFeature>(feature);

        // Act
        var act = () => ProblemResponse.WriteAsync(context, ErrorCodes.ServerUnexpectedError, "An unexpected error occurred.");

        // Assert
        await Should.NotThrowAsync(act, "the last-resort writer must never throw");
        feature.Body.Length.ShouldBe(0, "a response that already started cannot be rewritten, so nothing is added to it");
        feature.StatusCode.ShouldBe(200, "the status of a started response is never changed");
    }
}
