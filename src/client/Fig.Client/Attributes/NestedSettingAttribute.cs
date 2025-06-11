using System;

namespace Fig.Client.Attributes;

/// <summary>
/// Nested settings are a way of grouping or separating settings into logical sections in the application.
/// Fig just substitutes the settings within the nested setting class into the parent class.
/// Nested setting classes do not need to extend SettingBase and their properties can have all the same attributes as other settings.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NestedSettingAttribute : Attribute
{
}