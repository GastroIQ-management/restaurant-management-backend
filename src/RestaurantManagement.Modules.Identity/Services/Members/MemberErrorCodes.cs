using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Identity.Services.Members
{
    /// <summary>Error codes of this feature (the comment on each line is the value and status it answers).</summary>
    [ErrorCodeCatalog]
    public static class MemberErrorCodes
    {
        // "member.not_found", 404
        public static ErrorCode NotFound { get; } = new("member.not_found", 404);

        // "member.already_member", 400
        public static ErrorCode AlreadyMember { get; } = new("member.already_member", 400);

        // "member.cannot_change_own_access", 403
        public static ErrorCode CannotChangeOwnAccess { get; } = new("member.cannot_change_own_access", 403);

        // "access.level_above_own", 403
        public static ErrorCode LevelAboveOwn { get; } = new("access.level_above_own", 403);
    }
}
