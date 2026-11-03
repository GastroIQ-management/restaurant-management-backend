using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace RestaurantManagement.Shared.Services.Changes
{
    public static class ChangesServiceCollectionExtensions
    {
        /// <summary>Registers the change cursor codec, which reads the registered <see cref="TimeProvider"/>.</summary>
        public static IServiceCollection AddChangeCursors(this IServiceCollection services)
        {
            services.AddSingleton(sp => new ChangeCursorCodec(
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IStringLocalizer<SharedResource>>()));
            return services;
        }
    }
}
