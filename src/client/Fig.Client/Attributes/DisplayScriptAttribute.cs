using System;
using Fig.Client.Scripting;

namespace Fig.Client.Attributes;

public class DisplayScriptAttribute : Attribute
{
    public DisplayScriptAttribute(string displayScript)
    {
        DisplayScript = displayScript;
        ScriptType = DisplayScriptType.Custom;
    }

    public DisplayScriptAttribute(DisplayScriptType scriptType, params object[]? parameters)
    {
        ScriptType = scriptType;
        Parameters = parameters;
        DisplayScript = string.Empty; // Placeholder, actual script generated later
    }

    public string DisplayScript { get; }
    
    public DisplayScriptType? ScriptType { get; }
    
    public object[]? Parameters { get; }
}