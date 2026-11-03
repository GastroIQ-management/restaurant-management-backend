using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using RestaurantManagement.Modules.Menu.Data;
using RestaurantManagement.Modules.Menu.Models;
using RestaurantManagement.Shared;
using RestaurantManagement.Shared.Auth;
using RestaurantManagement.Shared.Data.Changes;
using RestaurantManagement.Shared.Data.Tenancy;
using RestaurantManagement.Shared.Models;
using RestaurantManagement.Shared.Services.Changes;
using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Menu.Services
{
    public class MenuChangesService : IMenuChangesService
    {
        private const string MenuType = "Menu";
        private const string LinkType = "LocationMenu";
        private const string CategoryType = "MenuCategory";

        private readonly MenuDbContext _menuDb;
        private readonly ChangeReader _reader;
        private readonly MenuAccessChecker _access;
        private readonly ITenantContext _tenantContext;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public MenuChangesService(
            MenuDbContext menuDb,
            ChangeReader reader,
            IAccessGuard accessGuard,
            ITenantContext tenantContext,
            IStringLocalizer<SharedResource> localizer)
        {
            _menuDb = menuDb;
            _reader = reader;
            _access = new MenuAccessChecker(accessGuard);
            _tenantContext = tenantContext;
            _localizer = localizer;
        }

        public async Task<ChangesPageResponse<MenuResponse>> GetMenuChangesAsync(ChangesQuery query, CancellationToken cancellationToken)
        {
            var (organizationId, locationId) = await RequireScopeAsync();

            // A menu changes when its own row or its link to this location does.
            var records = _menuDb.LocationMenus
                .Where(l => l.LocationId == locationId)
                .Select(l => new ChangeRow(l.MenuId, l.Menu.ChangeStamp > l.ChangeStamp ? l.Menu.ChangeStamp : l.ChangeStamp));

            return await _reader.ReadAsync(_menuDb, new ChangeFeedRead<MenuResponse>(
                ChangeFeeds.Menus, organizationId, locationId, query, records, Tombstones(organizationId, locationId, MenuType),
                async (ids, ct) => (await _menuDb.Menus.Include(m => m.LocationMenus).Where(m => ids.Contains(m.Id)).ToListAsync(ct))
                    .ToDictionary(m => m.Id, MenuService.MapToResponse)), cancellationToken);
        }

        public async Task<ChangesPageResponse<LocationMenuResponse>> GetLocationLinkChangesAsync(ChangesQuery query, CancellationToken cancellationToken)
        {
            var (organizationId, locationId) = await RequireScopeAsync();

            var records = _menuDb.LocationMenus
                .Where(l => l.LocationId == locationId)
                .Select(l => new ChangeRow(l.Id, l.ChangeStamp));

            return await _reader.ReadAsync(_menuDb, new ChangeFeedRead<LocationMenuResponse>(
                ChangeFeeds.MenuLocationLinks, organizationId, locationId, query, records, Tombstones(organizationId, locationId, LinkType),
                async (ids, ct) => (await _menuDb.LocationMenus.Where(l => ids.Contains(l.Id)).ToListAsync(ct))
                    .ToDictionary(l => l.Id, ToResponse)), cancellationToken);
        }

        public async Task<ChangesPageResponse<MenuCategoryResponse>> GetMenuCategoryChangesAsync(ChangesQuery query, CancellationToken cancellationToken)
        {
            var (organizationId, locationId) = await RequireScopeAsync();

            // Categories with no location are not synced.
            var records = _menuDb.MenuCategories
                .Where(c => c.LocationId == locationId)
                .Select(c => new ChangeRow(c.Id, c.ChangeStamp));

            return await _reader.ReadAsync(_menuDb, new ChangeFeedRead<MenuCategoryResponse>(
                ChangeFeeds.MenuCategories, organizationId, locationId, query, records, Tombstones(organizationId, locationId, CategoryType),
                async (ids, ct) => (await _menuDb.MenuCategories.Where(c => ids.Contains(c.Id)).ToListAsync(ct))
                    .ToDictionary(c => c.Id, MenuCategoryService.MapToResponse)), cancellationToken);
        }

        // The tombstone table has no query filter, so organization and location are always named here.
        private IQueryable<ChangeRow> Tombstones(Guid organizationId, Guid locationId, string entityType)
            => _menuDb.Set<ChangeTombstone>()
                .Where(t => t.OrganizationId == organizationId && t.LocationId == locationId && t.EntityType == entityType)
                .Select(t => new ChangeRow(t.EntityId, t.ChangeStamp));

        private async Task<(Guid OrganizationId, Guid LocationId)> RequireScopeAsync()
        {
            var locationId = _tenantContext.LocationId
                ?? throw new BusinessException(ErrorCodes.LocationRequired, _localizer["LocationRequired"].Value);
            var organizationId = _tenantContext.OrganizationId
                ?? throw new BusinessException(ErrorCodes.OrganizationRequired, _localizer["OrganizationRequired"].Value);

            await _access.RequireReadAtLocationAsync(locationId);
            return (organizationId, locationId);
        }

        private static LocationMenuResponse ToResponse(Entities.LocationMenu link) => new()
        {
            Id = link.Id,
            MenuId = link.MenuId,
            LocationId = link.LocationId,
            IsActive = link.IsActive
        };
    }
}
