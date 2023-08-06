using System;
using System.Collections;
using System.Collections.Generic;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.Settings;

namespace Fig.Contracts;

public static class ValueDataContractFactory
{
    public static SettingValueBaseDataContract CreateContract(object? value, Type type)
    {
        if (type.IsSupportedDataGridType())
        {
            return new DataGridSettingDataContract(value as List<Dictionary<string, object?>>);
        }
        
        return type.FigPropertyType() switch
        {
            FigPropertyType.Bool => new BoolSettingDataContract(value as bool? ?? false),
            FigPropertyType.DateTime => new DateTimeSettingDataContract(value as DateTime?),
            FigPropertyType.Double => new DoubleSettingDataContract(value as double? ?? 0),
            FigPropertyType.Int => new IntSettingDataContract(value as int? ?? 0),
            FigPropertyType.Long => new LongSettingDataContract(value as long? ?? 0),
            FigPropertyType.String => new StringSettingDataContract(value as string),
            FigPropertyType.TimeSpan => new TimeSpanSettingDataContract(value as TimeSpan? ?? TimeSpan.Zero),
            _ => throw new ApplicationException($"{type.FullName} did not have a valid data contract conversion")
        };
    }
}