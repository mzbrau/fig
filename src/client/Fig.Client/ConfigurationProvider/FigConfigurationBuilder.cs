using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using Fig.Client.Configuration;
using Fig.Common.NetStandard.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fig.Client.ConfigurationProvider;

public class FigConfigurationBuilder : IConfigurationBuilder
{
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
            LoggerFactory = _figOptions.LoggerFactory,
            ApiUri = ReadFigApiFromEnvironmentVariable(),
            PollIntervalMs = _figOptions.PollIntervalMs,
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

        Logging.Logger.LoggerFactory = source.LoggerFactory ?? new NullLoggerFactory();
        var logger = Logging.Logger.CreateLogger<FigConfigurationBuilder>();

        if (string.IsNullOrWhiteSpace(source.ApiUri) || !Uri.TryCreate(source.ApiUri, UriKind.Absolute, out _))
        {
            logger.LogWarning("Empty or invalid Fig API URI. Fig configuration provider will be disabled.");
        }
        else
        {
            Add(source);
        }

        return _configurationBuilder.Build();
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
}