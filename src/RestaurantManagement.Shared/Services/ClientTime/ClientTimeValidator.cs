using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Primitives;

namespace RestaurantManagement.Shared.Services.ClientTime
{
    /// <summary>Reads the two device-time headers, computes the clock error and checks the bounds.</summary>
    public sealed class ClientTimeValidator
    {
        public ClientTimeValidator(IStringLocalizer<SharedResource> localizer)
        {
        }

        public ClientTimes Validate(StringValues actionTime, StringValues sentAt, DateTimeOffset receivedAt)
            => throw new NotImplementedException();
    }
}
