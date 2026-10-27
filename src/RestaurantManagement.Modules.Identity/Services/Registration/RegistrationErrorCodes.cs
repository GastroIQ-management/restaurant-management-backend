using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Identity.Services.Registration
{
    /// <summary>Error codes of this feature (the comment on each line is the value and status it answers).</summary>
    [ErrorCodeCatalog]
    public static class RegistrationErrorCodes
    {
        // "auth.invalid_code", 401
        public static ErrorCode InvalidCode { get; } = new("auth.invalid_code", 401);

        // "registration.email_already_registered", 400
        public static ErrorCode EmailAlreadyRegistered { get; } = new("registration.email_already_registered", 400);
    }
}
