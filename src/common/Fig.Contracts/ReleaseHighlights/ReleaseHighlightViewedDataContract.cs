namespace Fig.Contracts.ReleaseHighlights
{
    public class ReleaseHighlightViewedDataContract
    {
        public ReleaseHighlightViewedDataContract(string releaseVersion, string featureKey)
        {
            ReleaseVersion = releaseVersion;
            FeatureKey = featureKey;
        }

        public string ReleaseVersion { get; }

        public string FeatureKey { get; }
    }
}
