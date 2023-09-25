using System;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class VerificationAttribute : Attribute
{
    public VerificationAttribute(string name, params string[] propertyArguments)
    {
        Name = name;
        SettingNames = propertyArguments;
    }

    public string Name { get; }

    public string[] SettingNames { get; }
}