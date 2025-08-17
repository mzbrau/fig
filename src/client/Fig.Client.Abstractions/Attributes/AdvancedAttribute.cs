using System;

namespace Fig.Client.Abstractions.Attributes;

/// <summary>
/// AdvancedAttribute is used to mark settings that don't usually need to change as part of normal operations.
/// They are hidden by default in the UI to reduce clutter and ensure that the settings that are most likely to be changed are more visible.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AdvancedAttribute : Attribute
{
}