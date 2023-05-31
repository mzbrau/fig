namespace Fig.Datalayer.BusinessEntities;

public class MemoryUsageAnalysisBusinessEntity
{
    public DateTime TimeOfAnalysisUtc { get; set; }

    public bool PossibleMemoryLeakDetected => TrendLineSlope > 0 && 
                                              EndingBytesAverage - StartingBytesAverage > StandardDeviation;
    
    public double TrendLineSlope { get; set; }
    
    public double Average { get; set; }
    
    public double StandardDeviation { get; set; }
    
    public double StartingBytesAverage { get; set; }
    
    public double EndingBytesAverage { get; set; }
    
    public int SecondsAnalyzed { get; set; }
    
    public int DataPointsAnalyzed { get; set; }
}