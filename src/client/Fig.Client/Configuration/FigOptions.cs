using System;
using System.Collections.Generic;
using System.Net.Http;
using Fig.Client.Contracts;
using Fig.Client.Enums;
using Fig.Client.Startup;
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
    /// - With offline settings: 5 seconds
    /// - Without offline settings, Windows Service: 5 seconds
    /// - Without offline settings, other contexts: 60 seconds
    /// <para>
    /// This can also be overridden at deployment time without a code change via the
    /// <c>FIG_API_REQUEST_TIMEOUT_SECONDS</c> environment variable (positive integer, seconds).
    /// The environment variable takes precedence over this property.
    /// </para>
    /// </summary>
    public TimeSpan? ApiRequestTimeout { get; set; }

    /// <summary>
    /// Number of retry attempts for failed HTTP requests to the Fig API. If not set, defaults are used based on execution context:
    /// - Windows Service: 0 retries (fail fast)
    /// - Other contexts: 2 retries
    /// </summary>
    public int? ApiRetryCount { get; set; }

    /// <summary>
    /// Delay before registering lookup tables from ILookupProvider and IKeyedLookupProvider implementations.
    /// This allows the application to fully start before lookup table registration begins.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan LookupTableRegistrationDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Optional hook called by the Fig client immediately before each long-running
    /// registration call.  Use this to signal the Windows Service Control Manager (or
    /// another supervisor) that more start-up time is needed.
    /// <para>
    /// A typical Windows-service implementation wraps
    /// <c>ServiceBase.RequestAdditionalTime(milliseconds)</c> in a class that implements
    /// <see cref="IServiceStartupExtender"/> and assigns an instance here.
    /// </para>
    /// <para>
    /// When this property is <c>null</c> (the default) the Fig client uses a no-op
    /// extender so no supervisor interaction occurs.
    /// </para>
    /// </summary>
    public IServiceStartupExtender? ServiceStartupExtender { get; set; }
}