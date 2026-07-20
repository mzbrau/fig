using Fig.Contracts.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Fig.Web.Services;

public interface ILoadPerfFlagsService
{
    LoadPerfFlags Flags { get; }

    /// <summary>
    /// Reads <c>?loadPerf=</c> (persists to localStorage) or existing localStorage value, then sets
    /// <see cref="LoadPerfFlags.Current"/>.
    /// </summary>
    Task InitializeAsync();
}

public sealed class LoadPerfFlagsService : ILoadPerfFlagsService
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;

    public LoadPerfFlagsService(NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;
    }

    public LoadPerfFlags Flags { get; private set; } = LoadPerfFlags.Optimized;

    public async Task InitializeAsync()
    {
        string? raw = null;

        try
        {
            var uri = new Uri(_navigationManager.Uri);
            var query = ParseQuery(uri.Query);
            if (query.TryGetValue(LoadPerfFlags.QueryParameterName, out var fromQuery)
                && !string.IsNullOrWhiteSpace(fromQuery))
            {
                raw = fromQuery;
                // Persist so subsequent navigations (without query) keep the A/B profile.
                await _jsRuntime.InvokeVoidAsync(
                    "localStorage.setItem",
                    LoadPerfFlags.LocalStorageKey,
                    raw);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadPerfFlags: failed to read query: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                raw = await _jsRuntime.InvokeAsync<string?>(
                    "localStorage.getItem",
                    LoadPerfFlags.LocalStorageKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadPerfFlags: failed to read localStorage: {ex.Message}");
            }
        }

        Flags = LoadPerfFlags.Parse(raw);
        LoadPerfFlags.Current = Flags;
        Console.WriteLine($"LoadPerfFlags active: {Flags.ToHeaderValue()}");
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query))
            return result;

        var q = query.StartsWith('?') ? query[1..] : query;
        foreach (var pair in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq < 0)
            {
                result[Uri.UnescapeDataString(pair)] = string.Empty;
                continue;
            }

            var key = Uri.UnescapeDataString(pair[..eq]);
            var value = Uri.UnescapeDataString(pair[(eq + 1)..]);
            result[key] = value;
        }

        return result;
    }
}
