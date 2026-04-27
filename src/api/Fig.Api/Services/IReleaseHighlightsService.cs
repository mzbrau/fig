using Fig.Contracts.ReleaseHighlights;

namespace Fig.Api.Services;

public interface IReleaseHighlightsService : IAuthenticatedService
{
    Task<ReleaseHighlightProgressDataContract> GetProgress();

    Task<ReleaseHighlightViewDataContract> RecordViewed(ReleaseHighlightViewedDataContract viewedHighlight);
}
