using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Modules.Identity.Models.Languages;
using RestaurantManagement.Modules.Identity.Services.Languages;

namespace RestaurantManagement.Modules.Identity.Controllers
{
    [ApiController]
    [Route("api/users/me")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserLanguageService _languageService;

        public UsersController(IUserLanguageService languageService)
        {
            _languageService = languageService;
        }

        // PUT api/users/me/language
        [HttpPut("language")]
        public async Task<IActionResult> UpdateLanguage([FromBody] UpdateLanguageRequest request)
        {
            var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(subject, out var userId))
                return Unauthorized();

            await _languageService.UpdateAsync(userId, request);
            return NoContent();
        }
    }
}
