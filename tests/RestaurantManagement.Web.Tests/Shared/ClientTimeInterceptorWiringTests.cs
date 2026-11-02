using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using RestaurantManagement.Modules.Identity.Data;
using RestaurantManagement.Modules.Menu.Data;
using RestaurantManagement.Modules.Organization.Data;
using RestaurantManagement.Modules.Organization.Services;
using RestaurantManagement.Shared.Data.AuditLog;
using RestaurantManagement.Shared.Data.ClientTime;
using RestaurantManagement.Shared.Services.ClientTime;
using RestaurantManagement.Shared.Services.Organization;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

public class ClientTimeInterceptorWiringTests : IDisposable
{
    private readonly AuthApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    // Outcome 8: every module's writes are stamped
    [Theory]
    [InlineData(typeof(IdentityDbContext))]
    [InlineData(typeof(MenuDbContext))]
    [InlineData(typeof(OrganizationDbContext))]
    [InlineData(typeof(AuditLogDbContext))]
    [Trait("Rule", "SHD19")]
    public void DbContext_Resolved_HasTheClientTimeInterceptorAttached(Type contextType)
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);

        // Act
        var coreOptions = context.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>();

        // Assert
        coreOptions!.Interceptors!.OfType<ClientTimeSaveChangesInterceptor>().ShouldNotBeEmpty($"writes of {contextType.Name} must be stamped with the client times");
    }

    [Fact]
    [Trait("Rule", "SHD20")]
    public void ILocationBusinessDay_Resolved_IsTheOrganizationImplementation()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var contract = scope.ServiceProvider.GetRequiredService<ILocationBusinessDay>();

        // Assert
        contract.ShouldBeOfType<LocationBusinessDay>("the business day comes from the Organization module");
    }

    [Fact]
    [Trait("Rule", "SHD21")]
    public void IClientTimeContext_ResolvedFromTwoRequestScopes_IsADifferentInstanceEachTime()
    {
        // Arrange
        using var firstScope = _factory.Services.CreateScope();
        using var secondScope = _factory.Services.CreateScope();

        // Act
        var first = firstScope.ServiceProvider.GetRequiredService<IClientTimeContext>();
        var second = secondScope.ServiceProvider.GetRequiredService<IClientTimeContext>();

        // Assert
        first.ShouldNotBeSameAs(second, "the accepted times belong to one request and must never leak into another");
    }
}
