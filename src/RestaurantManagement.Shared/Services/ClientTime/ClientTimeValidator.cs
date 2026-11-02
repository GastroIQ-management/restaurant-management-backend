using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Primitives;
using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Shared.Services.ClientTime
{
    /// <summary>Reads the two device-time headers, computes the clock error and checks the bounds.</summary>
    public sealed partial class ClientTimeValidator
    {
        public const int MaxLength = 64;

        public static readonly TimeSpan MaxAhead = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan MaxAge = TimeSpan.FromDays(7);

        private readonly IStringLocalizer<SharedResource> _localizer;

        public ClientTimeValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
        }

        public ClientTimes Validate(StringValues actionTime, StringValues sentAt, DateTimeOffset receivedAt)
        {
            if (StringValues.IsNullOrEmpty(actionTime) || StringValues.IsNullOrEmpty(sentAt))
                throw new BusinessException(ErrorCodes.ClientTimeRequired, _localizer["ClientTimeRequired"].Value);

            var action = Parse(actionTime);
            var sent = Parse(sentAt);
            var received = receivedAt.ToUniversalTime();
            var clockError = sent.UtcDateTime - received.UtcDateTime;
            var corrected = action.UtcDateTime - clockError;

            if (corrected > received.UtcDateTime + MaxAhead)
                throw new BusinessException(ErrorCodes.ClientTimeInFuture, _localizer["ClientTimeInFuture"].Value);
            if (corrected < received.UtcDateTime - MaxAge)
                throw new BusinessException(ErrorCodes.ClientTimeTooOld, _localizer["ClientTimeTooOld"].Value);

            return new ClientTimes(action.UtcDateTime, (int)action.Offset.TotalMinutes, corrected, clockError, received.UtcDateTime);
        }

        private DateTimeOffset Parse(StringValues header)
        {
            if (header.Count == 1 && header[0] is { Length: <= MaxLength } text && Rfc3339WithOffset().IsMatch(text)
                && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var moment))
                return moment;

            throw new BusinessException(ErrorCodes.ClientTimeInvalid, _localizer["ClientTimeInvalid"].Value);
        }

        [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,9})?(Z|[+-]\d{2}:\d{2})$")]
        private static partial Regex Rfc3339WithOffset();
    }
}
