using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Organization.Services
{
    /// <summary>Error codes of this feature (STUB: every getter throws until its task fills it; the comment on each line is the value and status it must answer).</summary>
    [ErrorCodeCatalog]
    public static class OrganizationErrorCodes
    {
        // "organization.trial_limit_reached", 400
        public static ErrorCode TrialLimitReached => throw new NotImplementedException();

        // "organization.settings_not_found", 404
        public static ErrorCode SettingsNotFound => throw new NotImplementedException();

        // "location.code_already_used", 400
        public static ErrorCode LocationCodeAlreadyUsed => throw new NotImplementedException();
    }
}
