namespace Fig.Web.Scripting;

public class InfiniteLoopDetector : IInfiniteLoopDetector
{
    private readonly Dictionary<Guid, List<Execution>> _executions = new();
    
    public bool IsPossibleInfiniteLoop(Guid clientId)
    {
        if (!_executions.ContainsKey(clientId))
            return false;

        var executions = _executions[clientId];
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
            _executions[clientId] = new List<Execution> { new(DateTime.UtcNow, durationMs) };
        }
    }

    private record Execution(DateTime Time, double Duration);
}