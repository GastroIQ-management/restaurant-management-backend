using System.Reflection;

namespace RestaurantManagement.Shared.Utils.Exceptions
{
    /// <summary>Reads every <see cref="ErrorCode"/> of the classes marked with <see cref="ErrorCodeCatalogAttribute"/>.</summary>
    public static class ErrorCodeCatalogs
    {
        // STUB (T1)
        public static IReadOnlyList<ErrorCode> Read(params Assembly[] assemblies)
            => throw new NotImplementedException();
    }
}
