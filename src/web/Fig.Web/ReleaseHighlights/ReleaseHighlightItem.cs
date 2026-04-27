namespace Fig.Web.ReleaseHighlights;

public class ReleaseHighlightItem
{
    public ReleaseHighlightItem(string releaseVersion, string featureKey, string title, string description,
        string? imagePath, int sortOrder, string? readMoreUrl = null)
    {
        ReleaseVersion = releaseVersion;
        FeatureKey = featureKey;
        Title = title;
        Description = description;
        ImagePath = imagePath;
        SortOrder = sortOrder;
        ReadMoreUrl = readMoreUrl;
    }

    public string ReleaseVersion { get; }

    public string FeatureKey { get; }

    public string Title { get; }

    public string Description { get; }

    public string? ImagePath { get; }

    public int SortOrder { get; }

    public string? ReadMoreUrl { get; }

    public string ViewKey => $"{ReleaseVersion}:{FeatureKey}";
}
