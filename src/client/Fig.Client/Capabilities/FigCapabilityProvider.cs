using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Capabilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client.Capabilities;

/// <summary>
/// Fetches and caches the capability flags advertised by the Fig API server.
/// <para>
/// Caching is implemented as a simple in-process <see cref="HashSet{T}"/> guarded by a
/// <see cref="SemaphoreSlim"/>.  An <c>IMemoryCache</c> is intentionally not used here:
/// the client library targets netstandard2.0, capability flags are stable for the lifetime
/// of a running server, and the flag-per-provider-instance lifetime semantics are sufficient.
/// The server side (<c>CapabilitiesController</c>) uses <c>IMemoryCache</c> to avoid
/// redundant version-helper calls across requests.
/// </para>
/// </summary>
internal sealed class FigCapabilityProvider : IFigCapabilityProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private HashSet<string> _features = new(StringComparer.OrdinalIgnoreCase);
    private bool _fetched;

    internal FigCapabilityProvider(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool Supports(string feature) => _features.Contains(feature);

    public async Task FetchAsync(bool force = false)
    {
        if (_fetched && !force)
            return;

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_fetched && !force)
                return;

            using var response = await _httpClient.GetAsync("/capabilities").ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Old server that does not have the endpoint — treat as no optional features and don't retry.
                _logger.LogDebug("GET /capabilities returned 404; old server, assuming no optional features");
                _features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _fetched = true;
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                // Transient failure — do not set _fetched so a subsequent call can retry.
                _logger.LogDebug("GET /capabilities returned {StatusCode}; will retry on next registration", response.StatusCode);
                return;
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var contract = JsonConvert.DeserializeObject<FigCapabilitiesDataContract>(json, JsonSettings.FigDefault);
            _features = new HashSet<string>(contract?.SupportedFeatures ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            _logger.LogDebug("Fig API capabilities: [{Features}]", string.Join(", ", _features));
            _fetched = true;
        }
        catch (Exception ex)
        {
            // Transient failure (network error etc.) — do not set _fetched so a subsequent call can retry.
            _logger.LogDebug(ex, "Failed to fetch Fig API capabilities; will retry on next registration");
        }
        finally
        {
            _lock.Release();
        }
    }
}
