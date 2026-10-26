using System.Globalization;

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

        // The language of the current request: its culture is set from Accept-Language, and a regional
        // variant such as pt-BR has already been mapped to its base language.
        public static string FromCurrentCulture()
        {
            var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return language is Portuguese or French ? language : Default;
        }
    }
}
