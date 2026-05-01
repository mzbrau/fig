#nullable enable
using Fig.Client.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Exceptions;
using Fig.Client.ExtensionMethods;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Fig.Client.Health;
using Fig.Contracts.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fig.Client.NetFramework;

/// <summary>
/// This class is for use in .net framework applications. Dotnet core applications should use the configuration provider.
/// </summary>
/// <typeparam name="T">The settings class to be used.</typeparam>
public static class FigConfigurationManager<T> where T : SettingsBase
{
    private static bool IsInitialized => _options != null;
    private static readonly object Sync = new();
    private static IOptionsMonitor<T>? _options;
    private static FigConfigurationHealthCheck<T>? _configurationHealthCheck;
    private static Func<Task<HealthDataContract>>? _healthReportProvider;

    public static IOptionsMonitor<T>? Settings
    {
        get
        {
            lock (Sync)
            {
                var options = _options;

                if (options is null)
                    throw new NotInitializedException();

                return options;
            }
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
                o.AllowOfflineSettings = figOptions.AllowOfflineSettings;
                o.LoggerFactory = figOptions.LoggerFactory;
                o.VersionOverride = figOptions.VersionOverride;
            }).Build();
        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.Configure<T>(configuration).BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<T>>();
        var healthLogger = (figOptions.LoggerFactory ?? new NullLoggerFactory())
            .CreateLogger<FigConfigurationHealthCheck<T>>();
        var configurationHealthCheck = new FigConfigurationHealthCheck<T>(options, healthLogger);

        Func<Task<HealthDataContract>> healthReportProvider = async () =>
        {
            var result = await configurationHealthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
            return new HealthDataContract
            {
                Status = FigHealthReportConverter.ConvertStatus(result.Status),
                Components =
                [
                    new ComponentHealthDataContract("Fig Configuration",
                        FigHealthReportConverter.ConvertStatus(result.Status), result.Description)
                ]
            };
        };

        lock (Sync)
        {
            ResetCore();
            _options = options;
            _configurationHealthCheck = configurationHealthCheck;
            _healthReportProvider = healthReportProvider;
            HealthCheckBridge.Register(healthReportProvider);
        }
    }

    public static void Reset()
    {
        lock (Sync)
        {
            ResetCore();
        }
    }

    private static void ResetCore()
    {
        if (_healthReportProvider is not null)
        {
            HealthCheckBridge.ClearIfRegistered(_healthReportProvider);
            _healthReportProvider = null;
        }

        _configurationHealthCheck?.Dispose();
        _configurationHealthCheck = null;
        _options = null;
    }
}
