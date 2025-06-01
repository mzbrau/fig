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
            services.AddHostedService<FigCustomActionWorker<T>>();
        });

        return builder;
    }
}