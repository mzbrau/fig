using System;

namespace Fig.Contracts.Scheduling;

public class ScheduleDataContract
{
    public ScheduleDataContract(DateTime? applyAtUtc, DateTime? revertAtUtc)
    {
        ApplyAtUtc = applyAtUtc;
        RevertAtUtc = revertAtUtc;
    }

    public DateTime? ApplyAtUtc { get; set; }
    
    public DateTime? RevertAtUtc { get; set; }
}