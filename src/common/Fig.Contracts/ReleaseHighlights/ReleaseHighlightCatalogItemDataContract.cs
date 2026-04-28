namespace Fig.Contracts.ReleaseHighlights
{
    public class ReleaseHighlightCatalogItemDataContract
    {
        public ReleaseHighlightCatalogItemDataContract(
            string releaseVersion,
            string featureKey,
            string title,
            string description,
            string imagePath,
            int sortOrder,
            string? readMoreUrl = null,
            bool markViewedOnDisplay = true)
        {
            ReleaseVersion = releaseVersion;
            FeatureKey = featureKey;
            Title = title;
            Description = description;
            ImagePath = imagePath;
            SortOrder = sortOrder;
            ReadMoreUrl = readMoreUrl;
            MarkViewedOnDisplay = markViewedOnDisplay;
        }

        public string ReleaseVersion { get; }

        public string FeatureKey { get; }

        public string Title { get; }

        public string Description { get; }

        public string ImagePath { get; }

        public int SortOrder { get; }

        public string? ReadMoreUrl { get; }

        public bool MarkViewedOnDisplay { get; }
    }
}
