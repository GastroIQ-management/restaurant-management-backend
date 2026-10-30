using RestaurantManagement.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Npgsql;
using RestaurantManagement.Modules.Organization.Data;
using RestaurantManagement.Modules.Organization.Entities;
using RestaurantManagement.Modules.Organization.Models;
using RestaurantManagement.Shared.Auth;
using RestaurantManagement.Shared.Utils.Exceptions;
using RestaurantManagement.Shared.Data.Tenancy;
using RestaurantManagement.Shared.Data.Versioning;
using RestaurantManagement.Shared.Services.AuditLog;

namespace RestaurantManagement.Modules.Organization.Services
{
    public class LocationService : ILocationService
    {
        private readonly OrganizationDbContext _orgDb;
        private readonly IAccessGuard _accessGuard;
        private readonly ITenantContext _tenantContext;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IAuditLog _auditLog;
        private readonly ILogger<LocationService> _logger;

        public LocationService(
            OrganizationDbContext orgDb,
            IAccessGuard accessGuard,
            ITenantContext tenantContext,
            IStringLocalizer<SharedResource> localizer,
            IAuditLog auditLog,
            ILogger<LocationService> logger)
        {
            _orgDb = orgDb;
            _accessGuard = accessGuard;
            _tenantContext = tenantContext;
            _localizer = localizer;
            _auditLog = auditLog;
            _logger = logger;
        }

