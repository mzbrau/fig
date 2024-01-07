using System;

namespace Fig.Client.Attributes;

public class DisplayScriptAttribute : Attribute
{
    public DisplayScriptAttribute(string displayScript)
    {
        DisplayScript = displayScript;
    }

    public string DisplayScript { get; }
}