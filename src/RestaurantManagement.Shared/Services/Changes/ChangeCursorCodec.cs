using System.Buffers.Text;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Shared.Services.Changes
{
    /// <summary>
    /// Encodes and decodes the opaque change cursor and refuses one that is invalid, bound elsewhere or expired.
    /// The cursor is unsigned on purpose: every read filters by token organization, header location and access anyway.
    /// </summary>
    public sealed class ChangeCursorCodec
    {
        /// <summary>How long a cursor (and a tombstone) stays usable.</summary>
        public static readonly TimeSpan Retention = TimeSpan.FromDays(14);

        private readonly TimeProvider _timeProvider;
        private readonly IStringLocalizer<SharedResource>? _localizer;

        public ChangeCursorCodec(TimeProvider timeProvider, IStringLocalizer<SharedResource>? localizer = null)
        {
            _timeProvider = timeProvider;
            _localizer = localizer;
        }

        public string Encode(ChangeCursor cursor)
            => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cursor)));

        public ChangeCursor Decode(string text, string feed, Guid organizationId, Guid? locationId)
        {
            var cursor = Read(text);
            if (cursor.Feed != feed || cursor.OrganizationId != organizationId || cursor.LocationId != locationId)
                throw Invalid();

            // Checked after the binding so a cursor of another tenant never learns anything from its age.
            if (_timeProvider.GetUtcNow() - cursor.IssuedAt > Retention)
                throw new BusinessException(ErrorCodes.ChangeCursorExpired, Message("ChangeCursorExpired"));

            return cursor;
        }

        private ChangeCursor Read(string text)
        {
            try
            {
                var cursor = JsonSerializer.Deserialize<ChangeCursor>(Base64Url.DecodeFromChars(text));
                if (cursor is null || string.IsNullOrEmpty(cursor.Feed) || cursor.OrganizationId == Guid.Empty || cursor.IssuedAt == default)
                    throw Invalid();
                return cursor;
            }
            catch (Exception ex) when (ex is JsonException or FormatException or ArgumentException)
            {
                throw Invalid();
            }
        }

        private BusinessException Invalid() => new(ErrorCodes.ChangeCursorInvalid, Message("ChangeCursorInvalid"));

        private string Message(string key) => _localizer?[key].Value ?? key;
    }
}
