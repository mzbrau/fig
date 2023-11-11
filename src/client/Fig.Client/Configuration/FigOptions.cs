using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Fig.Client.Configuration;

public class FigOptions
{
    public string ClientName { get; set; } = null!;

    public bool LiveReload { get; set; } = true;

    public string? VersionOverride { get; set; }

    public bool AllowOfflineSettings { get; set; } = true;

    public ILoggerFactory? LoggerFactory { get; set; }
    
    public bool SupportsRestart { get; set; }

    // Optional override, mostly for testing.
    public HttpClient? HttpClient { get; set; }

    // Only for testing purposes
    public string? ClientSecretOverride { get; set; }

    public string[]? CommandLineArgs { get; set; }
}