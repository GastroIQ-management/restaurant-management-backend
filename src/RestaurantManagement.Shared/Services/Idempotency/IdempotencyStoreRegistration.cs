using System.Reflection;

namespace RestaurantManagement.Shared.Services.Idempotency
{
    /// <summary>Names the context that stores the keys of the controllers of one module (assembly).</summary>
    public sealed record IdempotencyStoreRegistration(Assembly ControllersAssembly, Type ContextType);
}
