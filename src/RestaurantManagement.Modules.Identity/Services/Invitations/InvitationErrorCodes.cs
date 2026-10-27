using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Identity.Services.Invitations
{
    /// <summary>Error codes of this feature (STUB: every getter throws until its task fills it; the comment on each line is the value and status it must answer).</summary>
    [ErrorCodeCatalog]
    public static class InvitationErrorCodes
    {
        // "invitation.not_found", 404
        public static ErrorCode NotFound => throw new NotImplementedException();

        // "invitation.link_invalid", 400
        public static ErrorCode LinkInvalid => throw new NotImplementedException();

        // "invitation.already_open", 409
        public static ErrorCode AlreadyOpen => throw new NotImplementedException();

        // "invitation.for_another_account", 403
        public static ErrorCode ForAnotherAccount => throw new NotImplementedException();

        // "invitation.account_exists", 400
        public static ErrorCode AccountExists => throw new NotImplementedException();
    }
}
