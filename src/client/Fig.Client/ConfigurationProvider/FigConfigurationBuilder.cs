using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using Fig.Client.Configuration;
using Fig.Common.NetStandard.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fig.Client.ConfigurationProvider;

public class FigConfigurationBuilder : IConfigurationBuilder
{
    private const int DefaultPollIntervalMs = 30000;
    private readonly IConfigurationBuilder _configurationBuilder;
    private readonly FigOptions _figOptions;
    private readonly Type _settingsType;

    public IDictionary<string, object> Properties => _configurationBuilder.Properties;

    public IList<IConfigurationSource> Sources => _configurationBuilder.Sources;

    public FigConfigurationBuilder(IConfigurationBuilder configurationBuilder, FigOptions figOptions, Type settingsType)
    {
        _configurationBuilder = configurationBuilder ?? throw new ArgumentNullException(nameof(configurationBuilder));
        _figOptions = figOptions ?? throw new ArgumentNullException(nameof(figOptions));
        _settingsType = settingsType;
        ValidateClientName();
    }

    public IConfigurationBuilder Add(IConfigurationSource source)
    {
        return _configurationBuilder.Add(source);
    }

    public IConfigurationRoot Build()
    {
        var source = new FigConfigurationSource
        {
            LoggerFactory = _figOptions.LoggerFactory ?? new NullLoggerFactory(),
            ApiUris = ReadFigApiFromEnvironmentVariable(),
            PollIntervalMs = ReadPollIntervalFromEnvironmentVariable(),
            LiveReload = _figOptions.LiveReload,
            Instance = ReadInstanceFromEnvironmentVariable(_figOptions.ClientName),
            ClientName = _figOptions.ClientName,
            VersionOverride = _figOptions.VersionOverride,
            AllowOfflineSettings = _figOptions.AllowOfflineSettings,
            SettingsType = _settingsType,
            HttpClient = _figOptions.HttpClient,
            ClientSecretOverride = _figOptions.ClientSecretOverride ?? GetCommandLineSecretOverride(),
            LogAppConfigConfiguration = ShouldLogAppConfigConfiguration()
        };
        
        var logger = source.LoggerFactory.CreateLogger<FigConfigurationBuilder>();

        if (FigIsDisabled())
        {
            logger.LogInformation("Fig is disabled via command line argument");
        }
        else if (!IsFigApiUriValid(source.ApiUris) && !IsHttpClientOverriden())
        {
            logger.LogWarning("Empty or invalid Fig API URI. Fig configuration provider will be disabled");
        }
        else
        {
            Add(source);
        }

        return _configurationBuilder.Build();

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

    public string? ReadInstanceFromEnvironmentVariable(string clientName)
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
}