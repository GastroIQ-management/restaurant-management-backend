using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace RestaurantManagement.Shared.Services.Idempotency
{
    public static class IdempotencyServiceCollectionExtensions
    {
        /// <summary>Registers <typeparamref name="TContext"/> as the key store for the controllers in <paramref name="controllersAssembly"/>.</summary>
        public static IServiceCollection AddIdempotencyKeyStore<TContext>(this IServiceCollection services, Assembly controllersAssembly)
            where TContext : DbContext
            => throw new NotImplementedException();
    }
}
