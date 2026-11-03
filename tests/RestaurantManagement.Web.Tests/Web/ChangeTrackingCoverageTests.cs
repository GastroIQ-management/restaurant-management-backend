using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantManagement.Shared.Data.Changes;
using RestaurantManagement.Shared.Data.Entities;
using Shouldly;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The rule for offline-capable modules: a tenant entity is either change tracked (stamp, tombstones, changes feed) or on a
/// written exclusion list. A future module (for example Products) cannot ship a tenant entity without one or the other.
/// </summary>
public class ChangeTrackingCoverageTests : IClassFixture<PostgresDatabaseFixture>
{
    // Tenant entities that are NOT synced to devices, each with the reason. Add a new entity here only with a reason.
    private static readonly Dictionary<string, string> NotSynced = new()
    {
        ["OrganizationMember"] = "Identity members: access data, not synced to devices",
        ["MemberLocationLevel"] = "Identity access grid: access data, not synced to devices",
        ["MemberLocationRole"] = "Identity member roles: access data, not synced to devices",
        ["OrganizationRole"] = "Identity roles: access data, not synced to devices",
        ["OrganizationRoleLevel"] = "Identity role levels: access data, not synced to devices",
        ["OrganizationInvitation"] = "Identity invitations: access data, not synced to devices",
        ["OrganizationInvitationRole"] = "Identity invitations: access data, not synced to devices",
        ["OrganizationInvitationLevel"] = "Identity invitations: access data, not synced to devices",
        ["OrganizationSettings"] = "Plan and trial settings: not offline data yet",
        ["AuditLogEntry"] = "Server history: never synced to devices",
    };

    // Tracked tables whose rows are never deleted, so they need no tombstone trigger.
    private static readonly Dictionary<string, string> NeverDeleted = new()
    {
        ["locations"] = "Locations are closed, never deleted: a close is a stamped change",
    };

    private readonly PostgresDatabaseFixture _postgres;

    public ChangeTrackingCoverageTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private static List<Type> ModuleDbContextTypes()
    {
        var host = typeof(Program).Assembly;
        return host.GetReferencedAssemblies()
            .Where(a => a.Name!.StartsWith("RestaurantManagement.", StringComparison.Ordinal))
            .Select(System.Reflection.Assembly.Load)
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t))
            .ToList();
    }

    private static List<Microsoft.EntityFrameworkCore.Metadata.IEntityType> TenantEntityTypes(TenantApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return ModuleDbContextTypes()
            .SelectMany(t => ((DbContext)scope.ServiceProvider.GetRequiredService(t)).Model.GetEntityTypes())
            .Where(e => typeof(TenantEntity).IsAssignableFrom(e.ClrType) && !e.IsOwned())
            .DistinctBy(e => e.ClrType)
            .ToList();
    }

    [Fact]
    [Trait("Rule", "W37")]
    public async Task EveryTenantEntity_OfEveryModuleContext_IsChangeTrackedWithItsStampMappedOrExcludedWithAReason()
    {
        // Arrange
        await using var factory = await TenantApiFactory.CreateAsync(_postgres);
        var entities = TenantEntityTypes(factory);

        // Act
        var untracked = entities
            .Where(e => !typeof(IChangeTracked).IsAssignableFrom(e.ClrType) && !NotSynced.ContainsKey(e.ClrType.Name))
            .Select(e => e.ClrType.Name).ToList();
        var stampNotMapped = entities
            .Where(e => typeof(IChangeTracked).IsAssignableFrom(e.ClrType))
            .Where(e => e.FindProperty(nameof(IChangeTracked.ChangeStamp))?.GetColumnName() != "change_stamp")
            .Select(e => e.ClrType.Name).ToList();

        // Assert
        untracked.ShouldBeEmpty($"tenant entities without change tracking: {string.Join(", ", untracked)}. For each: implement IChangeTracked, call ApplyChangeTracking in its DbContext (with a tombstone table when rows can be deleted), add the ChangeTrackingSql migration and a changes feed, or add it to NotSynced in ChangeTrackingCoverageTests with a reason");
        stampNotMapped.ShouldBeEmpty($"tracked entities whose stamp is not mapped: {string.Join(", ", stampNotMapped)}. Call ApplyChangeTracking in their DbContext");
    }

    [Fact]
    [Trait("Rule", "W38")]
    public async Task EveryModuleWithATrackedEntity_ExposesAChangesFeed()
    {
        // Arrange
        await using var factory = await TenantApiFactory.CreateAsync(_postgres);
        var trackedAssemblies = TenantEntityTypes(factory)
            .Where(e => typeof(IChangeTracked).IsAssignableFrom(e.ClrType))
            .Select(e => e.ClrType.Assembly).Distinct().ToList();
        var feedAssemblies = factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Where(a => a.AttributeRouteInfo!.Template!.EndsWith("/changes", StringComparison.Ordinal))
            .Where(a => a.ActionConstraints!.OfType<Microsoft.AspNetCore.Mvc.ActionConstraints.HttpMethodActionConstraint>().Any(c => c.HttpMethods.Contains("GET")))
            .Select(a => a.ControllerTypeInfo.Assembly).Distinct().ToList();

        // Act
        var withoutFeed = trackedAssemblies.Where(a => !feedAssemblies.Contains(a)).Select(a => a.GetName().Name).ToList();

        // Assert
        trackedAssemblies.ShouldNotBeEmpty("no entity is change tracked yet: Menu and Organization must track theirs");
        withoutFeed.ShouldBeEmpty($"modules with tracked entities but no GET .../changes route: {string.Join(", ", withoutFeed)}. Expose a changes feed per resource with the same access as its normal read");
    }

    [Fact]
    [Trait("Rule", "W39")]
    public async Task EveryTrackedTable_HasItsStampTriggerAndATombstoneTriggerUnlessRowsAreNeverDeleted()
    {
        // Arrange
        await using var factory = await TenantApiFactory.CreateAsync(_postgres);
        var tables = TenantEntityTypes(factory)
            .Where(e => typeof(IChangeTracked).IsAssignableFrom(e.ClrType))
            .Select(e => e.GetTableName()!).Distinct().ToList();
        var triggersByTable = new Dictionary<string, List<string>>();
        foreach (var table in tables)
            triggersByTable[table] = await factory.InOrganizationAsync<Modules.Menu.Data.MenuDbContext, List<string>>(Guid.NewGuid(), db =>
                db.Database.SqlQuery<string>(
                    $"SELECT t.tgname AS \"Value\" FROM pg_trigger t JOIN pg_class c ON c.oid = t.tgrelid WHERE c.relname = {table} AND NOT t.tgisinternal").ToListAsync());

        // Act
        var noStamp = tables.Where(t => !triggersByTable[t].Any(n => n.Contains("_stamp"))).ToList();
        var noTombstone = tables.Where(t => !NeverDeleted.ContainsKey(t) && !triggersByTable[t].Any(n => n.Contains("_tombstone"))).ToList();

        // Assert
        tables.ShouldNotBeEmpty("no table is change tracked yet: Menu and Organization must track theirs");
        noStamp.ShouldBeEmpty($"tracked tables without a stamp trigger: {string.Join(", ", noStamp)}. Add the ChangeTrackingSql stamp trigger to the module migration");
        noTombstone.ShouldBeEmpty($"tracked tables without a tombstone trigger: {string.Join(", ", noTombstone)}. Add a tombstone trigger to the module migration, or add the table to NeverDeleted in ChangeTrackingCoverageTests with a reason");
    }
}
