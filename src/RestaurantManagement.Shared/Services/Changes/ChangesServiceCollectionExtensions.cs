using Microsoft.Extensions.DependencyInjection;
using RestaurantManagement.Shared.Data.Changes;
using Microsoft.Extensions.Localization;

namespace RestaurantManagement.Shared.Services.Changes
{
    public static class ChangesServiceCollectionExtensions
    {
        /// <summary>Registers the change cursor codec and reader, which read the registered <see cref="TimeProvider"/>.</summary>
        public static IServiceCollection AddChangeCursors(this IServiceCollection services)
        {
            services.AddSingleton(sp => new ChangeCursorCodec(
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IStringLocalizer<SharedResource>>()));
            services.AddSingleton<ChangeReader>();
            return services;
        }
    }
}
