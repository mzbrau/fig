using System;

namespace Fig.Contracts.ReleaseHighlights
{
    public class ReleaseHighlightViewDataContract
    {
        public ReleaseHighlightViewDataContract(string releaseVersion, string featureKey, DateTime viewedAtUtc)
        {
            ReleaseVersion = releaseVersion;
            FeatureKey = featureKey;
            ViewedAtUtc = viewedAtUtc;
        }

        public string ReleaseVersion { get; }

        public string FeatureKey { get; }

        public DateTime ViewedAtUtc { get; }
    }
}
