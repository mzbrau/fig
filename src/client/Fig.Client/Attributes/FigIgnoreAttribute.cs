using System;

namespace Fig.Client.Attributes;

/// <summary>
/// This attribute can be used on properties that would be included in a data grid and will result in them being excluded from the data grid.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class FigIgnoreAttribute : Attribute
{
}