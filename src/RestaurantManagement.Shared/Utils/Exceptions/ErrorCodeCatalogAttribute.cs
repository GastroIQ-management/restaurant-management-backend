namespace RestaurantManagement.Shared.Utils.Exceptions
{
    /// <summary>Marks a static class whose public static <see cref="ErrorCode"/> properties form one catalog of codes.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ErrorCodeCatalogAttribute : Attribute
    {
    }
}
