using RestaurantManagement.Modules.Identity.Models.Languages;

namespace RestaurantManagement.Modules.Identity.Services.Languages
{
    public interface IUserLanguageService
    {
        /// <summary>
        /// Stores the language of the signed-in user's emails (ID from the token only). A missing,
        /// inactive or unverified user is refused as an invalid session.
        /// </summary>
        Task UpdateAsync(Guid userId, UpdateLanguageRequest request);
    }
}
