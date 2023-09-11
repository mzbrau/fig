using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Fig.Client.ClientSecret;
using Fig.Client.Configuration;
using Fig.Client.Events;
using Fig.Client.OfflineSettings;
using Fig.Client.Status;
using Fig.Client.Versions;
using Fig.Common.NetStandard.Constants;
using Fig.Common.NetStandard.Cryptography;
using Fig.Common.NetStandard.Diag;
using Fig.Common.NetStandard.IpAddress;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fig.Client.ExtensionMethods;

public static class FigRegistrationExtensions
{
    public static IServiceCollection AddFig<TService, TImplementation>(
        this IServiceCollection services,
        Action<FigOptions>? options = null,
        Action<TService, ChangedSettingsEventArgs?>? onSettingsChanged = null,
        Action? onRestartRequested = null)
        where TService : class
        where TImplementation : SettingsBase, TService
    {
        var figOptions = CreateFigOptions(services, options);
        RegisterHttpClient(services, figOptions);
        RegisterFigDependencies(services, figOptions);
        
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetService<IFigConfigurationProvider>();
        var settings = provider!.Initialize<TImplementation>().Result;

        figOptions.ReadInstanceFromEnvironmentVariable(settings.ClientName);

        InitializeCallbacks(settings, onSettingsChanged, onRestartRequested);

        services.AddSingleton<TService>(a => settings);

        return services;
    }

    private static void InitializeCallbacks<TService, TImplementation>(
        TImplementation settings, 
        Action<TService,ChangedSettingsEventArgs?>? onSettingsChanged, 
        Action? onRestartRequested) 
        where TService : class
        where TImplementation : SettingsBase, TService
    {
        if (onSettingsChanged != null)
        {
            settings.SettingsChanged += (s, args) => onSettingsChanged((s as TService)!, args);
            onSettingsChanged(settings, null);
        }

        if (onRestartRequested != null)
            settings.RestartRequested += (_, _) => onRestartRequested();
    }

    private static void RegisterFigDependencies(IServiceCollection services, IFigOptions figOptions)
    {
        services.AddSingleton<IFigOptions>(_ => figOptions);
        services.AddSingleton<IVersionProvider, VersionProvider>();
        services.AddSingleton<IDiagnostics, Diagnostics>();
        services.AddSingleton<ISettingStatusMonitor, SettingStatusMonitor>();
        services.AddSingleton<IIpAddressResolver, IpAddressResolver>();
        services.AddSingleton<IClientSecretProvider, ClientSecretProvider>();
        services.AddSingleton<ICryptography, Cryptography>();
        services.AddSingleton<IBinaryFile, BinaryFile>();
        services.AddSingleton<IOfflineSettingsManager, OfflineSettingsManager>();
        services.AddSingleton<IFigConfigurationProvider, FigConfigurationProvider>();
    }

    private static void RegisterHttpClient(IServiceCollection services, IFigOptions figOptions)
    {
        services.AddHttpClient(HttpClientNames.FigApi, c =>
        {
            c.BaseAddress = figOptions.ApiUri;
            c.DefaultRequestHeaders.Add("Accept", "application/json");
        }); //.ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler()
            //{ AutomaticDecompression = DecompressionMethods.GZip });
    }

    private static FigOptions CreateFigOptions(IServiceCollection services, Action<FigOptions>? options = null)
    {
        var figOptions = new FigOptions();
        new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build()
            .GetSection("fig")
            .Bind(figOptions);

        options?.Invoke(figOptions);

        if (options != null)
            services.Configure(options);

        if (figOptions.ApiUri == null)
            figOptions.ReadUriFromEnvironmentVariable();

        return figOptions;
    }
}