using System.Globalization;
using Fig.Contracts.Status;
using Humanizer;

namespace Fig.Web.Utils;

public static class CustomStatusPropertyFormatter
{
    public static string Format(CustomStatusPropertyDataContract property)
    {
        if (property.Value is null)
            return "—";

        try
        {
            return property.ValueType switch
            {
                CustomStatusValueType.Boolean => Convert.ToBoolean(property.Value, CultureInfo.InvariantCulture)
                    ? "Yes"
                    : "No",
                CustomStatusValueType.DateTime when TryParseDateTime(property.Value, out var dt)
                    => $"{dt.ToLocalTime():g} ({dt.Humanize()})",
                CustomStatusValueType.DateTimeOffset when TryParseDateTimeOffset(property.Value, out var dto)
                    => $"{dto.LocalDateTime:g} ({dto.Humanize()})",
                CustomStatusValueType.DateOnly when TryParseDateOnly(property.Value, out var dateOnly)
                    => dateOnly.ToString("d", CultureInfo.CurrentCulture),
                CustomStatusValueType.TimeOnly when TryParseTimeOnly(property.Value, out var timeOnly)
                    => timeOnly.ToString("t", CultureInfo.CurrentCulture),
                CustomStatusValueType.TimeSpan when TryParseTimeSpan(property.Value, out var ts)
                    => ts.Humanize(),
                CustomStatusValueType.Guid when TryParseGuid(property.Value, out var guid)
                    => guid.ToString("D", CultureInfo.InvariantCulture),
                CustomStatusValueType.Decimal => Convert.ToString(property.Value, CultureInfo.InvariantCulture) ?? "—",
                _ => Convert.ToString(property.Value, CultureInfo.InvariantCulture) ?? "—"
            };
        }
        catch
        {
            return Convert.ToString(property.Value, CultureInfo.InvariantCulture) ?? "—";
        }
    }

    private static bool TryParseDateTime(object value, out DateTime result)
    {
        if (value is DateTime dt)
        {
            result = dt;
            return true;
        }

        return DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out result);
    }

    private static bool TryParseDateTimeOffset(object value, out DateTimeOffset result)
    {
        if (value is DateTimeOffset dto)
        {
            result = dto;
            return true;
        }

        return DateTimeOffset.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out result);
    }

    private static bool TryParseDateOnly(object value, out DateOnly result)
    {
        if (value is DateOnly dateOnly)
        {
            result = dateOnly;
            return true;
        }

        return DateOnly.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
    }

    private static bool TryParseTimeOnly(object value, out TimeOnly result)
    {
        if (value is TimeOnly timeOnly)
        {
            result = timeOnly;
            return true;
        }

        return TimeOnly.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
    }

    private static bool TryParseTimeSpan(object value, out TimeSpan result)
    {
        if (value is TimeSpan ts)
        {
            result = ts;
            return true;
        }

        return TimeSpan.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture,
            out result);
    }

    private static bool TryParseGuid(object value, out Guid result)
    {
        if (value is Guid guid)
        {
            result = guid;
            return true;
        }

        return Guid.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out result);
    }
}
