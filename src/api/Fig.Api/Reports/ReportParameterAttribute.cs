using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Fig.Contracts.Reports;

namespace Fig.Api.Reports;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ReportParameterAttribute : Attribute
{
    public ReportParameterAttribute(string displayName)
    {
        DisplayName = displayName;
    }

    public string DisplayName { get; }

    public ReportParameterLookupKind LookupKind { get; set; } = ReportParameterLookupKind.None;
}

public static class ReportParameterMetadataFactory
{
    public static IList<ReportParameterDataContract> Create(Type parametersType)
    {
        var result = new List<ReportParameterDataContract>();
        var nullabilityContext = new NullabilityInfoContext();
        var defaults = Activator.CreateInstance(parametersType);

        foreach (var property in parametersType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite)
                continue;

            var attr = property.GetCustomAttribute<ReportParameterAttribute>();
            var displayName = attr?.DisplayName ?? property.Name;
            var lookupKind = attr?.LookupKind ?? ReportParameterLookupKind.None;
            var defaultValue = defaults is null ? null : property.GetValue(defaults);

            result.Add(new ReportParameterDataContract(
                property.Name,
                displayName,
                MapType(property.PropertyType),
                IsRequired(property, nullabilityContext),
                defaultValue,
                lookupKind));
        }

        return result;
    }

    private static bool IsRequired(PropertyInfo property, NullabilityInfoContext nullabilityContext)
    {
        if (property.GetCustomAttribute<RequiredAttribute>() != null)
            return true;

        if (Nullable.GetUnderlyingType(property.PropertyType) != null)
            return false;

        // Bool/int optional parameters keep CLR / property-initializer defaults when omitted.
        if (property.PropertyType == typeof(bool) ||
            property.PropertyType == typeof(int) ||
            property.PropertyType == typeof(long))
            return false;

        if (property.PropertyType.IsValueType)
            return true;

        return nullabilityContext.Create(property).WriteState == NullabilityState.NotNull;
    }

    private static ReportParameterType MapType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(Guid))
            return ReportParameterType.Guid;
        if (underlying == typeof(bool))
            return ReportParameterType.Bool;
        if (underlying == typeof(DateTime))
            return ReportParameterType.DateTime;
        if (underlying == typeof(int) || underlying == typeof(long))
            return ReportParameterType.Int;
        return ReportParameterType.String;
    }
}
