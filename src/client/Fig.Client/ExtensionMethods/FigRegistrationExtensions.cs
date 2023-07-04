using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Client.Configuration;
using Fig.Client.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fig.Client.ExtensionMethods;

public static class FigRegistrationExtensions
{
    public static IServiceCollection AddFig<TService, TImplementation>(
        this IServiceCollection services,
        ILogger logger,
        Action<FigOptions>? options = null,
        Action<TService, ChangedSettingsEventArgs?>? onSettingsChanged = null,
        Action? onRestartRequested = null)
        where TService : class
        where TImplementation : SettingsBase, TService
    {
        new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("fig").Bind(options);

        if (options != null)
            services.Configure(options);

        var figOptions = new FigOptions();
        options?.Invoke(figOptions);

        if (figOptions.ApiUri == null)
            figOptions.ReadUriFromEnvironmentVariable();

        var provider = new FigConfigurationProvider(logger, figOptions);
        var settings = provider.Initialize<TImplementation>().Result;

        figOptions.ReadInstanceFromEnvironmentVariable(settings.ClientName);
        
        if (onSettingsChanged != null)
        {
            settings.SettingsChanged += (s, args) => onSettingsChanged((s as TService)!, args);
            onSettingsChanged(settings, null);
        }

        if (onRestartRequested != null)
            settings.RestartRequested += (_, _) => onRestartRequested();

        services.AddSingleton<TService>(a => settings);

        return services;
    }
}