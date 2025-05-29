using System;
using Fig.Client.CustomActions;
using Fig.Client.Health;
using Fig.Client.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Fig.Client.ExtensionMethods;

public static class FigRegistrationExtensions
{
    public static IHostBuilder UseFig<T>(this IHostBuilder builder) where T : SettingsBase
    {
        builder.ConfigureServices((_, services) =>
        {
            services.AddHealthChecks()
                .AddCheck<FigConfigurationHealthCheck<T>>("Fig configuration");
            services.AddHostedService<FigRestartWorker<T>>();
            services.AddHostedService<FigHealthWorker<T>>();
            
            // Ensure ApiCommunicationHandler is registered correctly with its new dependencies.
            // It's the implementation for IApiCommunicationHandler.
            services.TryAddSingleton<Fig.Client.ConfigurationProvider.IApiCommunicationHandler, Fig.Client.ConfigurationProvider.ApiCommunicationHandler>();
            
            services.TryAddSingleton<CustomActionExecutionWorker>();
            services.AddHostedService(sp => sp.GetRequiredService<CustomActionExecutionWorker>());
        });

        return builder;
    }

    public static IServiceCollection AddCustomAction<TAction, TSettings>(this IServiceCollection services)
        where TAction : class, ICustomAction
        where TSettings : class, SettingsBase
    {
        services.AddTransient<TAction>();
        services.AddHostedService<CustomActionRegistrar<TSettings, TAction>>();
        return services;
    }

    public static IServiceCollection AddCustomAction<TAction>(this IServiceCollection services)
        where TAction : class, ICustomAction
    {
        services.AddTransient<TAction>();
        services.AddHostedService<CustomActionRegistrar<TAction>>();
        return services;
    }
}