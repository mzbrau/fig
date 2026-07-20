using System.Collections.Generic;

namespace Fig.Common.NetStandard.Scripting;

public interface IScriptRunner
{
    /// <param name="bypassLoopDetection">
    /// When true, skip infinite-loop detection (used for deterministic initial-load runs).
    /// </param>
    ScriptRunResult RunScript(string? script, IScriptableClient client, bool bypassLoopDetection = false);

    /// <summary>
    /// Runs multiple display scripts against one shared JS engine and setting registration.
    /// Used for initial client load; interactive edits continue to use <see cref="RunScript"/>.
    /// </summary>
    /// <param name="bypassLoopDetection">
    /// When true, skip infinite-loop detection (used for deterministic initial-load runs).
    /// </param>
    IReadOnlyList<(string SettingName, ScriptRunResult Result)> RunScripts(
        IReadOnlyList<(string SettingName, string Script)> scripts,
        IScriptableClient client,
        bool bypassLoopDetection = false);
    
    string FormatScript(string script);
}
