using RestaurantManagement.Shared;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RestaurantManagement.Shared.Utils.Exceptions;
using RestaurantManagement.Modules.Organization.Models;
using RestaurantManagement.Modules.Organization.Services;

namespace RestaurantManagement.Modules.Organization.Controllers
{
    [ApiController]
    [Route("api/v1/organizations")]
    public class OrganizationController : ControllerBase
    {
        private readonly IOrganizationService _organizationService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public OrganizationController(IOrganizationService organizationService, IStringLocalizer<SharedResource> localizer)
        {
            _organizationService = organizationService;
            _localizer = localizer;
        }

        // POST api/v1/organizations/create
        [HttpPost("create")]
        [Authorize]
        public async Task<ActionResult<OrganizationResponse>> Create([FromBody] CreateOrganizationRequest request)
        {
            var userId = GetCurrentUserId();

            var response = await _organizationService.CreateOrganizationAsync(userId, request);
            return Ok(response);
        }

        // PUT api/v1/organizations/edit
        [HttpPut("edit")]
        [Authorize]
        public async Task<ActionResult<OrganizationResponse>> Edit([FromBody] EditOrganizationRequest request)
        {
            // Checked here, not with [Required] on the request, so the request's own field rules stay valid without a version.
            if (request.Version is null)
            {
                ModelState.AddModelError(nameof(request.Version), _localizer["RequiredAttribute_ValidationError", nameof(request.Version)]);
                return ValidationProblem(ModelState);
            }

            var userId = GetCurrentUserId();

            var response = await _organizationService.EditOrganizationAsync(userId, request);
            return Ok(response);
        }

        // GET api/v1/organizations
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<OrganizationResponse>>> GetAll()
        {
            var userId = GetCurrentUserId();

            var response = await _organizationService.GetOrganizationsAsync(userId);
            return Ok(response);
        }

        private Guid GetCurrentUserId()
        {
            var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (Guid.TryParse(subject, out var userId))
            {
                return userId;
            }
            throw new BusinessException(ErrorCodes.Unauthorized, _localizer["UnauthorizedMessage"].Value);
        }
    }
}