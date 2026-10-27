using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Identity.Services.Invitations
{
    /// <summary>Error codes of this feature (the comment on each line is the value and status it answers).</summary>
    [ErrorCodeCatalog]
    public static class InvitationErrorCodes
    {
        // "invitation.not_found", 404
        public static ErrorCode NotFound { get; } = new("invitation.not_found", 404);

        // "invitation.link_invalid", 400
        public static ErrorCode LinkInvalid { get; } = new("invitation.link_invalid", 400);

        // "invitation.already_open", 409
        public static ErrorCode AlreadyOpen { get; } = new("invitation.already_open", 409);

        // "invitation.for_another_account", 403
        public static ErrorCode ForAnotherAccount { get; } = new("invitation.for_another_account", 403);

        // "invitation.account_exists", 400
        public static ErrorCode AccountExists { get; } = new("invitation.account_exists", 400);
    }
}
