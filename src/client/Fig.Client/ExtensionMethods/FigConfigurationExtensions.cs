using Microsoft.Extensions.Configuration;
using System;
using Fig.Client.Configuration;
using Fig.Client.ConfigurationProvider;
using Microsoft.AspNetCore.Hosting;

namespace Fig.Client.ExtensionMethods;

public static class FigConfigurationExtensions
{
    public static IConfigurationBuilder AddFig<T>(this IConfigurationBuilder builder, Action<FigOptions> configure) 
        where T : SettingsBase
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var options = new FigOptions();
        configure(options);

        var remoteBuilder = new FigConfigurationBuilder(builder, options, typeof(T));
        return remoteBuilder;
    }

    public static IWebHostBuilder DisableFig(this IWebHostBuilder builder)
    {
        builder.UseSetting("disable-fig", "true");
        return builder;
    }
}