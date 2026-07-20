using Fig.Common.NetStandard.Scripting;
using Fig.Web.Models.Setting;

namespace Fig.Web.Services;

public interface IDisplayScriptStatusService
{
    bool IsProcessing { get; }
    bool IsComplete { get; }

    int ExecutedCount { get; }
    int SucceededCount { get; }
    int FailedCount { get; }
    int SkippedCount { get; }

    bool HasRuns { get; }

    IReadOnlyList<DisplayScriptRunRecord> Runs { get; }
    
    event Action? OnChange;
    
    void RegisterScripts(int count);
    void ScriptCompleted(ScriptRunResult? result = null, string? settingName = null, string? script = null);
    void MarkComplete();
    void Reset();
}
