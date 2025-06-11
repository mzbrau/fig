using System;

namespace Fig.Client.Attributes;

/// <summary>
/// This attribute indicates that the value of this string is sensitive and as a result it will not be sent to the UI and input will be masked.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SecretAttribute : Attribute
{
}