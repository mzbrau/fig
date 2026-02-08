namespace Fig.Web.Models.Compare;

public class CompareStatisticsModel
{
    public int TotalSettings { get; set; }

    public int MatchCount { get; set; }

    public int DifferenceCount { get; set; }

    public int OnlyInLiveCount { get; set; }

    public int OnlyInExportCount { get; set; }

    public int NotComparedCount { get; set; }
}
