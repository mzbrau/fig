namespace Fig.Common.NetStandard.Scripting;

public interface IScriptRunner
{
    void RunScript(string? script, IScriptableClient client);
    
    string FormatScript(string script);
}
