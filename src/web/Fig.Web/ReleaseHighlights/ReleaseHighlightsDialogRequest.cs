namespace Fig.Web.ReleaseHighlights;

public class ReleaseHighlightsDialogRequest
{
    public ReleaseHighlightsDialogRequest(IReadOnlyList<ReleaseHighlightItem> items, int startIndex, bool isManualRecall)
    {
        Items = items;
        StartIndex = startIndex;
        IsManualRecall = isManualRecall;
    }

    public IReadOnlyList<ReleaseHighlightItem> Items { get; }

    public int StartIndex { get; }

    public bool IsManualRecall { get; }
}
