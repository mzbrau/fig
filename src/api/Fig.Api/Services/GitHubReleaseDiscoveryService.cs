using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Fig.Common;
using Fig.Contracts.ReleaseHighlights;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fig.Api.Services;

public partial class GitHubReleaseDiscoveryService : IFigReleaseDiscoveryService
{
    private const string ReleaseFeatureKey = "new-release-available";
    private const string ReleasesUrl = "https://github.com/mzbrau/fig/releases";
    private const string PlaceholderImagePath = "images/release-highlights/shared/new-release.png";
    private const string CacheKey = "fig_github_newest_release";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
    private readonly IOptionsMonitor<ApiSettings> _apiSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly IVersionHelper _versionHelper;
    private readonly ILogger<GitHubReleaseDiscoveryService> _logger;

    public GitHubReleaseDiscoveryService(
        IOptionsMonitor<ApiSettings> apiSettings,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        IVersionHelper versionHelper,
        ILogger<GitHubReleaseDiscoveryService> logger)
    {
        _apiSettings = apiSettings;
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _versionHelper = versionHelper;
        _logger = logger;
    }

    public async Task<ReleaseHighlightCatalogItemDataContract?> GetNewestAvailableReleaseHighlight()
    {
        if (_memoryCache.TryGetValue(CacheKey, out ReleaseHighlightCatalogItemDataContract? cached))
            return cached;

        var result = await FetchNewestAvailableReleaseHighlight();
        _memoryCache.Set(CacheKey, result, CacheDuration);
        return result;
    }

    private async Task<ReleaseHighlightCatalogItemDataContract?> FetchNewestAvailableReleaseHighlight()
    {
        if (!TryParseNormalizedVersion(_versionHelper.GetVersion(), out var currentVersion, out var currentVersionText))
        {
            _logger.LogWarning("Skipping GitHub release discovery because the current API version could not be parsed");
            return null;
        }

        try
        {
            var clientLease = CreateClient();
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesUrl);
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Fig.Api", "1.0"));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

                using var response = await clientLease.Client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GitHub release discovery returned status code {StatusCode}", response.StatusCode);
                    return null;
                }

                var html = await response.Content.ReadAsStringAsync();
                var release = FindNewestRelease(html, currentVersion);
                if (release == null)
                    return null;

                return new ReleaseHighlightCatalogItemDataContract(
                    release.ReleaseVersion,
                    ReleaseFeatureKey,
                    $"Fig v{release.ReleaseVersion} is available",
                    $"A newer Fig release is available. You're currently running Fig v{currentVersionText}. Review the release notes for v{release.ReleaseVersion}.",
                    PlaceholderImagePath,
                    int.MaxValue,
                    release.ReleaseUrl,
                    markViewedOnDisplay: false);
            }
            finally
            {
                if (clientLease.DisposeClient)
                    clientLease.Client.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to discover newer Fig releases from GitHub");
            return null;
        }
    }

    private HttpClientLease CreateClient()
    {
        var proxyAddress = _apiSettings.CurrentValue.GetOutboundHttpProxyAddress();
        if (string.IsNullOrWhiteSpace(proxyAddress))
        {
            _logger.LogDebug("Using host default proxy resolution for GitHub release discovery.");
            return new HttpClientLease(CreateDefaultClient(), DisposeClient: false);
        }

        if (!Uri.TryCreate(proxyAddress, UriKind.Absolute, out var proxyUri))
        {
            _logger.LogWarning("Ignoring invalid outbound proxy address '{ProxyAddress}' for GitHub release discovery.", proxyAddress);
            return new HttpClientLease(CreateDefaultClient(), DisposeClient: false);
        }

        _logger.LogInformation("Using outbound proxy {ProxyAddress} for GitHub release discovery.", proxyUri);

        var proxy = new WebProxy(proxyUri)
        {
            Credentials = CredentialCache.DefaultCredentials
        };

        var handler = new HttpClientHandler
        {
            UseProxy = true,
            Proxy = proxy,
            DefaultProxyCredentials = CredentialCache.DefaultCredentials
        };

        return new HttpClientLease(new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(5)
        }, DisposeClient: true);
    }

    private HttpClient CreateDefaultClient()
    {
        var client = _httpClientFactory.CreateClient();
        if (client.Timeout > TimeSpan.FromSeconds(5))
            client.Timeout = TimeSpan.FromSeconds(5);

        return client;
    }

    private static ReleaseCandidate? FindNewestRelease(string html, Version currentVersion)
    {
        var releases = ReleaseLinkRegex()
            .Matches(html)
            .Select(match => CreateCandidate(
                WebUtility.HtmlDecode(match.Groups["tag"].Value),
                WebUtility.HtmlDecode(match.Groups["path"].Value)))
            .Where(candidate => candidate != null)
            .Cast<ReleaseCandidate>()
            .GroupBy(candidate => candidate.ReleaseVersion, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(candidate => candidate.NormalizedVersion)
            .ToList();

        return releases.FirstOrDefault(candidate => candidate.NormalizedVersion > currentVersion);
    }

    private static ReleaseCandidate? CreateCandidate(string rawTag, string relativePath)
    {
        if (!TryParseNormalizedVersion(rawTag, out var normalizedVersion, out var releaseVersion))
            return null;

        return new ReleaseCandidate(
            normalizedVersion,
            releaseVersion,
            new Uri(new Uri(ReleasesUrl), relativePath).ToString());
    }

    private static bool TryParseNormalizedVersion(string versionText, out Version normalizedVersion, out string releaseVersion)
    {
        var match = SemanticVersionRegex().Match(versionText);
        if (!match.Success || !Version.TryParse(match.Value, out var parsedVersion))
        {
            normalizedVersion = new Version(0, 0, 0, 0);
            releaseVersion = string.Empty;
            return false;
        }

        normalizedVersion = NormalizeVersion(parsedVersion);
        releaseVersion = match.Value;
        return true;
    }

    private static Version NormalizeVersion(Version version)
    {
        return new Version(version.Major, version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));
    }

    private sealed record HttpClientLease(HttpClient Client, bool DisposeClient);

    private sealed record ReleaseCandidate(Version NormalizedVersion, string ReleaseVersion, string ReleaseUrl);

    [GeneratedRegex("href=\"(?<path>/mzbrau/fig/releases/tag/(?<tag>[^\"]+))\"", RegexOptions.IgnoreCase)]
    private static partial Regex ReleaseLinkRegex();

    [GeneratedRegex("\\d+\\.\\d+(?:\\.\\d+){0,2}")]
    private static partial Regex SemanticVersionRegex();
}
