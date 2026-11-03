using RestaurantManagement.Shared;
using RestaurantManagement.Modules.Organization.Models;

namespace RestaurantManagement.Modules.Organization.Services
{
    public interface ILocationService
    {
        Task<LocationResponse> EditLocationAsync(Guid userId, Guid locationId, EditLocationRequest request);
        Task DeleteLocationAsync(Guid userId, Guid locationId, uint version);
        Task<LocationResponse> CreateLocationAsync(Guid userId, CreateLocationRequest request);
        Task<List<LocationResponse>> GetLocationsAsync(Guid userId);
        Task<RestaurantManagement.Shared.Models.ChangesPageResponse<LocationResponse>> GetLocationChangesAsync(Guid userId, RestaurantManagement.Shared.Models.ChangesQuery query);
    }
}
