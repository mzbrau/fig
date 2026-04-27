using System.Collections.Generic;

namespace Fig.Contracts.ReleaseHighlights
{
    public class ReleaseHighlightProgressDataContract
    {
        public ReleaseHighlightProgressDataContract(List<ReleaseHighlightViewDataContract> viewedHighlights)
        {
            ViewedHighlights = viewedHighlights;
        }

        public List<ReleaseHighlightViewDataContract> ViewedHighlights { get; }
    }
}
