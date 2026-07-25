using Fig.Contracts.ReleaseHighlights;

namespace Fig.Api.Services;

public interface IFigReleaseDiscoveryService
{
    /// <summary>
    /// Returns the cached newest available release highlight, or null if the cache is cold or no newer release exists.
    /// Does not perform network I/O.
    /// </summary>
    Task<ReleaseHighlightCatalogItemDataContract?> GetNewestAvailableReleaseHighlight();

    /// <summary>
    /// Fetches the newest Fig release from GitHub and updates the in-memory cache.
    /// </summary>
    Task RefreshAsync();
}
