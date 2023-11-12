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
            ApiUri = ReadFigApiFromEnvironmentVariable(),
            PollIntervalMs = ReadPollIntervalFromEnvironmentVariable(),
            LiveReload = _figOptions.LiveReload,
            Instance = ReadInstanceFromEnvironmentVariable(_figOptions.ClientName),
            ClientName = _figOptions.ClientName,
            VersionOverride = _figOptions.VersionOverride,
            AllowOfflineSettings = _figOptions.AllowOfflineSettings,
            SettingsType = _settingsType,
            SupportsRestart = _figOptions.SupportsRestart,
            HttpClient = _figOptions.HttpClient,
            ClientSecretOverride = _figOptions.ClientSecretOverride
        };

        var logger = source.LoggerFactory.CreateLogger<FigConfigurationBuilder>();

        if (FigIsDisabled())
        {
            logger.LogInformation("Fig is disabled via command line argument.");
        }
        else if (!IsFigApiUriValid(source.ApiUri) && !IsHttpClientOverriden())
        {
            logger.LogWarning("Empty or invalid Fig API URI. Fig configuration provider will be disabled.");
        }
        else
        {
            Add(source);
        }

        return _configurationBuilder.Build();

        bool IsFigApiUriValid(string? uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
                return false;

            return Uri.TryCreate(uri, UriKind.Absolute, out _);
        }

        bool IsHttpClientOverriden() => source?.HttpClient is not null;

        bool FigIsDisabled()
        {
            return _figOptions.CommandLineArgs?.Contains("--disable-fig=true") == true;
        }
    }

    private void ValidateClientName()
    {
        var validator = new ClientNameValidator();
        validator.Validate(_figOptions.ClientName);
    }

    private string? ReadFigApiFromEnvironmentVariable()
    {
        return Environment.GetEnvironmentVariable("FIG_API_URI");
    }

    public string? ReadInstanceFromEnvironmentVariable(string clientName)
    {
        var key = $"FIG_{clientName.Replace(" ", "")}_INSTANCE";
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