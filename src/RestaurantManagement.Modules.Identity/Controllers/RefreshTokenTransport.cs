using Microsoft.AspNetCore.Http;
using RestaurantManagement.Modules.Identity.Models.Sessions;
using RestaurantManagement.Modules.Identity.Services.NativeApps;

namespace RestaurantManagement.Modules.Identity.Controllers
{
    // Picks the refresh token carrier per request: native apps (exact Origin match) use the
    // X-Refresh-Token header and the response body, everything else uses the cookie.
    internal sealed class RefreshTokenTransport
    {
        public const string HeaderName = "X-Refresh-Token";

        private readonly HttpContext _context;
        private readonly bool _isNative;

        public RefreshTokenTransport(HttpContext context, NativeAppOptions options)
        {
            _context = context;
            var origin = context.Request.Headers.Origin.ToString();
            _isNative = origin.Length > 0 && options.Origins.Contains(origin, StringComparer.Ordinal);
        }

        public bool IsNative => _isNative;

        public string? Read()
        {
            if (!_isNative)
                return RefreshTokenCookie.Read(_context.Request);

            var value = _context.Request.Headers[HeaderName].ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public AuthResponse Write(SessionResult result)
        {
            if (!_isNative)
            {
                RefreshTokenCookie.Write(_context.Response, result.RefreshToken, result.RefreshTokenExpiresAt);
                return result.Auth;
            }

            result.Auth.RefreshToken = result.RefreshToken;
            result.Auth.RefreshTokenExpiresAt = result.RefreshTokenExpiresAt;
            return result.Auth;
        }

        public void Clear()
        {
            if (!_isNative)
                RefreshTokenCookie.Clear(_context.Response);
        }
    }
}
