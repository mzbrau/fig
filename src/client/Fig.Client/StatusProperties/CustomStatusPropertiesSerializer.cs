using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Fig.Client.Abstractions.StatusProperties;
using Fig.Contracts.Status;
using Microsoft.Extensions.Logging;

namespace Fig.Client.StatusProperties
{
    internal static class CustomStatusPropertiesSerializer
    {
        private static readonly HashSet<Type> IntegerTypes =
        [
            typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint)
        ];

        private static readonly HashSet<Type> LongTypes =
        [
            typeof(long), typeof(ulong)
        ];

        private static readonly HashSet<Type> DoubleTypes =
        [
            typeof(float), typeof(double)
        ];

        public static CustomStatusPropertiesDataContract? TryCreateSnapshot<T>(
            T instance,
            ILogger? logger = null,
            IReadOnlyDictionary<string, string>? textColors = null) where T : class
        {
            var properties = new List<CustomStatusPropertyDataContract>();
            foreach (var propertyInfo in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!propertyInfo.CanRead || propertyInfo.GetIndexParameters().Length > 0)
                    continue;

                var clrType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
                if (!TryMapType(clrType, out var valueType, out var enumTypeName))
                {
                    logger?.LogDebug(
                        "Skipping unsupported custom status property {PropertyName} of type {PropertyType}",
                        propertyInfo.Name,
                        propertyInfo.PropertyType.FullName);
                    continue;
                }

                var attribute = propertyInfo.GetCustomAttribute<StatusPropertyAttribute>();
                object? rawValue;
                try
                {
                    rawValue = propertyInfo.GetValue(instance);
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex, "Failed to read custom status property {PropertyName}", propertyInfo.Name);
                    continue;
                }

                object? wireValue = null;
                if (rawValue is not null)
                {
                    if (!TryConvertToWireValue(rawValue, valueType, out wireValue))
                    {
                        logger?.LogDebug(
                            "Skipping custom status property {PropertyName} because value could not be converted",
                            propertyInfo.Name);
                        continue;
                    }
                }

                string? textColor = null;
                textColors?.TryGetValue(propertyInfo.Name, out textColor);

                properties.Add(new CustomStatusPropertyDataContract(
                    propertyInfo.Name,
                    valueType,
                    wireValue,
                    attribute?.DisplayName,
                    enumTypeName,
                    attribute?.Highlight ?? false,
                    attribute?.ShowInUi ?? true,
                    attribute?.Order ?? 0,
                    textColor));
            }

            return new CustomStatusPropertiesDataContract(properties);
        }

        private static bool TryMapType(Type clrType, out CustomStatusValueType valueType, out string? enumTypeName)
        {
            enumTypeName = null;
            valueType = CustomStatusValueType.String;

            if (clrType == typeof(string))
            {
                valueType = CustomStatusValueType.String;
                return true;
            }

            if (clrType == typeof(bool))
            {
                valueType = CustomStatusValueType.Boolean;
                return true;
            }

            if (IntegerTypes.Contains(clrType))
            {
                valueType = CustomStatusValueType.Integer;
                return true;
            }

            if (LongTypes.Contains(clrType))
            {
                valueType = CustomStatusValueType.Long;
                return true;
            }

            if (DoubleTypes.Contains(clrType))
            {
                valueType = CustomStatusValueType.Double;
                return true;
            }

            if (clrType == typeof(decimal))
            {
                valueType = CustomStatusValueType.Decimal;
                return true;
            }

            if (clrType == typeof(DateTime))
            {
                valueType = CustomStatusValueType.DateTime;
                return true;
            }

            if (clrType == typeof(DateTimeOffset))
            {
                valueType = CustomStatusValueType.DateTimeOffset;
                return true;
            }

            if (clrType == typeof(TimeSpan))
            {
                valueType = CustomStatusValueType.TimeSpan;
                return true;
            }

            if (clrType == typeof(Guid))
            {
                valueType = CustomStatusValueType.Guid;
                return true;
            }

            if (clrType.IsEnum)
            {
                valueType = CustomStatusValueType.Enum;
                enumTypeName = clrType.Name;
                return true;
            }

            // DateOnly / TimeOnly are not available on netstandard2.0 compile-time; detect by name.
            if (clrType.FullName == "System.DateOnly")
            {
                valueType = CustomStatusValueType.DateOnly;
                return true;
            }

            if (clrType.FullName == "System.TimeOnly")
            {
                valueType = CustomStatusValueType.TimeOnly;
                return true;
            }

            return false;
        }

        private static bool TryConvertToWireValue(object rawValue, CustomStatusValueType valueType, out object? wireValue)
        {
            wireValue = null;
            switch (valueType)
            {
                case CustomStatusValueType.String:
                    wireValue = Convert.ToString(rawValue, CultureInfo.InvariantCulture);
                    return wireValue is not null;

                case CustomStatusValueType.Boolean:
                    wireValue = Convert.ToBoolean(rawValue, CultureInfo.InvariantCulture);
                    return true;

                case CustomStatusValueType.Integer:
                    wireValue = Convert.ToInt32(rawValue, CultureInfo.InvariantCulture);
                    return true;

                case CustomStatusValueType.Long:
                    if (rawValue is ulong ul && ul > long.MaxValue)
                        return false;
                    wireValue = Convert.ToInt64(rawValue, CultureInfo.InvariantCulture);
                    return true;

                case CustomStatusValueType.Double:
                    wireValue = Convert.ToDouble(rawValue, CultureInfo.InvariantCulture);
                    return true;

                case CustomStatusValueType.Decimal:
                    wireValue = Convert.ToDecimal(rawValue, CultureInfo.InvariantCulture)
                        .ToString(CultureInfo.InvariantCulture);
                    return true;

                case CustomStatusValueType.DateTime:
                    wireValue = ((DateTime)rawValue).ToString("O", CultureInfo.InvariantCulture);
                    return true;

                case CustomStatusValueType.DateTimeOffset:
                    wireValue = ((DateTimeOffset)rawValue).ToString("O", CultureInfo.InvariantCulture);
                    return true;

                case CustomStatusValueType.DateOnly:
                    // DateOnly.ToString() is culture-dependent; wire format must be yyyy-MM-dd ("O").
                    wireValue = FormatWithProvider(rawValue, "O") ?? rawValue.ToString();
                    return wireValue is not null;

                case CustomStatusValueType.TimeOnly:
                    // Prefer round-trippable formatting when available ("O" => HH:mm:ss.fffffff).
                    wireValue = FormatWithProvider(rawValue, "O") ?? rawValue.ToString();
                    return wireValue is not null;

                case CustomStatusValueType.TimeSpan:
                    wireValue = ((TimeSpan)rawValue).ToString("c", CultureInfo.InvariantCulture);
                    return true;

                case CustomStatusValueType.Guid:
                    wireValue = ((Guid)rawValue).ToString("D", CultureInfo.InvariantCulture);
                    return true;

                case CustomStatusValueType.Enum:
                    wireValue = rawValue.ToString();
                    return wireValue is not null;

                default:
                    return false;
            }
        }

        private static string? FormatWithProvider(object rawValue, string format)
        {
            var toString = rawValue.GetType().GetMethod("ToString", [typeof(string), typeof(IFormatProvider)]);
            return toString?.Invoke(rawValue, [format, CultureInfo.InvariantCulture]) as string;
        }
    }
}
