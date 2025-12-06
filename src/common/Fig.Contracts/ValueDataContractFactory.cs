using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.Settings;
using Newtonsoft.Json.Linq;

namespace Fig.Contracts;

public static class ValueDataContractFactory
{
    public static SettingValueBaseDataContract CreateContract(object? value, Type type)
    {
        if (type.IsSupportedDataGridType())
        {
            return CreateDataGrid(value);
        }

        return ConvertAndReturnDataContract(value, type);
    }

    private static SettingValueBaseDataContract ConvertAndReturnDataContract(object? value, Type type)
    {
        if (value is null || value.GetType() == type)
            return CreateDataContract(value, type);

        var underlyingType = type;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            underlyingType = Nullable.GetUnderlyingType(type);
        }

        var converter = TypeDescriptor.GetConverter(underlyingType!);
        if (converter.CanConvertFrom(value.GetType()))
        {
            // Use invariant culture for consistent parsing of numeric values from environment variables
            if (value is string stringValue)
            {
                return CreateDataContract(converter.ConvertFromInvariantString(stringValue)!, type);
            }
            return CreateDataContract(converter.ConvertFrom(value)!, type);
        }

        return CreateDataContract(Convert.ChangeType(value, underlyingType!, CultureInfo.InvariantCulture), type);
    }

    private static SettingValueBaseDataContract CreateDataContract(object? value, Type type)
    {
        return type.FigPropertyType() switch
        {
            FigPropertyType.Bool => new BoolSettingDataContract(value as bool? ?? false),
            FigPropertyType.DateTime => new DateTimeSettingDataContract(value as DateTime?),
            FigPropertyType.Double => new DoubleSettingDataContract(value as double? ?? 0),
            FigPropertyType.Int => new IntSettingDataContract(value as int? ?? 0),
            FigPropertyType.Long => new LongSettingDataContract(value as long? ?? 0),
            FigPropertyType.String => new StringSettingDataContract(value as string),
            FigPropertyType.TimeSpan => new TimeSpanSettingDataContract(value as TimeSpan?),
            _ => throw new ApplicationException($"{type.FullName} did not have a valid data contract conversion")
        };
    }

    private static DataGridSettingDataContract CreateDataGrid(object? value)
    {
        if (value is null)
            return new DataGridSettingDataContract(null);

        if (value is List<Dictionary<string, object?>> list)
            return new DataGridSettingDataContract(list);

        if (value.GetType() == typeof(JArray))
        {
            var convertedValue = ((JArray)value).ToObject<List<Dictionary<string, object?>>>();
            return new DataGridSettingDataContract(convertedValue);
        }

        return new DataGridSettingDataContract(null);
    }
}