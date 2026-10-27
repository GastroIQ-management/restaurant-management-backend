using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Identity.Services.Registration
{
    /// <summary>Error codes of this feature (STUB: every getter throws until its task fills it; the comment on each line is the value and status it must answer).</summary>
    [ErrorCodeCatalog]
    public static class RegistrationErrorCodes
    {
        // "auth.invalid_code", 401
        public static ErrorCode InvalidCode => throw new NotImplementedException();

        // "registration.email_already_registered", 400
        public static ErrorCode EmailAlreadyRegistered => throw new NotImplementedException();
    }
}
