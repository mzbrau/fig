using System;

namespace Fig.Client.Attributes;

/// <summary>
/// This attribute should be used for data grid properties.
/// It will result in that column being read-only in the UI.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ReadOnlyAttribute : Attribute
{
}