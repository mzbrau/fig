using Fig.Client.Abstractions.StatusProperties;
using Fig.Client.StatusProperties;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fig.Client.ExtensionMethods
{
    public static class FigStatusPropertiesExtensions
    {
        public static IServiceCollection AddFigStatusProperties<T>(this IServiceCollection services)
            where T : class, new()
        {
            services.TryAddSingleton<FigStatusProperties<T>>();
            services.TryAddSingleton<IFigStatusProperties<T>>(sp => sp.GetRequiredService<FigStatusProperties<T>>());
            services.TryAddSingleton<IFigStatusPropertiesSnapshotProvider>(sp =>
                new FigStatusPropertiesSnapshotProviderAdapter<T>(sp.GetRequiredService<FigStatusProperties<T>>()));
            return services;
        }
    }
}
