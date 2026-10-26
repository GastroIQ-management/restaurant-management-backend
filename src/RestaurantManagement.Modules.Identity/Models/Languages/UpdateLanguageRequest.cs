using System.ComponentModel.DataAnnotations;
using RestaurantManagement.Modules.Identity.Services.Languages;

namespace RestaurantManagement.Modules.Identity.Models.Languages
{
    // The user comes only from the token, so this model never carries a user ID.
    public class UpdateLanguageRequest
    {
        [Required(ErrorMessage = "RequiredAttribute_ValidationError")]
        [AllowedValues(SupportedLanguages.English, SupportedLanguages.Portuguese, SupportedLanguages.French,
            ErrorMessage = "LanguageUnsupported_ValidationError")]
        public string Language { get; set; } = string.Empty;
    }
}
