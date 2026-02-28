using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace Fig.Common.NetStandard.Json;

/// <summary>
/// Restricts Newtonsoft.Json TypeNameHandling deserialization to known Fig types,
/// preventing arbitrary type instantiation attacks (SEC-01).
/// Only contract and shared assemblies are allowed — server assemblies are excluded.
/// Generic type arguments are recursively validated.
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
        "Fig.Client",
        "Fig.Web" // Needed for internal Web model serialization (DataGrid deep cloning)
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

    private static readonly HashSet<string> AllowedGenericTypeNames = new(StringComparer.Ordinal)
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
        // Allow Fig types from known contract/shared assemblies only
        if (!string.IsNullOrEmpty(assemblyName) && IsFigAssembly(assemblyName!))
            return true;

        // Allow specific system primitives
        var baseTypeName = GetBaseTypeName(typeName);
        if (AllowedSystemTypeNames.Contains(baseTypeName))
            return true;

        // Allow known generic containers, but recursively validate their type arguments
        if (AllowedGenericTypeNames.Contains(baseTypeName))
            return AreGenericArgumentsAllowed(typeName);

        return false;
    }

    private static bool IsFigAssembly(string assemblyName)
    {
        return AllowedAssemblyPrefixes.Any(prefix =>
            assemblyName.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
            assemblyName.StartsWith(prefix + ",", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Validates that all generic type arguments in an assembly-qualified type name are allowed.
    /// Handles nested generics like List`1[[Dictionary`2[[String,...],[Int32,...]],...]]
    /// </summary>
    internal static bool AreGenericArgumentsAllowed(string typeName)
    {
        var args = ParseGenericArguments(typeName);
        
        // If we couldn't parse args but the type has brackets, deny by default
        // (prevents bypass via single-bracket syntax like List`1[Evil.Type])
        if (args == null || args.Count == 0)
        {
            var backtickIdx = typeName.IndexOf('`');
            if (backtickIdx >= 0 && typeName.IndexOf('[', backtickIdx) >= 0)
                return false; // Has brackets we couldn't parse — deny
            return true; // Open generic without brackets — safe
        }

        foreach (var arg in args)
        {
            var (argAssembly, argType) = ParseAssemblyQualifiedName(arg.Trim());
            
            if (string.IsNullOrEmpty(argType))
                continue;
            
            // Recursively validate each argument
            if (!IsAllowedType(argAssembly, argType))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Parses top-level generic arguments from a type name.
    /// E.g., "List`1[[System.String, mscorlib]]" → ["System.String, mscorlib"]
    /// E.g., "Dictionary`2[[System.String, mscorlib],[System.Int32, mscorlib]]" → ["System.String, mscorlib", "System.Int32, mscorlib"]
    /// </summary>
    internal static List<string>? ParseGenericArguments(string typeName)
    {
        // Find the start of generic arguments: first [[ after the backtick
        var backtickIdx = typeName.IndexOf('`');
        if (backtickIdx < 0)
            return null;

        var outerStart = typeName.IndexOf("[[", backtickIdx, StringComparison.Ordinal);
        if (outerStart < 0)
            return null;

        var result = new List<string>();
        var i = outerStart + 1; // skip the outer [

        while (i < typeName.Length)
        {
            if (typeName[i] == '[')
            {
                // Find matching close bracket, accounting for nesting
                var depth = 1;
                var start = i + 1;
                i++;
                while (i < typeName.Length && depth > 0)
                {
                    if (typeName[i] == '[') depth++;
                    else if (typeName[i] == ']') depth--;
                    i++;
                }
                result.Add(typeName.Substring(start, i - start - 1));
            }
            else if (typeName[i] == ']')
            {
                break; // end of outer argument list
            }
            else
            {
                i++;
            }
        }

        return result;
    }

    /// <summary>
    /// Splits "System.String, mscorlib, Version=..." into (assembly: "mscorlib, ...", typeName: "System.String").
    /// For types without assembly, returns (null, typeName).
    /// </summary>
    internal static (string? assembly, string typeName) ParseAssemblyQualifiedName(string qualifiedName)
    {
        // Generic types contain brackets — find the assembly part after the last ]]
        var lastBracket = qualifiedName.LastIndexOf(']');
        int commaSearchStart = lastBracket >= 0 ? lastBracket + 1 : 0;

        var commaIdx = qualifiedName.IndexOf(',', commaSearchStart);
        if (commaIdx < 0)
            return (null, qualifiedName.Trim());

        var typePart = qualifiedName.Substring(0, commaIdx).Trim();
        var assemblyPart = qualifiedName.Substring(commaIdx + 1).Trim();
        return (assemblyPart, typePart);
    }

    private static string GetBaseTypeName(string typeName)
    {
        var bracketIndex = typeName.IndexOf('[');
        return bracketIndex >= 0 ? typeName.Substring(0, bracketIndex) : typeName;
    }
}
