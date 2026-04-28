using Fig.Contracts.ReleaseHighlights;

namespace Fig.Api.Services;

public interface IFigReleaseDiscoveryService
{
    Task<ReleaseHighlightCatalogItemDataContract?> GetNewestAvailableReleaseHighlight();
}
