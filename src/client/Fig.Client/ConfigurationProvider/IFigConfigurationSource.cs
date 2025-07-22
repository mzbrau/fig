﻿using Fig.Client.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Fig.Client.ConfigurationProvider;

public interface IFigConfigurationSource : IConfigurationSource, IDisposable
{
    ILoggerFactory? LoggerFactory { get; set; }

    List<string>? ApiUris { get; set; }

    double PollIntervalMs { get; set; }

    string? Instance { get; set; }

    string ClientName { get; set; }

    string? VersionOverride { get; set; }

    bool AllowOfflineSettings { get; set; }

    Type SettingsType { get; set; }
    
    bool LogAppConfigConfiguration { get; set; }

    public VersionType VersionType { get; set; }

    public bool AutomaticallyGenerateHeadings { get; set; }
}