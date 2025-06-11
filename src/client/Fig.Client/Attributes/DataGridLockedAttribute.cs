using System;

namespace Fig.Client.Attributes;

/// <summary>
/// This attribute can be used on data grid properties and will remove the add and remove column options in the UI.
/// Users will still be able to edit the values in the grid, but they will not be able to add or remove columns.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DataGridLockedAttribute : Attribute
{
}