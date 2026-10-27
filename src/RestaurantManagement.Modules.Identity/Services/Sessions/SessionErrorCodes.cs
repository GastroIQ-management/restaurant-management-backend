using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Identity.Services.Sessions
{
    /// <summary>Error codes of this feature (STUB: every getter throws until its task fills it; the comment on each line is the value and status it must answer).</summary>
    [ErrorCodeCatalog]
    public static class SessionErrorCodes
    {
        // "auth.invalid_credentials", 401
        public static ErrorCode InvalidCredentials => throw new NotImplementedException();

        // "session.invalid_or_expired", 401
        public static ErrorCode InvalidOrExpiredSession => throw new NotImplementedException();

        // "session.organization_required", 400
        public static ErrorCode SelectOrganizationRequired => throw new NotImplementedException();

        // "session.not_a_member", 403
        public static ErrorCode NotAMember => throw new NotImplementedException();
    }
}
