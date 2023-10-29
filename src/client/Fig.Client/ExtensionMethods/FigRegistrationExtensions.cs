using Fig.Client.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fig.Client.ExtensionMethods;

public static class FigRegistrationExtensions
{
    public static IHostBuilder UseFigValidation<T>(this IHostBuilder builder) where T : SettingsBase
    {
        builder.ConfigureServices((_, services) =>
        {
            services.AddHostedService<FigValidationWorker<T>>();
        });

        return builder;
    }

    public static IHostBuilder UseFigRestart<T>(this IHostBuilder builder) where T : SettingsBase
    {
        builder.ConfigureServices((_, services) =>
        {
            services.AddHostedService<FigRestartWorker<T>>();
        });

        return builder;
    }
}