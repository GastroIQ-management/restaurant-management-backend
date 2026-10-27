using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Identity.Services.Roles
{
    /// <summary>Error codes of this feature (the comment on each line is the value and status it answers).</summary>
    [ErrorCodeCatalog]
    public static class RoleErrorCodes
    {
        // "role.not_found", 404
        public static ErrorCode NotFound { get; } = new("role.not_found", 404);

        // "role.name_invalid", 400
        public static ErrorCode NameInvalid { get; } = new("role.name_invalid", 400);

        // "role.name_already_used", 400
        public static ErrorCode NameAlreadyUsed { get; } = new("role.name_already_used", 400);
    }
}
