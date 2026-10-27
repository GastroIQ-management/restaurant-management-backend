using System.Net;
using System.Net.Http.Headers;
using NSubstitute;
using RestaurantManagement.Shared.Auth;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// A signed-in user whose stored access lacks the level gets the translated "access denied" message.
/// </summary>
public class AccessDeniedMessageTests : IDisposable
{
    private const string MembersUrl = "/api/test-probe/members";
    private const string EnglishAccessDeniedMessage = "You do not have permission to access this resource. Please contact your administrator if you believe this is an error.";
    private const string PortugueseAccessDeniedMessage = "Você não tem permissão para acessar este recurso. Entre em contato com o administrador se acreditar que isso é um erro.";

    private static readonly Guid MariaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");
    private static readonly Guid CantinaId = Guid.Parse("c1c1c1c1-0000-4000-8000-000000000001");

    private static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");

    private readonly PermissionProbeApiFactory _factory = new();

    public AccessDeniedMessageTests()
    {
        // A Waiter: Menus Read and Orders Manage at Batel, nothing in Members.
        _factory.Organizations.GetNonClosedLocationIdsAsync(CantinaId).Returns(new List<Guid> { BatelId });
        _factory.MemberAccess.GetAccessGridAsync(MariaId, CantinaId).Returns(new AccessGridDto
        {
            Cells = new[]
            {
                new AccessCellDto { LocationId = BatelId, Module = AccessModule.Menus, Level = AccessLevel.Read },
                new AccessCellDto { LocationId = BatelId, Module = AccessModule.Orders, Level = AccessLevel.Manage }
            }
        });
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task<HttpResponseMessage> WaiterGetsMembersAsync(string? language = null)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, MembersUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", TestAccessTokens.Create(_factory.Services, MariaId, CantinaId));
        if (language is not null)
            request.Headers.Add("Accept-Language", language);
        return await client.SendAsync(request);
    }

    [Fact]
    [Trait("Rule", "W3")]
    public async Task MembersManagePolicy_WaiterWithoutMembersAccess_ReturnsAccessDeniedMessageInEnglish()
    {
        // Arrange
        // Act
        var response = await WaiterGetsMembersAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "a waiter holds no level in the Members module");
        (await LocalizedResponses.MessageAsync(response)).ShouldBe(EnglishAccessDeniedMessage, "a signed-in user who is not allowed gets the translated access denied message");
    }

    [Fact]
    [Trait("Rule", "W3")]
    public async Task MembersManagePolicy_WaiterWithoutMembersAccessInPortuguese_ReturnsAccessDeniedMessageInPortuguese()
    {
        // Arrange
        // Act
        var response = await WaiterGetsMembersAsync("pt");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, "a waiter holds no level in the Members module");
        (await LocalizedResponses.MessageAsync(response)).ShouldBe(PortugueseAccessDeniedMessage, "the access denied answer follows the caller's language");
    }

    [Fact]
    [Trait("Rule", "W3")]
    public async Task MembersManagePolicy_WaiterWithoutMembersAccess_AnswersAccessDeniedCode()
    {
        // Arrange
        // Act
        var response = await WaiterGetsMembersAsync();

        // Assert
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json", "a refused permission is answered as Problem Details");
        (await LocalizedResponses.CodeAsync(response)).ShouldBe("access.denied", "a member without the level gets the access denied code");
    }

    [Fact]
    [Trait("Rule", "W26")]
    public async Task MembersManagePolicy_ManageInAnotherOrganizationOnly_AnswersTheSameBodyAndCodeAsAWaiter()
    {
        // Arrange
        var pedroId = Guid.Parse("33333333-cccc-4ccc-8ccc-00000000000c");
        var trattoriaId = Guid.Parse("b2b2b2b2-0000-4000-8000-000000000002");
        var trattoriaCentroId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000003");
        _factory.Organizations.GetNonClosedLocationIdsAsync(trattoriaId).Returns(new List<Guid> { trattoriaCentroId });
        _factory.MemberAccess.GetAccessGridAsync(pedroId, trattoriaId).Returns(new AccessGridDto
        {
            Cells = new[] { new AccessCellDto { LocationId = trattoriaCentroId, Module = AccessModule.Members, Level = AccessLevel.Manage } }
        });
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, MembersUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", TestAccessTokens.Create(_factory.Services, pedroId, CantinaId));

        // Act
        var waiter = await WaiterGetsMembersAsync();
        var pedro = await client.SendAsync(request);

        // Assert
        pedro.StatusCode.ShouldBe(waiter.StatusCode, "Manage in another organization gives no access in this one");
        (await LocalizedResponses.CodeAsync(pedro)).ShouldBe("access.denied", "the refusal carries the access denied code");
        (await LocalizedResponses.MessageAsync(pedro)).ShouldBe(await LocalizedResponses.MessageAsync(waiter), "the other organization's access changes nothing in the answer");
    }
}
