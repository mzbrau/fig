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
    private static readonly object Sync = new();
    private static bool IsInitialized => _options is not null;
    private static IConfigurationRoot? _configurationRoot;
    private static ServiceProvider? _serviceProvider;
    private static IOptionsMonitor<T>? _options;
    private static FigConfigurationHealthCheck<T>? _configurationHealthCheck;
    private static Func<Task<HealthDataContract>>? _healthReportProvider;

    public static IOptionsMonitor<T> Settings
    {
        get
        {
            lock (Sync)
            {
                if (!IsInitialized)
                    throw new NotInitializedException();

                return _options!;
            }
        }
    }

    public static void Initialize(FigOptions figOptions, ILogger logger)
    {
        if (figOptions is null)
            throw new ArgumentNullException(nameof(figOptions));

        if (logger is null)
            throw new ArgumentNullException(nameof(logger));

        logger.LogInformation("Initializing Fig configuration manager for settings type {SettingsType}.", typeof(T).FullName);

        IConfigurationRoot? configurationRoot = null;
        ServiceProvider? serviceProvider = null;
        FigConfigurationHealthCheck<T>? configurationHealthCheck = null;

        try
        {
            configurationRoot = new ConfigurationBuilder()
                .AddFig<T>(o =>
                {
                    o.ClientName = figOptions.ClientName;
                    o.LiveReload = figOptions.LiveReload;
                    o.AllowOfflineSettings = figOptions.AllowOfflineSettings;
                    o.LoggerFactory = figOptions.LoggerFactory;
                    o.VersionOverride = figOptions.VersionOverride;
                    o.ClientSecretProviders = figOptions.ClientSecretProviders;
                    o.HttpClient = figOptions.HttpClient;
                    o.ClientSecretOverride = figOptions.ClientSecretOverride;
                    o.InstanceOverride = figOptions.InstanceOverride;
                    o.CommandLineArgs = figOptions.CommandLineArgs;
                    o.VersionType = figOptions.VersionType;
                    o.ApiRequestTimeout = figOptions.ApiRequestTimeout;
                    o.ApiRetryCount = figOptions.ApiRetryCount;
                    o.CustomActionPollInterval = figOptions.CustomActionPollInterval;
                    o.AutomaticallyGenerateHeadings = figOptions.AutomaticallyGenerateHeadings;
                    o.LookupTableRegistrationDelay = figOptions.LookupTableRegistrationDelay;
                }).Build();

            var serviceCollection = new ServiceCollection();
            serviceProvider = serviceCollection.Configure<T>(configurationRoot).BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<T>>();
            var healthLogger = (figOptions.LoggerFactory ?? new NullLoggerFactory())
                .CreateLogger<FigConfigurationHealthCheck<T>>();
            configurationHealthCheck = new FigConfigurationHealthCheck<T>(options, healthLogger);

            var healthCheck = configurationHealthCheck;
            Func<Task<HealthDataContract>> healthReportProvider = async () =>
            {
                var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
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
                _configurationRoot = configurationRoot;
                _serviceProvider = serviceProvider;
                _options = options;
                _configurationHealthCheck = configurationHealthCheck;
                _healthReportProvider = healthReportProvider;
                HealthCheckBridge.GetHealthReportAsync = healthReportProvider;

                configurationRoot = null;
                serviceProvider = null;
                configurationHealthCheck = null;
            }
        }
        finally
        {
            configurationHealthCheck?.Dispose();
            serviceProvider?.Dispose();
            (configurationRoot as IDisposable)?.Dispose();
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
        if (_healthReportProvider is not null &&
            ReferenceEquals(HealthCheckBridge.GetHealthReportAsync, _healthReportProvider))
        {
            HealthCheckBridge.GetHealthReportAsync = null;
        }

        _healthReportProvider = null;
        _configurationHealthCheck?.Dispose();
        _configurationHealthCheck = null;
        _options = null;
        _serviceProvider?.Dispose();
        _serviceProvider = null;
        (_configurationRoot as IDisposable)?.Dispose();
        _configurationRoot = null;
    }
}
