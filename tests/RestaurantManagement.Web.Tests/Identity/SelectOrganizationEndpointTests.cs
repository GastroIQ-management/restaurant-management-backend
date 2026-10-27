using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RestaurantManagement.Modules.Identity.Models.Sessions;
using RestaurantManagement.Modules.Identity.Services.Sessions;
using RestaurantManagement.Modules.Organization.Entities;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

public class SelectOrganizationEndpointTests : IClassFixture<PostgresDatabaseFixture>, IDisposable
{
    private const string SelectOrganizationUrl = "/api/auth/select-organization";
    private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    private static readonly Guid MariaId = Guid.Parse("11111111-aaaa-4aaa-8aaa-00000000000a");

    private readonly AuthApiFactory _factory = new();
    private readonly PostgresDatabaseFixture _postgres;

    public SelectOrganizationEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    public void Dispose()
    {
        _factory.Dispose();
    }

    // Scenario 31
    [Fact]
    [Trait("Rule", "S1")]
    public async Task SelectOrganization_NoAuthenticationToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new SelectOrganizationRequest { OrganizationId = CantinaId };

        // Act
        var response = await client.PostAsJsonAsync(SelectOrganizationUrl, request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "only a signed-in person can choose an organization");
    }

    [Fact]
    [Trait("Rule", "S8")]
    public async Task SelectOrganization_SignedInWithoutRefreshCookie_ReturnsUnauthorizedAndSetsNoCookie()
    {
        // Arrange
        using var tenant = await TenantApiFactory.CreateAsync(_postgres);
        await tenant.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await tenant.SeedUserAsync(MariaId, "maria@cantinadanonna.com.br", "Maria", TenantApiFactory.OwnerOf(CantinaId));
        using var app = tenant.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISessionService>();
            services.AddScoped<ISessionService, SessionService>();
        }));
        var client = tenant.ClientFor(MariaId, null);
        var request = new SelectOrganizationRequest { OrganizationId = CantinaId };

        // Act
        var response = await app.CreateClient().SendAsync(WithToken(client, request));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, "a switch without the caller's own refresh cookie is refused, so it can never start a fresh 30 days");
        response.Headers.Contains("Set-Cookie").ShouldBeFalse("a refused switch sets no new refresh cookie");
    }

    private static HttpRequestMessage WithToken(HttpClient signedIn, SelectOrganizationRequest body)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, SelectOrganizationUrl) { Content = JsonContent.Create(body) };
        message.Headers.Authorization = signedIn.DefaultRequestHeaders.Authorization;
        return message;
    }
}
