namespace Fig.Common.NetStandard.Scripting;

public interface IScriptRunner
{
    ScriptRunResult RunScript(string? script, IScriptableClient client);
    
    string FormatScript(string script);
}
