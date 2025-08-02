using System;
using System.Collections.Generic;
using System.Linq;

namespace Fig.Common.NetStandard.Scripting;

public class InfiniteLoopDetector : IInfiniteLoopDetector
{
    private readonly Dictionary<Guid, List<Execution>> _executions = new();
    
    public bool IsPossibleInfiniteLoop(Guid clientId)
    {
        if (!_executions.TryGetValue(clientId, out var executions))
            return false;

        var averageExecutionTime = executions.Average(a => a.Duration);
        
        var threshold = DateTime.UtcNow - TimeSpan.FromMilliseconds(averageExecutionTime * 11);
        var isPossibleLoop = _executions[clientId].Count(a => a.Time > threshold) > 10;
        
        if (isPossibleLoop)
            Console.WriteLine("Possible infinite loop detected in display script.");

        return isPossibleLoop;
    }

    public void AddExecution(Guid clientId, double durationMs)
    {
        if (_executions.TryGetValue(clientId, out var execution))
        {
            execution.Add(new Execution(DateTime.UtcNow, durationMs));
        }
        else
        {
            _executions[clientId] = [new Execution(DateTime.UtcNow, durationMs)];
        }
    }

    private record Execution
    {
        public DateTime Time { get; }
        public double Duration { get; }

        public Execution(DateTime time, double duration)
        {
            Time = time;
            Duration = duration;
        }
    }
}
