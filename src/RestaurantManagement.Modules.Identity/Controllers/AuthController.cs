using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.RateLimiting;
using RestaurantManagement.Modules.Identity.Models.Common;
using RestaurantManagement.Modules.Identity.Models.Invitations;
using RestaurantManagement.Modules.Identity.Models.Passwords;
using RestaurantManagement.Modules.Identity.Models.Registration;
using RestaurantManagement.Modules.Identity.Models.Sessions;
using RestaurantManagement.Modules.Identity.Services.Invitations;
using RestaurantManagement.Modules.Identity.Services.NativeApps;
using RestaurantManagement.Modules.Identity.Services.Passwords;
using RestaurantManagement.Modules.Identity.Services.Registration;
using RestaurantManagement.Modules.Identity.Services.Sessions;
using RestaurantManagement.Shared.RateLimiting;
using RestaurantManagement.Shared.Services.Idempotency;

namespace RestaurantManagement.Modules.Identity.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IRegistrationService _registrationService;
        private readonly ISessionService _sessionService;
        private readonly IVerificationService _verificationService;
        private readonly IInvitationService _invitationService;

        public AuthController(
            IRegistrationService registrationService,
            ISessionService sessionService,
            IVerificationService verificationService,
            IInvitationService invitationService)
        {
            _registrationService = registrationService;
            _sessionService = sessionService;
            _verificationService = verificationService;
            _invitationService = invitationService;
        }

        // POST api/v1/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting(AuthRateLimitPolicies.Register)]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var response = await _registrationService.RegisterAsync(request);
            return Ok(response);
        }

        // POST api/v1/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(AuthRateLimitPolicies.Login)]
        public async Task<ActionResult<AuthResponse>> Login(
            [FromBody] LoginRequest request, [FromServices] IOptions<NativeAppOptions> native)
        {
            var transport = new RefreshTokenTransport(HttpContext, native.Value);
            var previousToken = transport.IsNative ? null : transport.Read();
            var result = await _sessionService.LoginAsync(request, RefreshTokenCookie.ClientOf(HttpContext), previousToken);
            return Ok(transport.Write(result));
        }

        // POST api/v1/auth/refresh
        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting(AuthRateLimitPolicies.Refresh)]
        public async Task<ActionResult<AuthResponse>> Refresh([FromServices] IOptions<NativeAppOptions> native)
        {
            var transport = new RefreshTokenTransport(HttpContext, native.Value);
            var result = await _sessionService.RefreshTokenAsync(transport.Read());
            return Ok(transport.Write(result));
        }

        // POST api/v1/auth/accept-invitation
        [HttpPost("accept-invitation")]
        [AllowAnonymous]
        [EnableRateLimiting(AuthRateLimitPolicies.Verify)]
        public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationWithAccountRequest request)
        {
            await _invitationService.AcceptWithNewAccountAsync(request);
            return NoContent();
        }

        // POST api/v1/auth/select-organization
        [HttpPost("select-organization")]
        [Authorize]
        [IgnoreIdempotencyKey]
        public async Task<ActionResult<AuthResponse>> SelectOrganization(
            [FromBody] SelectOrganizationRequest request, [FromServices] IOptions<NativeAppOptions> native)
        {
            var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(subject, out var userId))
                return Unauthorized();

            var transport = new RefreshTokenTransport(HttpContext, native.Value);
            var result = await _sessionService.SelectOrganizationAsync(
                userId, request, transport.Read(), RefreshTokenCookie.ClientOf(HttpContext));
            return Ok(transport.Write(result));
        }

        // POST api/v1/auth/logout
        [HttpPost("logout")]
        [AllowAnonymous]
        [EnableRateLimiting(AuthRateLimitPolicies.Refresh)]
        public async Task<IActionResult> Logout([FromServices] IOptions<NativeAppOptions> native)
        {
            var transport = new RefreshTokenTransport(HttpContext, native.Value);
            await _sessionService.LogoutAsync(transport.Read());
            transport.Clear();
            return NoContent();
        }

        // POST api/v1/auth/logout-all
        [HttpPost("logout-all")]
        [Authorize]
        [IgnoreIdempotencyKey]
        public async Task<IActionResult> LogoutAll([FromServices] IOptions<NativeAppOptions> native)
        {
            var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(subject, out var userId))
                return Unauthorized();

            await _sessionService.LogoutAllAsync(userId);
            new RefreshTokenTransport(HttpContext, native.Value).Clear();
            return NoContent();
        }

        // POST api/v1/auth/verify
        [HttpPost("verify")]
        [AllowAnonymous]
        [EnableRateLimiting(AuthRateLimitPolicies.Verify)]
        public async Task<ActionResult<MessageResponse>> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            var response = await _verificationService.VerifyEmailAsync(request);
            return Ok(response);
        }

        // POST api/v1/auth/resend-verification
        [HttpPost("resend-verification")]
        [AllowAnonymous]
        [EnableRateLimiting(AuthRateLimitPolicies.ResendVerification)]
        public async Task<ActionResult<MessageResponse>> ResendVerification([FromBody] ResendVerificationRequest request)
        {
            var response = await _verificationService.ResendVerificationCodeAsync(request);
            return Ok(response);
        }

        // POST api/v1/auth/forgot-password
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting(AuthRateLimitPolicies.ForgotPassword)]
        public async Task<ActionResult<MessageResponse>> ForgotPassword(
            [FromBody] ForgotPasswordRequest request, [FromServices] IPasswordResetService resetService)
        {
            return Ok(await resetService.RequestResetCodeAsync(request));
        }

        // POST api/v1/auth/reset-password
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting(AuthRateLimitPolicies.ResetPassword)]
        public async Task<ActionResult<MessageResponse>> ResetPassword(
            [FromBody] ResetPasswordRequest request, [FromServices] IPasswordResetService resetService)
        {
            return Ok(await resetService.ResetPasswordAsync(request));
        }

        // POST api/v1/auth/change-password
        [HttpPost("change-password")]
        [Authorize]
        [IgnoreIdempotencyKey]
        [EnableRateLimiting(AuthRateLimitPolicies.ChangePassword)]
        public async Task<ActionResult<MessageResponse>> ChangePassword(
            [FromBody] ChangePasswordRequest request,
            [FromServices] IPasswordChangeService changeService,
            [FromServices] IOptions<NativeAppOptions> native)
        {
            var subject = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(subject, out var userId))
                return Unauthorized();

            var response = await changeService.ChangePasswordAsync(userId, request);
            new RefreshTokenTransport(HttpContext, native.Value).Clear();
            return Ok(response);
        }
    }
}
