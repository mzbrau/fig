using Microsoft.Extensions.Configuration;
using System;
using Fig.Client.Configuration;
using Fig.Client.ConfigurationProvider;
using Fig.Client.IntegrationTest;
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
    
    public static IConfigurationBuilder AddIntegrationTestConfiguration<T>(this IConfigurationBuilder builder, ConfigReloader? configReloader = null, T? initialConfiguration = null)
        where T : SettingsBase
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
        
        var source = new ReloadableConfigurationSource
        {
            ConfigReloader = configReloader ?? new ConfigReloader(),
            SettingsType = typeof(T),
            InitialConfiguration = initialConfiguration
        };
        builder.Add(source);
        return builder;
    }

    public static IWebHostBuilder DisableFig(this IWebHostBuilder builder)
    {
        builder.UseSetting("disable-fig", "true");
        return builder;
    }
}