using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Organization.Services
{
    /// <summary>Error codes of this feature (the comment on each line is the value and status it answers).</summary>
    [ErrorCodeCatalog]
    public static class OrganizationErrorCodes
    {
        // "organization.trial_limit_reached", 400
        public static ErrorCode TrialLimitReached { get; } = new("organization.trial_limit_reached", 400);

        // "organization.settings_not_found", 404
        public static ErrorCode SettingsNotFound { get; } = new("organization.settings_not_found", 404);

        // "location.code_already_used", 400
        public static ErrorCode LocationCodeAlreadyUsed { get; } = new("location.code_already_used", 400);
    }
}
