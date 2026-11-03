using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Modules.Menu.Models;
using RestaurantManagement.Shared.Models;

namespace RestaurantManagement.Modules.Menu.Controllers
{
    /// <summary>"Changes since" reads for devices catching up: menus, menu-location links and menu categories of the header location.</summary>
    [ApiController]
    [Route("api/v1")]
    public class MenuChangesController : ControllerBase
    {
        [HttpGet("menus/changes")]
        [Authorize]
        public Task<ActionResult<ChangesPageResponse<MenuResponse>>> GetMenuChanges([FromQuery] ChangesQuery query)
            => throw new NotImplementedException();

        [HttpGet("menus/location-links/changes")]
        [Authorize]
        public Task<ActionResult<ChangesPageResponse<LocationMenuResponse>>> GetLocationLinkChanges([FromQuery] ChangesQuery query)
            => throw new NotImplementedException();

        [HttpGet("menu-categories/changes")]
        [Authorize]
        public Task<ActionResult<ChangesPageResponse<MenuCategoryResponse>>> GetMenuCategoryChanges([FromQuery] ChangesQuery query)
            => throw new NotImplementedException();
    }
}
