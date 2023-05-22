using Humanizer;

namespace Fig.Web.Models.Clients;

public class MemoryUsageAnalysisModel
{
    public MemoryUsageAnalysisModel(string clientName,
        string? hostname,
        DateTime timeOfAnalysisUtc,
        bool possibleMemoryLeakDetected,
        double trendLineSlope,
        double average,
        double standardDeviation,
        double startingAverage,
        double endingAverage,
        int secondsAnalyzed,
        int dataPointsAnalyzed)
    {
        ClientName = clientName;
        Hostname = hostname;
        TimeOfAnalysisLocal = timeOfAnalysisUtc.ToLocalTime();
        PossibleMemoryLeakDetected = possibleMemoryLeakDetected;
        TrendLineSlope = trendLineSlope;
        Average = average.Bytes().Humanize();
        StandardDeviation = standardDeviation.Bytes().Humanize();
        StartingAverage = startingAverage.Bytes().Humanize();
        EndingAverage = endingAverage.Bytes().Humanize();
        SecondsAnalyzed = Convert.ToInt32(secondsAnalyzed);
        DataPointsAnalyzed = dataPointsAnalyzed;
    }

    public string ClientName { get; }
    
    public string? Hostname { get; }
    
    public DateTime TimeOfAnalysisLocal { get; }

    public bool PossibleMemoryLeakDetected { get; } 

    public double TrendLineSlope { get; }
    
    public string Average { get; }
    
    public string StandardDeviation { get; }
    
    public string StartingAverage { get; }
    
    public string EndingAverage { get; }
    
    public int SecondsAnalyzed { get; }
    
    public int DataPointsAnalyzed { get; }
}