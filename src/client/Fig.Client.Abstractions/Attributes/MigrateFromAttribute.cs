using System;

namespace Fig.Client.Abstractions.Attributes;

/// <summary>
/// Marks a setting as renamed from a previous Fig setting name.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MigrateFromAttribute : Attribute
{
    public MigrateFromAttribute(string previousSettingName)
    {
        PreviousSettingName = previousSettingName;
    }

    public string PreviousSettingName { get; }
}
