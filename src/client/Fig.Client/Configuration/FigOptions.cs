using System.Net.Http;
using Fig.Client.Enums;
using Fig.Client.Versions;
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

    public VersionType VersionType { get; set; } = VersionType.Assembly;
}