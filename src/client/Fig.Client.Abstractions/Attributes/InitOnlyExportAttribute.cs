using System;

namespace Fig.Client.Abstractions.Attributes;

/// <summary>
/// Marks a setting so value-only export split mode can place it in an init-only export file.
/// Init-only exports are intended to be imported with the "Update Values Init Only" import type.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class InitOnlyExportAttribute : Attribute
{
}
