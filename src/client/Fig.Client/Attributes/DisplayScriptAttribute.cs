using System;

namespace Fig.Client.Attributes;

/// <summary>
/// Display scripts are JavaScript snippets that can be used to modify the display of settings in the UI.
/// Using JavaScript, you can add validation, change visibility or update the value of settings among other things.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DisplayScriptAttribute : Attribute
{
    public DisplayScriptAttribute(string displayScript)
    {
        DisplayScript = displayScript;
    }

    public string DisplayScript { get; }
}