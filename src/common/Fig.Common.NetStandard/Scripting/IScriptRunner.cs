namespace Fig.Common.NetStandard.Scripting;

public interface IScriptRunner
{
    /// <param name="bypassLoopDetection">
    /// When true, skip infinite-loop detection (used for deterministic initial-load runs).
    /// </param>
    ScriptRunResult RunScript(string? script, IScriptableClient client, bool bypassLoopDetection = false);
    
    string FormatScript(string script);
}
