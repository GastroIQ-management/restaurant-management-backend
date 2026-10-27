using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Menu.Services
{
    /// <summary>Error codes of this feature (the comment on each line is the value and status it answers).</summary>
    [ErrorCodeCatalog]
    public static class MenuErrorCodes
    {
        // "menu.not_found", 404
        public static ErrorCode MenuNotFound { get; } = new("menu.not_found", 404);

        // "menu_category.not_found", 404
        public static ErrorCode MenuCategoryNotFound { get; } = new("menu_category.not_found", 404);
    }
}
