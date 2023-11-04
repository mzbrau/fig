#nullable enable
using Fig.Client.Configuration;
using System;
using Fig.Client.Exceptions;
using Fig.Client.ExtensionMethods;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Fig.Client.NetFramework;

/// <summary>
/// This class is for use in .net framework applications. Dotnet core applications should use the configuration provider.
/// </summary>
/// <typeparam name="T">The settings class to be used.</typeparam>
public static class FigConfigurationManager<T> where T : SettingsBase
{
    private static bool IsInitialized => _options != null;
    private static IOptionsMonitor<T>? _options;
    
    public static IOptionsMonitor<T>? Settings
    {
        get
        {
            if (!IsInitialized)
                throw new NotInitializedException();

            return _options;
        }
    }

    public static void Initialize(FigOptions figOptions, ILogger logger)
    {
        if (figOptions is null) 
            throw new ArgumentNullException(nameof(figOptions));

        var configuration = new ConfigurationBuilder()
            .AddFig<T>(o =>
            {
                o.ClientName = figOptions.ClientName;
                o.LiveReload = figOptions.LiveReload;
                o.SupportsRestart = figOptions.SupportsRestart;
                o.AllowOfflineSettings = figOptions.AllowOfflineSettings;
                o.LoggerFactory = figOptions.LoggerFactory;
                o.VersionOverride = figOptions.VersionOverride;
            }).Build();

        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.Configure<T>(configuration).BuildServiceProvider();
        _options = serviceProvider.GetRequiredService<IOptionsMonitor<T>>();

        _options!.OnChange((settings, _) =>
        {
            settings.Validate(logger);
        });
    }
}