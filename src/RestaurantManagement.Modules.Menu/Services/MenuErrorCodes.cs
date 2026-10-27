using RestaurantManagement.Shared.Utils.Exceptions;

namespace RestaurantManagement.Modules.Menu.Services
{
    /// <summary>Error codes of this feature (STUB: every getter throws until its task fills it; the comment on each line is the value and status it must answer).</summary>
    [ErrorCodeCatalog]
    public static class MenuErrorCodes
    {
        // "menu.not_found", 404
        public static ErrorCode MenuNotFound => throw new NotImplementedException();

        // "menu_category.not_found", 404
        public static ErrorCode MenuCategoryNotFound => throw new NotImplementedException();
    }
}
