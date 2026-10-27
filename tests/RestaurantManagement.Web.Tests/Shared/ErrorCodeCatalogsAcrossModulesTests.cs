using System.Text.RegularExpressions;
using RestaurantManagement.Modules.Identity.Services.Sessions;
using RestaurantManagement.Modules.Menu.Services;
using RestaurantManagement.Modules.Organization.Services;
using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// Every code of every module catalog (Shared, Menu, Organization and the Identity catalogs): one format,
/// never reused, and a retry set of exactly three codes.
/// </summary>
public class ErrorCodeCatalogsAcrossModulesTests
{
    private static readonly Regex CodeFormat = new("^[a-z]+(_[a-z]+)*\\.[a-z]+(_[a-z]+)*$");

    private static IReadOnlyList<ErrorCode> AllCodes() => ErrorCodeCatalogs.Read(
        typeof(ErrorCodes).Assembly,
        typeof(MenuErrorCodes).Assembly,
        typeof(OrganizationErrorCodes).Assembly,
        typeof(SessionErrorCodes).Assembly);

    [Fact]
    [Trait("Rule", "SH55")]
    public void AllCatalogs_EveryCode_HasTheSubjectDotReasonFormatAndIsUnique()
    {
        // Act
        var codes = AllCodes();

        // Assert
        codes.Count.ShouldBeGreaterThan(40, "every module adds its catalog to the Shared one");
        codes.ShouldAllBe(code => CodeFormat.IsMatch(code.Value), "a code is lowercase snake_case as subject.reason");
        codes.Select(code => code.Value).ShouldBeUnique("a code is never reused for another refusal");
    }

    [Fact]
    [Trait("Rule", "SH56")]
    public void AllCatalogs_RetryFlag_IsSetOnExactlyTheThreeRetryCodes()
    {
        // Act
        var retryCodes = AllCodes().Where(code => code.IsRetry).ToList();

        // Assert
        retryCodes.Select(code => code.Value).ShouldBe(
            new[] { "rate_limit.exceeded", "request.in_progress", "server.unexpected_error" },
            ignoreOrder: true,
            "the app retries only on 429, 5xx and a request still in progress: no other code is a retry code");
        retryCodes.Where(code => code.Status == 409).Select(code => code.Value).ShouldBe(
            new[] { "request.in_progress" }, "a 409 is retried only when it says the request is still in progress");
    }
}
