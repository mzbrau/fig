namespace Fig.Web.ReleaseHighlights;

public class ReleaseHighlightsCacheState
{
    public Guid UserId { get; set; }

    public List<string> CompletedVersions { get; set; } = [];
}
