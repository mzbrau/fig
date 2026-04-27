namespace Fig.Web.ReleaseHighlights;

public interface IReleaseHighlightsCoordinator
{
    bool ShouldRetryAutoOpen { get; }

    Task<ReleaseHighlightsDialogRequest?> GetAutoOpenDialog();

    Task<ReleaseHighlightsDialogRequest?> GetManualRecallDialog();

    Task<bool> RecordViewed(ReleaseHighlightItem item);

    void ResetSession();
}
