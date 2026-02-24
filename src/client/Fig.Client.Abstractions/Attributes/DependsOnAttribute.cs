using System;
using System.Linq;

namespace Fig.Client.Abstractions.Attributes;

/// <summary>
/// This attribute is used to create conditional visibility for settings based on the value of another property.
/// The setting will only be visible when the specified property has one of the valid values.
/// The setting's indent level will be automatically incremented by 1.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DependsOnAttribute : Attribute
{
    public DependsOnAttribute(string dependsOnProperty, params object[] validValues)
    {
        DependsOnProperty = dependsOnProperty ?? string.Empty;
        ValidValues = validValues?.Select(v => v?.ToString() ?? string.Empty).ToArray() ?? [];
    }
    
    /// <summary>
    /// Gets the name of the property this setting depends on.
    /// For nested settings, you can usually pass the unqualified property name (e.g. nameof(OtherSetting)).
    /// Fig first tries the current nested setting path (for example, Nested->OtherSetting),
    /// and falls back to the unqualified name only when no nested match exists.
    /// </summary>
    public string DependsOnProperty { get; }
    
    /// <summary>
    /// Gets the valid values for the dependent property that will make this setting visible.
    /// </summary>
    public string[] ValidValues { get; }
}
