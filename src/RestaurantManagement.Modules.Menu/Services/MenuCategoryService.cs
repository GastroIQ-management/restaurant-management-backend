using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using RestaurantManagement.Modules.Menu.Data;
using RestaurantManagement.Modules.Menu.Entities;
using RestaurantManagement.Modules.Menu.Models;
using RestaurantManagement.Shared;
using RestaurantManagement.Shared.Data.Tenancy;
using RestaurantManagement.Shared.Data.Versioning;
using RestaurantManagement.Shared.Auth;
using RestaurantManagement.Shared.Services.Organization;
using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Menu.Services
{
    public class MenuCategoryService : IMenuCategoryService
    {
        private readonly MenuDbContext _context;
        private readonly IOrganizationLookup _organizationLookup;
        private readonly MenuAccessChecker _access;
        private readonly ITenantContext _tenantContext;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public MenuCategoryService(
            MenuDbContext context,
            IOrganizationLookup organizationLookup,
            IAccessGuard accessGuard,
            ITenantContext tenantContext,
            IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _organizationLookup = organizationLookup;
            _access = new MenuAccessChecker(accessGuard);
            _tenantContext = tenantContext;
            _localizer = localizer;
        }

        public async Task<MenuCategoryResponse> CreateAsync(Guid userId, CreateMenuCategoryRequest request)
        {
            var organizationId = GetTokenOrganizationId();

            // The tenant query filter hides other organizations' menus, so a foreign MenuId looks missing.
            var menuExists = await _context.Menus.AnyAsync(m => m.Id == request.MenuId);
            if (!menuExists)
                throw new BusinessException(MenuErrorCodes.MenuNotFound, _localizer["MenuNotFound"].Value);

            if (request.LocationId.HasValue)
            {
                var location = await _organizationLookup.GetLocationAsync(request.LocationId.Value);
                if (location == null || location.OrganizationId != organizationId)
                    throw new BusinessException(ErrorCodes.LocationNotFound, _localizer["LocationNotFound"].Value);
            }

            await _access.RequireManageCategoryAsync(request.LocationId);

            var category = new MenuCategory
            {
                Id = request.Id,
                MenuId = request.MenuId,
                OrganizationId = organizationId,
                LocationId = request.LocationId,
                Name = request.Name,
                Description = request.Description,
                SortOrder = request.SortOrder,
                CreatedAt = DateTime.UtcNow
            };

            _context.MenuCategories.Add(category);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (PrimaryKeyViolation.Matches(ex))
            {
                throw new BusinessException(ErrorCodes.IdAlreadyUsed, _localizer["IdAlreadyUsed"].Value);
            }

            return MapToResponse(category);
        }

        public async Task<MenuCategoryResponse?> GetByIdAsync(Guid userId, Guid id)
        {
            var category = await _context.MenuCategories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return null;

            await _access.RequireReadCategoryAsync(category.LocationId);

            return MapToResponse(category);
        }

        public async Task<IEnumerable<MenuCategoryResponse>> GetByMenuIdAsync(Guid userId, Guid menuId)
        {
            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == menuId);
            if (menu == null)
                return Enumerable.Empty<MenuCategoryResponse>();

            await _access.RequireReadEverywhereAsync();

            var categories = await _context.MenuCategories
                .Where(c => c.MenuId == menuId)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return categories.Select(MapToResponse);
        }

        public async Task<IEnumerable<MenuCategoryResponse>> GetAllAsync(Guid userId)
        {
            var organizationId = GetTokenOrganizationId();
            await _access.RequireReadEverywhereAsync();

            var categories = await _context.MenuCategories
                .Where(c => c.OrganizationId == organizationId)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return categories.Select(MapToResponse);
        }

        public async Task<IEnumerable<MenuCategoryResponse>> GetByLocationIdAsync(Guid userId, Guid locationId)
        {
            var location = await _organizationLookup.GetLocationAsync(locationId);
            if (location == null)
                return Enumerable.Empty<MenuCategoryResponse>();

            await _access.RequireReadAtLocationAsync(locationId);

            var categories = await _context.MenuCategories
                .Where(c => c.LocationId == locationId)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return categories.Select(MapToResponse);
        }

        public async Task<MenuCategoryResponse?> UpdateAsync(Guid userId, Guid id, UpdateMenuCategoryRequest request)
        {
            var category = await _context.MenuCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
                return null;

            await _access.RequireManageCategoryAsync(category.LocationId);
            VersionCheck.Require(_context, category, request.Version ?? 0, () => MapToResponse(category));

            category.Name = request.Name;
            category.Description = request.Description;
            category.SortOrder = request.SortOrder;

            await VersionCheck.SaveAsync(_context, async () => await CurrentCategoryAsync(id));

            return MapToResponse(category);
        }

        public async Task<bool> DeleteAsync(Guid userId, Guid id, uint version)
        {
            var category = await _context.MenuCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
                return false;

            await _access.RequireManageCategoryAsync(category.LocationId);

            VersionCheck.Require(_context, category, version, () => MapToResponse(category));

            _context.MenuCategories.Remove(category);
            await VersionCheck.SaveAsync(_context, async () => await CurrentCategoryAsync(id));

            return true;
        }

        private Guid GetTokenOrganizationId()
        {
            return _tenantContext.OrganizationId
                ?? throw new BusinessException(ErrorCodes.OrganizationRequired, _localizer["OrganizationRequired"].Value);
        }

        // The tracked category may be stale after a lost race, so read the stored one again; gone means 404.
        private async Task<object> CurrentCategoryAsync(Guid id)
        {
            _context.ChangeTracker.Clear();
            var stored = await _context.MenuCategories.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new BusinessException(MenuErrorCodes.MenuCategoryNotFound, _localizer["MenuCategoryNotFound"].Value);
            return MapToResponse(stored);
        }

        private static MenuCategoryResponse MapToResponse(MenuCategory category)
        {
            return new MenuCategoryResponse
            {
                Version = category.Version,
                Id = category.Id,
                MenuId = category.MenuId,
                OrganizationId = category.OrganizationId,
                LocationId = category.LocationId,
                Name = category.Name,
                Description = category.Description,
                SortOrder = category.SortOrder,
                CreatedAt = category.CreatedAt
            };
        }
    }
}
