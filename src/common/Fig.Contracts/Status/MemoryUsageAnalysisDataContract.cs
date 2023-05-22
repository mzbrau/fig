using System;

namespace Fig.Contracts.Status;

public class MemoryUsageAnalysisDataContract
{
    public MemoryUsageAnalysisDataContract(DateTime timeOfAnalysisUtc,
        bool possibleMemoryLeakDetected,
        double trendLineSlope,
        double average,
        double standardDeviation,
        double startingAverage,
        double endingAverage,
        int secondsAnalyzed,
        int dataPointsAnalyzed)
    {
        TimeOfAnalysisUtc = timeOfAnalysisUtc;
        PossibleMemoryLeakDetected = possibleMemoryLeakDetected;
        TrendLineSlope = trendLineSlope;
        Average = average;
        StandardDeviation = standardDeviation;
        StartingAverage = startingAverage;
        EndingAverage = endingAverage;
        SecondsAnalyzed = secondsAnalyzed;
        DataPointsAnalyzed = dataPointsAnalyzed;
    }

    public DateTime TimeOfAnalysisUtc { get; }

    public bool PossibleMemoryLeakDetected { get; } 

    public double TrendLineSlope { get; }
    
    public double Average { get; }
    
    public double StandardDeviation { get; }
    
    public double StartingAverage { get; }
    
    public double EndingAverage { get; }
    
    public int SecondsAnalyzed { get; }
    
    public int DataPointsAnalyzed { get; }
}