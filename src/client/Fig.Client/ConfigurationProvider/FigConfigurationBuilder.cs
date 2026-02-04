using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using Fig.Client.Configuration;
using Fig.Client.Contracts;
using Fig.Client.CustomActions;
using Fig.Client.SettingDefinitions;
using Fig.Client.Versions;
using Fig.Common.NetStandard.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fig.Client.ConfigurationProvider;

public class FigConfigurationBuilder : IConfigurationBuilder
{
    private const int DefaultPollIntervalMs = 30000;
    private readonly IConfigurationBuilder _configurationBuilder;
    private readonly FigOptions _figOptions;

    public IDictionary<string, object> Properties => _configurationBuilder.Properties;

    public IList<IConfigurationSource> Sources => _configurationBuilder.Sources;

    public FigConfigurationBuilder(IConfigurationBuilder configurationBuilder, FigOptions figOptions, Type settingsType)
    {
        _configurationBuilder = configurationBuilder ?? throw new ArgumentNullException(nameof(configurationBuilder));
        _figOptions = figOptions ?? throw new ArgumentNullException(nameof(figOptions));
        ValidateClientName();
        
        CustomActionBridge.CustomActionPollInterval = figOptions.CustomActionPollInterval;

        // Handle --setting-definitions argument
        if (ShouldExportSettingDefinitions())
        {
            ExportSettingDefinitions(settingsType);
            Environment.Exit(0);
        }
 
        var source = new FigConfigurationSource
        {
            LoggerFactory = _figOptions.LoggerFactory ?? new NullLoggerFactory(),
            ApiUris = ReadFigApiFromEnvironmentVariable(),
            PollIntervalMs = ReadPollIntervalFromEnvironmentVariable(),
            LiveReload = _figOptions.LiveReload,
            Instance = _figOptions.InstanceOverride ?? ReadInstanceFromEnvironmentVariable(_figOptions.ClientName),
            ClientName = _figOptions.ClientName,
            VersionOverride = _figOptions.VersionOverride,
            AllowOfflineSettings = _figOptions.AllowOfflineSettings,
            SettingsType = settingsType,
            HttpClient = _figOptions.HttpClient,
            ClientSecretProviders = SelectSecretProviders(_figOptions.ClientSecretProviders),
            ClientSecretOverride = _figOptions.ClientSecretOverride ?? GetCommandLineSecretOverride(),
            LogAppConfigConfiguration = ShouldLogAppConfigConfiguration(),
            VersionType = _figOptions.VersionType,
            AutomaticallyGenerateHeadings = _figOptions.AutomaticallyGenerateHeadings,
            ApiRequestTimeout = _figOptions.ApiRequestTimeout,
            ApiRetryCount = _figOptions.ApiRetryCount
        };
        
        var logger = source.LoggerFactory.CreateLogger<FigConfigurationBuilder>();

        if (FigIsDisabled())
        {
            logger.LogInformation("Fig is disabled via command line argument");
        }
        else if (!IsFigApiUriValid(source.ApiUris) && !IsHttpClientOverriden())
        {
            logger.LogWarning("Empty or invalid Fig API URI. Fig configuration provider will be disabled. To enable Fig, set the FIG_API_URI environment variable to the address of the Fig API");
        }
        else
        {
            Add(source);
        }

        bool IsFigApiUriValid(List<string>? uris)
        {
            if (uris is null || !uris.Any())
                return false;
            
            if (string.IsNullOrWhiteSpace(uris[0]))
                return false;

            return Uri.TryCreate(uris[0], UriKind.Absolute, out _);
        }

        bool IsHttpClientOverriden() => source?.HttpClient is not null;

        bool FigIsDisabled()
        {
            return _figOptions.CommandLineArgs?.Contains("--disable-fig=true") == true;
        }

        bool ShouldLogAppConfigConfiguration()
        {
            return _figOptions.CommandLineArgs?.Contains("--printappconfig") == true;
        }

        string? GetCommandLineSecretOverride()
        {
            foreach (var arg in _figOptions.CommandLineArgs ?? [])
            {
                if (arg.Contains("--secret="))
                {
                    return arg.Substring("--secret=".Length);
                }
            }

            return null;
        }

        bool ShouldExportSettingDefinitions()
        {
            return _figOptions.CommandLineArgs?.Contains("--setting-definitions") == true;
        }

        void ExportSettingDefinitions(Type settingsType)
        {
            var settings = Activator.CreateInstance(settingsType) as SettingsBase
                           ?? throw new InvalidOperationException(
                               $"Could not create settings instance for type '{settingsType.FullName}'. " +
                               $"The type must inherit from {nameof(SettingsBase)} and have a public parameterless constructor.");
            var versionProvider = new VersionProvider(new FigConfigurationSource
            {
                VersionOverride = _figOptions.VersionOverride,
                VersionType = _figOptions.VersionType
            });
            var clientVersion = versionProvider.GetHostVersion();
            var dataContract = settings.CreateDataContract(_figOptions.ClientName, _figOptions.AutomaticallyGenerateHeadings, clientVersion: clientVersion);
            
            var exporter = new SettingDefinitionsExporter();
            exporter.Export(dataContract);
        }
    }

    public IConfigurationBuilder Add(IConfigurationSource source)
    {
        return _configurationBuilder.Add(source);
    }

    public IConfigurationRoot Build()
    {
        return _configurationBuilder.Build();
    }

    private void ValidateClientName()
    {
        var validator = new ClientNameValidator();
        validator.Validate(_figOptions.ClientName);
    }

    private List<string>? ReadFigApiFromEnvironmentVariable()
    {
        var value = Environment.GetEnvironmentVariable("FIG_API_URI");
        if (value is null)
            return null;

        return value.Split(',').Select(a => a.Trim()).ToList();
    }

    private string? ReadInstanceFromEnvironmentVariable(string clientName)
    {
        var key = $"FIG_{clientName.Replace(" ", "").ToUpper()}_INSTANCE";
        return Environment.GetEnvironmentVariable(key);
    }
    
    private int ReadPollIntervalFromEnvironmentVariable()
    {
        var value = Environment.GetEnvironmentVariable("FIG_POLL_INTERVAL_MS");
        if (string.IsNullOrWhiteSpace(value))
            return DefaultPollIntervalMs;

        if (int.TryParse(value, out var result))
            return result;

        return DefaultPollIntervalMs;
    }

    private IEnumerable<IClientSecretProvider> SelectSecretProviders(
        IEnumerable<IClientSecretProvider> secretProviders)
    {
        var key = "FIG_CLIENT_SECRET_PROVIDERS";
        var providers = Environment.GetEnvironmentVariable(key);

        // No overrides
        var providersList = secretProviders.ToList();
        if (string.IsNullOrWhiteSpace(providers))
        {
            foreach (var item in providersList)
                yield return item;

            yield break;
        }

        foreach (var provider in providers.Split(',').Select(a => a.Trim()))
        {
            var match = providersList.FirstOrDefault(a => a.Name == provider);
            if (match is not null)
                yield return match;
        }
    }
}