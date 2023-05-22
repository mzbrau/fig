namespace Fig.Datalayer.BusinessEntities;

public class MemoryUsageAnalysisBusinessEntity
{
    public DateTime TimeOfAnalysisUtc { get; set; }

    public bool PossibleMemoryLeakDetected => TrendLineSlope > 0 && 
                                              EndingAverage - StartingAverage > StandardDeviation;
    
    public double TrendLineSlope { get; set; }
    
    public double Average { get; set; }
    
    public double StandardDeviation { get; set; }
    
    public double StartingAverage { get; set; }
    
    public double EndingAverage { get; set; }
    
    public int SecondsAnalyzed { get; set; }
    
    public int DataPointsAnalyzed { get; set; }
}