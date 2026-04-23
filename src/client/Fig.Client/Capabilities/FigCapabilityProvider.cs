using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fig.Common.NetStandard.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Client.Capabilities;

internal sealed class FigCapabilityProvider : IFigCapabilityProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private HashSet<string> _features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

            var response = await _httpClient.GetAsync("/capabilities").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("GET /capabilities returned {StatusCode}; assuming no optional features", response.StatusCode);
                _fetched = true;
                return;
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var obj = JsonConvert.DeserializeObject<JObject>(json, JsonSettings.FigDefault);
            var features = obj?["supportedFeatures"]?.ToObject<List<string>>(JsonSerializer.Create(JsonSettings.FigDefault));
            _features = new HashSet<string>(features ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            _logger.LogDebug("Fig API capabilities: [{Features}]", string.Join(", ", _features));
            _fetched = true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch Fig API capabilities; assuming no optional features");
            _fetched = true;
        }
        finally
        {
            _lock.Release();
        }
    }
}
