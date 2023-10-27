using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace Fig.Client.ConfigurationProvider;

public interface IFigConfigurationSource : IConfigurationSource
{
    ILoggerFactory? LoggerFactory { get; set; }

    string? ApiUri { get; set; }

    double PollIntervalMs { get; set; }

    bool LiveReload { get; set; }

    string? Instance { get; set; }

    string ClientName { get; set; }

    string? VersionOverride { get; set; }

    bool AllowOfflineSettings { get; set; }

    Type SettingsType { get; set; }

    bool SupportsRestart { get; set; }
}