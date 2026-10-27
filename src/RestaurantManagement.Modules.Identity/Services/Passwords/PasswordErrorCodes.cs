using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Identity.Services.Passwords
{
    /// <summary>Error codes of this feature (the comment on each line is the value and status it answers).</summary>
    [ErrorCodeCatalog]
    public static class PasswordErrorCodes
    {
        // "password.current_incorrect", 400
        public static ErrorCode CurrentPasswordIncorrect { get; } = new("password.current_incorrect", 400);

        // "password.same_as_current", 400
        public static ErrorCode NewPasswordSameAsCurrent { get; } = new("password.same_as_current", 400);
    }
}
