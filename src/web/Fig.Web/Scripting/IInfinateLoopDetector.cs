namespace Fig.Web.Scripting;

public interface IInfiniteLoopDetector
{
    bool IsPossibleInfiniteLoop(Guid clientId);

    void AddExecution(Guid clientId, double durationMs);
}