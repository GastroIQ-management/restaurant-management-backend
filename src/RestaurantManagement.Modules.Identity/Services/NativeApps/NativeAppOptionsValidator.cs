using Microsoft.Extensions.Options;

namespace RestaurantManagement.Modules.Identity.Services.NativeApps
{
    /// <summary>Refuses to start the app when a native origin is anything but one of the two app origins, naming the setting.</summary>
    public class NativeAppOptionsValidator : IValidateOptions<NativeAppOptions>
    {
        private static readonly string[] AllowedOrigins = { "https://localhost", "capacitor://localhost" };

        public ValidateOptionsResult Validate(string? name, NativeAppOptions options)
        {
            var invalid = options.Origins.Any(o => !AllowedOrigins.Contains(o, StringComparer.Ordinal));
            return invalid
                ? ValidateOptionsResult.Fail($"NativeApp:Origins may only contain {string.Join(" or ", AllowedOrigins)} (exact value).")
                : ValidateOptionsResult.Success;
        }
    }
}
