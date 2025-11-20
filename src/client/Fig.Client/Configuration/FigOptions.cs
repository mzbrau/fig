using System;
using System.Collections.Generic;
using System.Net.Http;
using Fig.Client.Contracts;
using Fig.Client.Enums;
using Microsoft.Extensions.Logging;

namespace Fig.Client.Configuration;

public class FigOptions
{
    public string ClientName { get; set; } = null!;

    public bool LiveReload { get; set; } = true;

    public string? VersionOverride { get; set; }

    public bool AllowOfflineSettings { get; set; } = true;

    public ILoggerFactory? LoggerFactory { get; set; }

    public IEnumerable<IClientSecretProvider> ClientSecretProviders { get; set; } = [];

    // Optional override, mostly for testing.
    public HttpClient? HttpClient { get; set; }

    // Only for testing purposes
    public string? ClientSecretOverride { get; set; }
    
    // Only for testing purposes
    public string? InstanceOverride { get; set; }

    public string[]? CommandLineArgs { get; set; }

    public VersionType VersionType { get; set; } = VersionType.Assembly;

    public TimeSpan CustomActionPollInterval { get; set; } = TimeSpan.FromSeconds(5);

    public bool AutomaticallyGenerateHeadings { get; set; } = true;

    /// <summary>
    /// Timeout for individual HTTP requests to the Fig API. If not set, defaults are used based on execution context:
    /// - Windows Service: 2 seconds
    /// - Other contexts: 5 seconds
    /// </summary>
    public TimeSpan? ApiRequestTimeout { get; set; }

    /// <summary>
    /// Number of retry attempts for failed HTTP requests to the Fig API. If not set, defaults are used based on execution context:
    /// - Windows Service: 0 retries (fail fast)
    /// - Other contexts: 2 retries
    /// </summary>
    public int? ApiRetryCount { get; set; }
}