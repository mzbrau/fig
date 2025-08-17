using System;

namespace Fig.Client.Abstractions.ExtensionMethods;

public static class TypeExtensionMethods
{
    public static bool IsNumeric(this Type type)
    {
        // Unwrap nullable
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            type = underlying;

        return
            type == typeof(int) ||
            type == typeof(long) ||
            type == typeof(double);
    }
}