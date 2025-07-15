using System.Reflection;

namespace Fig.Client;

public readonly struct SettingDetails(
    string path,
    PropertyInfo property,
    object? defaultValue,
    string name,
    object parentInstance)
{
    public string Path { get; } = path;
    
    public PropertyInfo Property { get; } = property;
    
    public object? DefaultValue { get; } = defaultValue;

    public string Name { get; } = name;

    public object ParentInstance { get; } = parentInstance;
}