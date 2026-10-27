using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Identity.Services.Passwords
{
    /// <summary>Error codes of this feature (STUB: every getter throws until its task fills it; the comment on each line is the value and status it must answer).</summary>
    [ErrorCodeCatalog]
    public static class PasswordErrorCodes
    {
        // "password.current_incorrect", 400
        public static ErrorCode CurrentPasswordIncorrect => throw new NotImplementedException();

        // "password.same_as_current", 400
        public static ErrorCode NewPasswordSameAsCurrent => throw new NotImplementedException();
    }
}
