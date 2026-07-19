using Fig.Common.NetStandard.Scripting;

namespace Fig.Web.Services;

public interface IDisplayScriptStatusService
{
    bool IsProcessing { get; }
    bool IsComplete { get; }

    int ExecutedCount { get; }
    int SucceededCount { get; }
    int FailedCount { get; }
    int SkippedCount { get; }
    
    event Action? OnChange;
    
    void RegisterScripts(int count);
    void ScriptCompleted(ScriptRunResult? result = null);
    void MarkComplete();
    void Reset();
}
