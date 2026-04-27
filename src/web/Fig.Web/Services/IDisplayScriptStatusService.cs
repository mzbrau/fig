namespace Fig.Web.Services;

public interface IDisplayScriptStatusService
{
    bool IsProcessing { get; }
    bool IsComplete { get; }
    
    event Action? OnChange;
    
    void RegisterScripts(int count);
    void ScriptCompleted();
    void MarkComplete();
    void Reset();
}
