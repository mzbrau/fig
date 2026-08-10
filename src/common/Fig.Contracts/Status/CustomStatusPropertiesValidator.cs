using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Fig.Common.NetStandard.Json;
using Newtonsoft.Json;

namespace Fig.Contracts.Status
{
    public static class CustomStatusPropertiesValidator
    {
        private static readonly Regex HexColorRegex = new(
            @"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool IsValidTextColor(string? textColor)
        {
            return !string.IsNullOrWhiteSpace(textColor) && HexColorRegex.IsMatch(textColor);
        }

        public static void ValidateOrThrow(CustomStatusPropertiesDataContract properties)
        {
            if (properties.Properties is null)
                throw new ArgumentException("Custom properties list cannot be null.");

            if (properties.Properties.Count > CustomStatusPropertiesLimits.MaxProperties)
            {
                throw new ArgumentException(
                    $"Custom status properties cannot exceed {CustomStatusPropertiesLimits.MaxProperties} entries.");
            }

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in properties.Properties)
            {
                ValidateProperty(property, names);
            }

            var json = JsonConvert.SerializeObject(properties, JsonSettings.CustomStatusProperties);
            var byteCount = Encoding.UTF8.GetByteCount(json);
            if (byteCount > CustomStatusPropertiesLimits.MaxSerializedJsonBytes)
            {
                throw new ArgumentException(
                    $"Custom status properties JSON exceeds {CustomStatusPropertiesLimits.MaxSerializedJsonBytes} bytes (was {byteCount}).");
            }
        }

