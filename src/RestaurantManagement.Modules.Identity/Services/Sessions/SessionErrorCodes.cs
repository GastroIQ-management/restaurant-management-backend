using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Identity.Services.Sessions
{
    /// <summary>Error codes of this feature (the comment on each line is the value and status it answers).</summary>
    [ErrorCodeCatalog]
    public static class SessionErrorCodes
    {
        // "auth.invalid_credentials", 401
        public static ErrorCode InvalidCredentials { get; } = new("auth.invalid_credentials", 401);

        // "session.invalid_or_expired", 401
        public static ErrorCode InvalidOrExpiredSession { get; } = new("session.invalid_or_expired", 401);

        // "session.organization_required", 400
        public static ErrorCode SelectOrganizationRequired { get; } = new("session.organization_required", 400);

        // "session.not_a_member", 403
        public static ErrorCode NotAMember { get; } = new("session.not_a_member", 403);
    }
}
