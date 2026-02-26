using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace Fig.Common.NetStandard.Json;

/// <summary>
/// Restricts Newtonsoft.Json TypeNameHandling deserialization to known Fig types,
/// preventing arbitrary type instantiation attacks (SEC-01).
/// </summary>
public class FigSerializationBinder : ISerializationBinder
{
    private static readonly DefaultSerializationBinder DefaultBinder = new();
    
    private static readonly HashSet<string> AllowedAssemblyPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Fig.Contracts",
        "Fig.WebHooks.Contracts",
        "Fig.Common.NetStandard",
        "Fig.Common",
        "Fig.Api",
        "Fig.Web",
        "Fig.Client",
        "Fig.Datalayer",
        "Fig.Integration",
        "Fig.Test"
    };

    private static readonly HashSet<string> AllowedSystemTypeNames = new(StringComparer.Ordinal)
    {
        "System.String",
        "System.Boolean",
        "System.Int16",
        "System.Int32",
        "System.Int64",
        "System.UInt16",
        "System.UInt32",
        "System.UInt64",
        "System.Single",
        "System.Double",
        "System.Decimal",
        "System.DateTime",
        "System.DateTimeOffset",
        "System.TimeSpan",
        "System.Guid",
        "System.Byte",
        "System.Byte[]",
        "System.SByte",
        "System.Char",
        "System.Object",
        "System.DBNull",
        "System.Uri"
    };

    private static readonly HashSet<string> AllowedGenericPrefixes = new(StringComparer.Ordinal)
    {
        "System.Collections.Generic.List`1",
        "System.Collections.Generic.Dictionary`2",
        "System.Collections.Generic.IList`1",
        "System.Collections.Generic.ICollection`1",
        "System.Collections.Generic.IEnumerable`1",
        "System.Collections.Generic.KeyValuePair`2",
        "System.Collections.Generic.HashSet`1",
        "System.Nullable`1"
    };

    public Type BindToType(string? assemblyName, string typeName)
    {
        // Allow empty/null — Newtonsoft may call this during normal resolution
        if (string.IsNullOrEmpty(typeName))
            return DefaultBinder.BindToType(assemblyName, typeName);

        if (IsAllowedType(assemblyName, typeName))
            return DefaultBinder.BindToType(assemblyName, typeName);

        throw new InvalidOperationException(
            $"Deserialization of type '{typeName}' from assembly '{assemblyName}' is not allowed.");
    }

    public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
    {
        DefaultBinder.BindToName(serializedType, out assemblyName, out typeName);
    }

    private static bool IsAllowedType(string? assemblyName, string typeName)
    {
        // Allow Fig contract types from known assemblies
        if (!string.IsNullOrEmpty(assemblyName) && IsFigAssembly(assemblyName!))
            return true;

        // Check if the type name itself indicates a Fig type (covers assembly-qualified names in generics)
        if (typeName.StartsWith("Fig.", StringComparison.Ordinal))
            return true;

        // Allow specific system types needed for DataGrid and primitive values
        var baseTypeName = GetBaseTypeName(typeName);
        if (AllowedSystemTypeNames.Contains(baseTypeName))
            return true;

        if (AllowedGenericPrefixes.Any(prefix => baseTypeName.StartsWith(prefix, StringComparison.Ordinal)))
            return true;

        // Enum types from Fig assemblies
        if (!string.IsNullOrEmpty(assemblyName) && IsFigAssembly(assemblyName!))
            return true;

        return false;
    }

    private static bool IsFigAssembly(string assemblyName)
    {
        return AllowedAssemblyPrefixes.Any(prefix =>
            assemblyName.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
            assemblyName.StartsWith(prefix + ",", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Extracts the base type name without generic type arguments for matching.
    /// E.g., "System.Collections.Generic.List`1[[System.String, ...]]" → "System.Collections.Generic.List`1"
    /// </summary>
    private static string GetBaseTypeName(string typeName)
    {
        var bracketIndex = typeName.IndexOf('[');
        return bracketIndex >= 0 ? typeName.Substring(0, bracketIndex) : typeName;
    }
}