        public static bool TryValidate(CustomStatusPropertiesDataContract properties, out string? error)
        {
            try
            {
                ValidateOrThrow(properties);
                error = null;
                return true;
            }
            catch (ArgumentException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static void ValidateProperty(CustomStatusPropertyDataContract property, HashSet<string> names)
        {
            if (string.IsNullOrWhiteSpace(property.Name))
                throw new ArgumentException("Custom status property name is required.");

            if (property.Name.Length > CustomStatusPropertiesLimits.MaxPropertyNameLength)
            {
                throw new ArgumentException(
                    $"Custom status property name '{property.Name}' exceeds {CustomStatusPropertiesLimits.MaxPropertyNameLength} characters.");
            }

            if (!names.Add(property.Name))
                throw new ArgumentException($"Duplicate custom status property name '{property.Name}'.");

            if (property.DisplayName is { Length: > CustomStatusPropertiesLimits.MaxStringValueLength })
            {
                throw new ArgumentException(
                    $"Custom status property display name for '{property.Name}' exceeds {CustomStatusPropertiesLimits.MaxStringValueLength} characters.");
            }

            if (!Enum.IsDefined(typeof(CustomStatusValueType), property.ValueType))
                throw new ArgumentException($"Unknown custom status value type for '{property.Name}'.");

            if (property.TextColor is not null && !IsValidTextColor(property.TextColor))
            {
                throw new ArgumentException(
                    $"Property '{property.Name}' TextColor must be a hex color (#RGB or #RRGGBB).");
            }

            ValidateValue(property);
        }

        private static void ValidateValue(CustomStatusPropertyDataContract property)
        {
            if (property.Value is null)
                return;

            switch (property.ValueType)
            {
                case CustomStatusValueType.String:
                case CustomStatusValueType.Enum:
                    var asString = Convert.ToString(property.Value, CultureInfo.InvariantCulture);
                    if (asString is null)
                        throw new ArgumentException($"Property '{property.Name}' must be a string.");
                    if (asString.Length > CustomStatusPropertiesLimits.MaxStringValueLength)
                    {
                        throw new ArgumentException(
                            $"Property '{property.Name}' string value exceeds {CustomStatusPropertiesLimits.MaxStringValueLength} characters.");
                    }

                    if (property.ValueType == CustomStatusValueType.Enum &&
                        string.IsNullOrWhiteSpace(property.EnumTypeName))
                    {
                        // EnumTypeName is optional metadata; value must still be string-like.
                    }

                    break;

                case CustomStatusValueType.Boolean:
                    if (property.Value is not bool)
                        throw new ArgumentException($"Property '{property.Name}' must be a boolean.");
                    break;

                case CustomStatusValueType.Integer:
                    if (!IsInteger(property.Value))
                        throw new ArgumentException($"Property '{property.Name}' must be an integer.");
                    break;

                case CustomStatusValueType.Long:
                    if (!IsLong(property.Value))
                        throw new ArgumentException($"Property '{property.Name}' must be a long.");
                    break;

                case CustomStatusValueType.Double:
                    if (!IsDouble(property.Value))
                        throw new ArgumentException($"Property '{property.Name}' must be a double.");
                    break;

                case CustomStatusValueType.Decimal:
                    var decimalText = Convert.ToString(property.Value, CultureInfo.InvariantCulture);
                    if (decimalText is null ||
                        !decimal.TryParse(decimalText, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                    {
                        throw new ArgumentException($"Property '{property.Name}' must be a decimal string.");
                    }

                    if (decimalText.Length > CustomStatusPropertiesLimits.MaxStringValueLength)
                    {
                        throw new ArgumentException(
                            $"Property '{property.Name}' decimal value exceeds {CustomStatusPropertiesLimits.MaxStringValueLength} characters.");
                    }

                    break;

                case CustomStatusValueType.DateTime:
                    if (!TryParseDateTime(property.Value, out _))
                        throw new ArgumentException($"Property '{property.Name}' must be an ISO-8601 DateTime.");
                    break;

                case CustomStatusValueType.DateTimeOffset:
                    if (!TryParseDateTimeOffset(property.Value, out _))
                        throw new ArgumentException($"Property '{property.Name}' must be an ISO-8601 DateTimeOffset.");
                    break;

                case CustomStatusValueType.DateOnly:
                    if (!TryParseDateOnly(property.Value))
                        throw new ArgumentException($"Property '{property.Name}' must be a yyyy-MM-dd date.");
                    break;

                case CustomStatusValueType.TimeOnly:
                    if (!TryParseTimeOnly(property.Value))
                        throw new ArgumentException($"Property '{property.Name}' must be a time-of-day string.");
                    break;

                case CustomStatusValueType.TimeSpan:
                    if (!TryParseTimeSpan(property.Value, out _))
                        throw new ArgumentException($"Property '{property.Name}' must be a TimeSpan string.");
                    break;

                case CustomStatusValueType.Guid:
                    if (!TryParseGuid(property.Value, out _))
                        throw new ArgumentException($"Property '{property.Name}' must be a Guid.");
                    break;

                default:
                    throw new ArgumentException($"Unsupported value type for property '{property.Name}'.");
            }
        }

        private static bool IsInteger(object value)
        {
            return value is sbyte or byte or short or ushort or int or uint ||
                   (value is long l && l is >= int.MinValue and <= int.MaxValue) ||
                   (value is ulong ul && ul <= int.MaxValue);
        }

        private static bool IsLong(object value)
        {
            return value is sbyte or byte or short or ushort or int or uint or long ||
                   (value is ulong ul && ul <= long.MaxValue);
        }

        private static bool IsDouble(object value)
        {
            return value is float or double or sbyte or byte or short or ushort or int or uint or long;
        }

        private static bool TryParseDateTime(object value, out DateTime result)
        {
            if (value is DateTime dt)
            {
                result = dt;
                return true;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result);
        }

        private static bool TryParseDateTimeOffset(object value, out DateTimeOffset result)
        {
            if (value is DateTimeOffset dto)
            {
                result = dto;
                return true;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result);
        }

        private static bool TryParseDateOnly(object value)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // netstandard2.0: validate format without DateOnly type.
            return DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        private static bool TryParseTimeOnly(object value)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return TimeSpan.TryParseExact(text, new[] { @"hh\:mm\:ss", @"hh\:mm\:ss\.FFFFFFF", @"h\:mm\:ss", @"h\:mm\:ss\.FFFFFFF" },
                       CultureInfo.InvariantCulture, out _) ||
                   DateTime.TryParseExact(text, new[] { "HH:mm:ss", "HH:mm:ss.FFFFFFF", "H:mm:ss", "H:mm:ss.FFFFFFF" },
                       CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        private static bool TryParseTimeSpan(object value, out TimeSpan result)
        {
            if (value is TimeSpan ts)
            {
                result = ts;
                return true;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            return TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryParseGuid(object value, out Guid result)
        {
            if (value is Guid g)
            {
                result = g;
                return true;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            return Guid.TryParse(text, out result);
        }
    }
}
