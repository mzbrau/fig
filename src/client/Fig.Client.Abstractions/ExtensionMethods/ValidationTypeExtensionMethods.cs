using System;
using System.Reflection;
using Fig.Client.Abstractions.Validation;
using Fig.Client.Abstractions.Attributes;

namespace Fig.Client.Abstractions.ExtensionMethods;

public static class ValidationTypeExtensionMethods
{
    public static (string? Regex, string? Explanation) GetDefinition(this ValidationType validationType)
    {
        var type = typeof(ValidationType);
        var name = Enum.GetName(type, validationType);
        var attribute = type.GetField(name)
            .GetCustomAttribute<ValidationDefinitionAttribute>();

        return (attribute?.Regex, attribute?.Explanation);
    }
}