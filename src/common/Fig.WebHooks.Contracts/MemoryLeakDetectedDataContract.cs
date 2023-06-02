namespace Fig.WebHooks.Contracts;

public class MemoryLeakDetectedDataContract
{
    public MemoryLeakDetectedDataContract(string clientName,
        string? instance,
        double trendLineSlope,
        double startingBytesAverage,
        double endingBytesAverage,
        int secondsAnalyzed,
        int dataPointsAnalyzed, Uri? link)
    {
        ClientName = clientName;
        Instance = instance;
        TrendLineSlope = trendLineSlope;
        StartingBytesAverage = startingBytesAverage;
        EndingBytesAverage = endingBytesAverage;
        SecondsAnalyzed = secondsAnalyzed;
        DataPointsAnalyzed = dataPointsAnalyzed;
        Link = link;
    }

    public string ClientName { get; set; }
    
    public string? Instance { get; set; }
    
    public double TrendLineSlope { get; set; }

    public double StartingBytesAverage { get; set; }
    
    public double EndingBytesAverage { get; set; }
    
    public int SecondsAnalyzed { get; set; }
    
    public int DataPointsAnalyzed { get; set; }
    
    public Uri? Link { get; set; }
}