namespace Fig.Web.ReleaseHighlights;

public interface IReleaseHighlightsCatalog
{
    IReadOnlyList<ReleaseHighlightItem> GetAll();
}
