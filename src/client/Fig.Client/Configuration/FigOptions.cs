using System;
using System.Net.Http;
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

    // Optional override, mostly for testing.
    public HttpClient? HttpClient { get; set; }

    // Only for testing purposes
    public string? ClientSecretOverride { get; set; }
    
    // Only for testing purposes
    public string? InstanceOverride { get; set; }

    public string[]? CommandLineArgs { get; set; }

    public VersionType VersionType { get; set; } = VersionType.Assembly;

    public TimeSpan CustomActionPollInterval { get; set; } = TimeSpan.FromSeconds(10);
}