using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Identity.Services.Members
{
    /// <summary>Error codes of this feature (STUB: every getter throws until its task fills it; the comment on each line is the value and status it must answer).</summary>
    [ErrorCodeCatalog]
    public static class MemberErrorCodes
    {
        // "member.not_found", 404
        public static ErrorCode NotFound => throw new NotImplementedException();

        // "member.already_member", 400
        public static ErrorCode AlreadyMember => throw new NotImplementedException();

        // "member.cannot_change_own_access", 403
        public static ErrorCode CannotChangeOwnAccess => throw new NotImplementedException();

        // "access.level_above_own", 403
        public static ErrorCode LevelAboveOwn => throw new NotImplementedException();
    }
}