        public async Task<LocationResponse> CreateLocationAsync(Guid userId, CreateLocationRequest request)
        {
            var organizationId = RequireOrganizationId();

            // 1. Only the Owner creates a location
            await RequireOwnerAsync();

            // 2. Create Location
            var location = new Location
            {
                Id = request.Id,
                OrganizationId = organizationId,
                Name = request.Name,
                Address = request.Address,
                City = request.City,
                State = request.State,
                PostalCode = request.PostalCode,
                Country = request.Country,
                PhoneNumber = request.PhoneNumber,
                Status = LocationStatus.Active,
                TimeZone = request.TimeZone!,
                Type = ParseType(request.Type) ?? LocationType.Store,
                Code = NormalizeCode(request.Code),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // A retry carries the same ID and code; answer the ID clash first so it stays a 409, not a code error.
            // The query filter limits this to the token organization; other organizations' clashes reach the primary key.
            if (await _orgDb.Locations.AnyAsync(l => l.Id == location.Id))
                throw new BusinessException(ErrorCodes.IdAlreadyUsed, _localizer["IdAlreadyUsed"].Value);
            await EnsureCodeIsFreeAsync(location.Code, null);
            _orgDb.Locations.Add(location);
            await SaveAsync();

            return MapToResponse(location);
        }

        public async Task<LocationResponse> EditLocationAsync(Guid userId, Guid locationId, EditLocationRequest request)
        {
            var location = await _orgDb.Locations.FirstOrDefaultAsync(l => l.Id == locationId);
            if (location == null)
                throw new BusinessException(ErrorCodes.LocationNotFound, _localizer["LocationNotFound"].Value);

            await RequireManageLocationAsync(location, request.Status);
            VersionCheck.Require(_orgDb, location, request.Version ?? 0, () => MapToResponse(location));

            var previousStatus = location.Status;
            location.Name = request.Name;
            location.Address = request.Address;
            location.City = request.City;
            location.State = request.State;
            location.PostalCode = request.PostalCode;
            location.Country = request.Country;
            location.PhoneNumber = request.PhoneNumber;
            location.Status = request.Status ?? location.Status;
            location.TimeZone = request.TimeZone!;
            location.Type = ParseType(request.Type) ?? location.Type;
            location.Code = NormalizeCode(request.Code);
            location.UpdatedAt = DateTime.UtcNow;

            await EnsureCodeIsFreeAsync(location.Code, location.Id);
            await SaveAsync(location.Id);
            await RecordStatusChangeAsync(location, previousStatus);

            return MapToResponse(location);
        }

        public async Task DeleteLocationAsync(Guid userId, Guid locationId, uint version)
        {
            var location = await _orgDb.Locations.FirstOrDefaultAsync(l => l.Id == locationId);
            if (location == null)
                throw new BusinessException(ErrorCodes.LocationNotFound, _localizer["LocationNotFound"].Value);

            await RequireOwnerAsync();
            VersionCheck.Require(_orgDb, location, version, () => MapToResponse(location));

            // Delete closes the location instead of removing it, so its data is kept.

            var previousStatus = location.Status;
            location.Status = LocationStatus.Closed;
            location.UpdatedAt = DateTime.UtcNow;

            await SaveAsync(location.Id);
            await RecordStatusChangeAsync(location, previousStatus);
        }

        // The change is already saved; a failed audit write must not undo it or fail the request.
        private async Task RecordStatusChangeAsync(Location location, LocationStatus previousStatus)
        {
            if (location.Status == previousStatus)
                return;

            try
            {
                await _auditLog.RecordAsync(new AuditEntryDto
                {
                    Action = AuditActions.LocationStatusChanged,
                    EntityType = "Location",
                    EntityId = location.Id,
                    LocationId = location.Id,
                    Before = previousStatus.ToString(),
                    After = location.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record the status change of location {LocationId}.", location.Id);
            }
        }

        public async Task<List<LocationResponse>> GetLocationsAsync(Guid userId)
        {
            var organizationId = RequireOrganizationId();

            // Any access at all (the Owner or any cell) may list the locations
            await RequireAnyAccessAsync();

            var locations = await _orgDb.Locations
                .OrderBy(l => l.Name)
                .ToListAsync();

            return locations.Select(MapToResponse).ToList();
        }

        private Task RequireOwnerAsync() => _accessGuard.RequireOwnerAsync();

        private Task RequireAnyAccessAsync() => _accessGuard.RequireAnyAccessAsync();

        // Locations Manage at the location; an edit that sets the status Closed needs the Owner.
        private Task RequireManageLocationAsync(Location location, LocationStatus? requestedStatus)
            => requestedStatus == LocationStatus.Closed
                ? _accessGuard.RequireOwnerAsync()
                : _accessGuard.RequireAtLocationAsync(AccessModule.Locations, AccessLevel.Manage, location.Id);

        private static LocationType? ParseType(string? type)
            => type is null ? null : Enum.Parse<LocationType>(type);

        private static string NormalizeCode(string? code) => code!.Trim().ToUpperInvariant();

        // The tenant query filter limits this check to the token organization.
        private async Task EnsureCodeIsFreeAsync(string code, Guid? ignoredLocationId)
        {
            var taken = await _orgDb.Locations.AnyAsync(l => l.Code == code && l.Id != ignoredLocationId);
            if (taken)
                throw new BusinessException(OrganizationErrorCodes.LocationCodeAlreadyUsed, _localizer["LocationCodeAlreadyUsed"].Value);
        }

        // A concurrent request can pass the check above; the unique index then rejects the second one.
        private async Task SaveAsync(Guid? locationId = null)
        {
            try
            {
                if (locationId is null)
                    await _orgDb.SaveChangesAsync();
                else
                    await VersionCheck.SaveAsync(_orgDb, async () => await CurrentLocationAsync(locationId.Value));
            }
            catch (DbUpdateException ex) when (PrimaryKeyViolation.Matches(ex))
            {
                throw new BusinessException(ErrorCodes.IdAlreadyUsed, _localizer["IdAlreadyUsed"].Value);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                throw new BusinessException(OrganizationErrorCodes.LocationCodeAlreadyUsed, _localizer["LocationCodeAlreadyUsed"].Value);
            }
        }

        // The tracked location may be stale after a lost race, so read the stored one again; gone means 404.
        private async Task<object> CurrentLocationAsync(Guid locationId)
        {
            _orgDb.ChangeTracker.Clear();
            var location = await _orgDb.Locations.FirstOrDefaultAsync(l => l.Id == locationId)
                ?? throw new BusinessException(ErrorCodes.LocationNotFound, _localizer["LocationNotFound"].Value);
            return MapToResponse(location);
        }

        private Guid RequireOrganizationId()
            => _tenantContext.OrganizationId
                ?? throw new BusinessException(ErrorCodes.OrganizationRequired, _localizer["OrganizationRequired"].Value);

        private static LocationResponse MapToResponse(Location location)
        {
            return new LocationResponse
            {
                Version = location.Version,
                Id = location.Id,
                OrganizationId = location.OrganizationId,
                Name = location.Name,
                Address = location.Address,
                City = location.City,
                State = location.State,
                PostalCode = location.PostalCode,
                Country = location.Country,
                PhoneNumber = location.PhoneNumber,
                Status = location.Status,
                TimeZone = location.TimeZone,
                Type = location.Type,
                Code = location.Code,
                CreatedAt = location.CreatedAt,
                UpdatedAt = location.UpdatedAt
            };
        }
    }
}
