using System;
using System.Collections.Generic;

namespace Fig.Contracts.CheckPoint;

public class CheckPointCollectionDataContract
{
    public CheckPointCollectionDataContract(DateTime earliestCheckPoint,
        DateTime resultStartTime,
        DateTime resultEndTime,
        IEnumerable<CheckPointDataContract> checkPoints)
    {
        EarliestCheckPoint = earliestCheckPoint;
        ResultStartTime = resultStartTime;
        ResultEndTime = resultEndTime;
        CheckPoints = checkPoints;
    }

    public DateTime EarliestCheckPoint { get; }
        
    public DateTime ResultStartTime { get; }
        
    public DateTime ResultEndTime { get; }
        
    public IEnumerable<CheckPointDataContract> CheckPoints { get; }
}