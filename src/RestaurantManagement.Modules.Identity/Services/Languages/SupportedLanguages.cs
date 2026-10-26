namespace RestaurantManagement.Modules.Identity.Services.Languages
{
    // The languages that have an email text and a message catalog (see RegionalCultureProvider in Shared).
    public static class SupportedLanguages
    {
        public const string English = "en";
        public const string Portuguese = "pt";
        public const string French = "fr";

        // Used when no language is known: a background job has no request to read one from.
        public const string Default = English;

        // Maps an Accept-Language header to a supported language; regional variants map to their base language.
        public static string FromAcceptLanguage(string? header)
        {
            var value = header?.TrimStart() ?? string.Empty;
            if (value.StartsWith(Portuguese, StringComparison.OrdinalIgnoreCase))
                return Portuguese;
            if (value.StartsWith(French, StringComparison.OrdinalIgnoreCase))
                return French;
            return Default;
        }
    }
}
