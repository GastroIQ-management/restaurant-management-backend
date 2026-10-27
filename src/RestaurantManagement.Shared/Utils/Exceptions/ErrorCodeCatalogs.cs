using System.Reflection;

namespace RestaurantManagement.Shared.Utils.Exceptions
{
    /// <summary>Reads every <see cref="ErrorCode"/> of the classes marked with <see cref="ErrorCodeCatalogAttribute"/>.</summary>
    public static class ErrorCodeCatalogs
    {
        public static IReadOnlyList<ErrorCode> Read(params Assembly[] assemblies)
            => assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsDefined(typeof(ErrorCodeCatalogAttribute), inherit: false))
                .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Static))
                .Where(p => p.PropertyType == typeof(ErrorCode))
                .Select(p => (ErrorCode)p.GetValue(null)!)
                .ToList();
    }
}
