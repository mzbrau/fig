using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Api.Reports;

public class ReportParameterBinder : IReportParameterBinder
{
    public object Bind(Type parametersType, IDictionary<string, object?> rawParameters)
    {
        var instance = Activator.CreateInstance(parametersType)
                       ?? throw new InvalidOperationException($"Unable to create {parametersType.Name}");

        var errors = new List<string>();
        var definitions = ReportParameterMetadataFactory.Create(parametersType)
            .ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var property in parametersType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite)
                continue;

            definitions.TryGetValue(property.Name, out var definition);
            var hasValue = TryGetRawValue(rawParameters, property.Name, out var rawValue);

            if (!hasValue || IsEmpty(rawValue))
            {
                if (definition?.Required == true)
                    errors.Add($"Parameter '{property.Name}' is required.");
                continue;
            }

            try
            {
                var converted = ConvertValue(rawValue, property.PropertyType);
                property.SetValue(instance, converted);
            }
            catch (Exception ex)
            {
                errors.Add($"Parameter '{property.Name}' is invalid: {ex.Message}");
            }
        }

        if (errors.Count > 0)
            throw new ReportParameterValidationException(string.Join(" ", errors));

        return instance;
    }

    private static bool TryGetRawValue(IDictionary<string, object?> rawParameters, string name, out object? value)
    {
        foreach (var pair in rawParameters)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool IsEmpty(object? value)
    {
        if (value is null)
            return true;
        if (value is string s)
            return string.IsNullOrWhiteSpace(s);
        if (value is JValue { Type: JTokenType.Null })
            return true;
        return false;
    }

    private static object? ConvertValue(object? rawValue, Type targetType)
    {
        if (rawValue is null || rawValue is JValue { Type: JTokenType.Null })
            return null;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (rawValue is JToken token)
            rawValue = token.Type == JTokenType.String
                ? token.Value<string>()
                : token.ToObject(underlying);

        if (rawValue is null)
            return null;

        if (underlying.IsInstanceOfType(rawValue))
            return EnsureUtcIfDateTime(rawValue, underlying);

        if (underlying == typeof(Guid))
        {
            if (rawValue is Guid g)
                return g;
            return Guid.Parse(Convert.ToString(rawValue, CultureInfo.InvariantCulture)!);
        }

        if (underlying == typeof(DateTime))
        {
            if (rawValue is DateTime dt)
                return EnsureUtc(dt);
            if (DateTime.TryParse(Convert.ToString(rawValue, CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed))
                return EnsureUtc(parsed);
            throw new FormatException("Invalid DateTime value.");
        }

        if (underlying == typeof(bool))
        {
            if (rawValue is bool b)
                return b;
            return bool.Parse(Convert.ToString(rawValue, CultureInfo.InvariantCulture)!);
        }

        if (underlying == typeof(int))
            return Convert.ToInt32(rawValue, CultureInfo.InvariantCulture);

        if (underlying == typeof(long))
            return Convert.ToInt64(rawValue, CultureInfo.InvariantCulture);

        if (underlying == typeof(string))
            return Convert.ToString(rawValue, CultureInfo.InvariantCulture);

        var converter = TypeDescriptor.GetConverter(underlying);
        if (converter.CanConvertFrom(rawValue.GetType()))
            return converter.ConvertFrom(rawValue);

        return Convert.ChangeType(rawValue, underlying, CultureInfo.InvariantCulture);
    }

    private static object EnsureUtcIfDateTime(object value, Type underlying)
    {
        if (underlying == typeof(DateTime) && value is DateTime dt)
            return EnsureUtc(dt);
        return value;
    }

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc);
}

public class ReportParameterValidationException : Exception
{
    public ReportParameterValidationException(string message) : base(message)
    {
    }
}
