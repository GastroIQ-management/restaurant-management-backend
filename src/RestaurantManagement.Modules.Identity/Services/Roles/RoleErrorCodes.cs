using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Identity.Services.Roles
{
    /// <summary>Error codes of this feature (STUB: every getter throws until its task fills it; the comment on each line is the value and status it must answer).</summary>
    [ErrorCodeCatalog]
    public static class RoleErrorCodes
    {
        // "role.not_found", 404
        public static ErrorCode NotFound => throw new NotImplementedException();

        // "role.name_invalid", 400
        public static ErrorCode NameInvalid => throw new NotImplementedException();

        // "role.name_already_used", 400
        public static ErrorCode NameAlreadyUsed => throw new NotImplementedException();
    }
}
