using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;

namespace RestaurantManagement.Shared.Tests;

/// <summary>The Shared catalog of error codes and the fallback code of each status the host can answer.</summary>
public class ErrorCodeCatalogTests
{
    [Theory]
    [InlineData("validation.failed", 400)]
    [InlineData("auth.unauthorized", 401)]
    [InlineData("auth.invalid_token", 401)]
    [InlineData("access.denied", 403)]
    [InlineData("organization.required", 403)]
    [InlineData("organization.not_found", 404)]
    [InlineData("location.not_found", 404)]
    [InlineData("rate_limit.exceeded", 429)]
    [InlineData("server.unexpected_error", 500)]
    [InlineData("request.in_progress", 409)]
    [InlineData("location.invalid_id", 400)]
    [InlineData("location.access_denied", 403)]
    [InlineData("audit_log.invalid_date_range", 400)]
    [InlineData("audit_log.unknown_action", 400)]
    [InlineData("tenant.mismatch", 403)]
    [Trait("Rule", "SH52")]
    public void Read_SharedCatalog_ListsEveryNamedCodeWithItsStatus(string value, int status)
    {
        // Arrange
        var assembly = typeof(ErrorCodes).Assembly;

        // Act
        var codes = ErrorCodeCatalogs.Read(assembly);

        // Assert
        var code = codes.SingleOrDefault(c => c.Value == value);
        code.ShouldNotBeNull($"the Shared catalog holds {value}");
        code.Status.ShouldBe(status, "a code always answers one status");
    }

    [Theory]
    [InlineData(400, "http.bad_request")]
    [InlineData(401, "http.unauthorized")]
    [InlineData(403, "http.forbidden")]
    [InlineData(404, "http.not_found")]
    [InlineData(405, "http.method_not_allowed")]
    [InlineData(409, "http.conflict")]
    [InlineData(415, "http.unsupported_media_type")]
    [InlineData(429, "http.too_many_requests")]
    [InlineData(500, "http.internal_server_error")]
    [Trait("Rule", "SH53")]
    public void Fallback_StatusTheHostCanAnswer_ReturnsItsFallbackCodeWithThatStatus(int status, string expected)
    {
        // Act
        var code = ErrorCodes.Fallback(status);

        // Assert
        code.Value.ShouldBe(expected, "every status the host can answer has a fallback code");
        code.Status.ShouldBe(status, "the fallback code answers its own status");
        code.IsRetry.ShouldBeFalse("a fallback code is never a retry code: the app retries by status class and the three retry codes");
    }
}
