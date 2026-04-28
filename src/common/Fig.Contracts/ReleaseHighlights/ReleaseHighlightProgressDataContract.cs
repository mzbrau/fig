using System.Collections.Generic;

namespace Fig.Contracts.ReleaseHighlights
{
    public class ReleaseHighlightProgressDataContract
    {
        public ReleaseHighlightProgressDataContract(
            List<ReleaseHighlightViewDataContract> viewedHighlights,
            List<ReleaseHighlightCatalogItemDataContract>? availableHighlights = null)
        {
            ViewedHighlights = viewedHighlights;
            AvailableHighlights = availableHighlights ?? new List<ReleaseHighlightCatalogItemDataContract>();
        }

        public List<ReleaseHighlightViewDataContract> ViewedHighlights { get; }

        public List<ReleaseHighlightCatalogItemDataContract> AvailableHighlights { get; }
    }
}
