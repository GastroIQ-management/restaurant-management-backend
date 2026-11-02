using Microsoft.Extensions.Options;

namespace RestaurantManagement.Modules.Identity.Services.NativeApps
{
    /// <summary>Refuses to start the app when a native origin is anything but one of the two app origins, naming the setting.</summary>
    public class NativeAppOptionsValidator : IValidateOptions<NativeAppOptions>
    {
        public ValidateOptionsResult Validate(string? name, NativeAppOptions options)
            => throw new NotImplementedException();
    }
}
