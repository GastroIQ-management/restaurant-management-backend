using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Modules.Menu.Models;
using RestaurantManagement.Modules.Menu.Services;
using RestaurantManagement.Shared.Models;

namespace RestaurantManagement.Modules.Menu.Controllers
{
    /// <summary>"Changes since" reads for devices catching up: menus, menu-location links and menu categories of the header location.</summary>
    [ApiController]
    [Route("api/v1")]
    public class MenuChangesController : ControllerBase
    {
        private readonly IMenuChangesService _changes;

        public MenuChangesController(IMenuChangesService changes) => _changes = changes;

        [HttpGet("menus/changes")]
        [Authorize]
        public async Task<ActionResult<ChangesPageResponse<MenuResponse>>> GetMenuChanges([FromQuery] ChangesQuery query, CancellationToken cancellationToken)
            => Ok(await _changes.GetMenuChangesAsync(query, cancellationToken));

        [HttpGet("menus/location-links/changes")]
        [Authorize]
        public async Task<ActionResult<ChangesPageResponse<LocationMenuResponse>>> GetLocationLinkChanges([FromQuery] ChangesQuery query, CancellationToken cancellationToken)
            => Ok(await _changes.GetLocationLinkChangesAsync(query, cancellationToken));

        [HttpGet("menu-categories/changes")]
        [Authorize]
        public async Task<ActionResult<ChangesPageResponse<MenuCategoryResponse>>> GetMenuCategoryChanges([FromQuery] ChangesQuery query, CancellationToken cancellationToken)
            => Ok(await _changes.GetMenuCategoryChangesAsync(query, cancellationToken));
    }
}
