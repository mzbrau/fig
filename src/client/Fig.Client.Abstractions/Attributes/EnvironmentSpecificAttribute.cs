using System;

namespace Fig.Client.Abstractions.Attributes;

/// <summary>
/// EnvironmentSpecificAttribute is used to mark settings that are likely specific to a particular environment.
/// They can be excluded from exports to make it easier to transfer settings between environments.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class EnvironmentSpecificAttribute : Attribute
{
}