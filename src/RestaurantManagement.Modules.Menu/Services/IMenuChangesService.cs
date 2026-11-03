using RestaurantManagement.Modules.Menu.Models;
using RestaurantManagement.Shared.Models;

namespace RestaurantManagement.Modules.Menu.Services
{
    /// <summary>"Changes since" reads of the header location for devices catching up.</summary>
    public interface IMenuChangesService
    {
        Task<ChangesPageResponse<MenuResponse>> GetMenuChangesAsync(ChangesQuery query, CancellationToken cancellationToken);

        Task<ChangesPageResponse<LocationMenuResponse>> GetLocationLinkChangesAsync(ChangesQuery query, CancellationToken cancellationToken);

        Task<ChangesPageResponse<MenuCategoryResponse>> GetMenuCategoryChangesAsync(ChangesQuery query, CancellationToken cancellationToken);
    }
}
