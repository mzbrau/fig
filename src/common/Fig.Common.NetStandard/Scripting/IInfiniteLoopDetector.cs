using System;

namespace Fig.Common.NetStandard.Scripting;

public interface IInfiniteLoopDetector
{
    bool IsPossibleInfiniteLoop(Guid clientId);
    
    void AddExecution(Guid clientId, double durationMs);
}
