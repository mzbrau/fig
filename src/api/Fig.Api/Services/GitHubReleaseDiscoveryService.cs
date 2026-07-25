using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Fig.Common;
using Fig.Contracts.ReleaseHighlights;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public partial class GitHubReleaseDiscoveryService : IFigReleaseDiscoveryService
{
    private const string ReleaseFeatureKey = "new-release-available";
    private const string LatestReleaseUrl = "https://api.github.com/repos/mzbrau/fig/releases/latest";
    private const string PlaceholderImagePath = "images/release-highlights/shared/new-release.png";
    private const string CacheKey = "fig_github_newest_release";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private static readonly JsonSerializerSettings GitHubJsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None
    };

    private readonly IOptionsMonitor<ApiSettings> _apiSettings;
    private readonly IMemoryCache _memoryCache;
    private readonly IVersionHelper _versionHelper;
    private readonly ILogger<GitHubReleaseDiscoveryService> _logger;
    private readonly HttpMessageHandler? _httpMessageHandler;

    public GitHubReleaseDiscoveryService(
        IOptionsMonitor<ApiSettings> apiSettings,
        IMemoryCache memoryCache,
        IVersionHelper versionHelper,
        ILogger<GitHubReleaseDiscoveryService> logger)
        : this(apiSettings, memoryCache, versionHelper, logger, httpMessageHandler: null)
    {
    }

    // Used by unit tests to inject a mock handler without sharing IHttpClientFactory clients.
    internal GitHubReleaseDiscoveryService(
        IOptionsMonitor<ApiSettings> apiSettings,
        IMemoryCache memoryCache,
        IVersionHelper versionHelper,
        ILogger<GitHubReleaseDiscoveryService> logger,
        HttpMessageHandler? httpMessageHandler)
    {
        _apiSettings = apiSettings;
        _memoryCache = memoryCache;
        _versionHelper = versionHelper;
        _logger = logger;
        _httpMessageHandler = httpMessageHandler;
    }

    public Task<ReleaseHighlightCatalogItemDataContract?> GetNewestAvailableReleaseHighlight()
    {
        if (_memoryCache.TryGetValue(CacheKey, out ReleaseHighlightCatalogItemDataContract? cached))
            return Task.FromResult(cached);

        return Task.FromResult<ReleaseHighlightCatalogItemDataContract?>(null);
    }

    public async Task RefreshAsync()
    {
        var (completed, result) = await TryFetchNewestAvailableReleaseHighlight();
        if (!completed)
        {
            // Transient failure: keep any existing highlight and extend its TTL.
            if (_memoryCache.TryGetValue(CacheKey, out ReleaseHighlightCatalogItemDataContract? existing) && existing != null)
                _memoryCache.Set(CacheKey, existing, CacheDuration);
            return;
        }

        _memoryCache.Set(CacheKey, result, CacheDuration);
    }

    private async Task<(bool Completed, ReleaseHighlightCatalogItemDataContract? Highlight)> TryFetchNewestAvailableReleaseHighlight()
    {
        if (!TryParseNormalizedVersion(_versionHelper.GetVersion(), out var currentVersion, out var currentVersionText))
        {
            _logger.LogWarning("Skipping GitHub release discovery because the current API version could not be parsed");
            return (true, null);
        }

        try
        {
            using var client = CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Fig.Api", "1.0"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub release discovery returned status code {StatusCode}", response.StatusCode);
                return (false, null);
            }

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonConvert.DeserializeObject<GitHubReleaseResponse>(json, GitHubJsonSettings);
            if (release == null || string.IsNullOrWhiteSpace(release.TagName))
                return (true, null);

            if (!TryParseNormalizedVersion(release.TagName, out var releaseVersion, out var releaseVersionText))
                return (true, null);

            if (releaseVersion <= currentVersion)
                return (true, null);

            var releaseUrl = string.IsNullOrWhiteSpace(release.HtmlUrl)
                ? $"https://github.com/mzbrau/fig/releases/tag/{release.TagName.Trim()}"
                : release.HtmlUrl;

            return (true, new ReleaseHighlightCatalogItemDataContract(
                releaseVersionText,
                ReleaseFeatureKey,
                $"Fig v{releaseVersionText} is available",
                $"A newer Fig release is available. You're currently running Fig v{currentVersionText}. Review the release notes for v{releaseVersionText}.",
                PlaceholderImagePath,
                int.MaxValue,
                releaseUrl,
                markViewedOnDisplay: false));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to discover newer Fig releases from GitHub");
            return (false, null);
        }
    }

    private HttpClient CreateClient()
    {
        var proxyAddress = _apiSettings.CurrentValue.GetOutboundHttpProxyAddress();
        if (!string.IsNullOrWhiteSpace(proxyAddress) &&
            Uri.TryCreate(proxyAddress, UriKind.Absolute, out var proxyUri))
        {
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

            return new HttpClient(handler, disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        if (!string.IsNullOrWhiteSpace(proxyAddress))
            _logger.LogWarning("Ignoring invalid outbound proxy address '{ProxyAddress}' for GitHub release discovery.", proxyAddress);
        else
            _logger.LogDebug("Using host default proxy resolution for GitHub release discovery.");

        // Always use a dedicated client so discovery never mutates shared IHttpClientFactory instances
        // (integration tests replace the factory with a single shared WebHookClient).
        if (_httpMessageHandler != null)
        {
            return new HttpClient(_httpMessageHandler, disposeHandler: false)
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
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

    private sealed class GitHubReleaseResponse
    {
        [JsonProperty("tag_name")]
        public string? TagName { get; set; }

        [JsonProperty("html_url")]
        public string? HtmlUrl { get; set; }
    }

    [GeneratedRegex("\\d+\\.\\d+(?:\\.\\d+){0,2}")]
    private static partial Regex SemanticVersionRegex();
}
