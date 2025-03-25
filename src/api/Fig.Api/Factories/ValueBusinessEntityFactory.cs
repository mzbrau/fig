using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Api.Factories;

public static class ValueBusinessEntityFactory
{
    public static SettingValueBaseBusinessEntity CreateBusinessEntity(object? value, Type type)
    {
        return type.FigPropertyType() switch
        {
            FigPropertyType.Bool => new BoolSettingBusinessEntity(value as bool? ?? false),
            FigPropertyType.DateTime => new DateTimeSettingBusinessEntity(value as DateTime?),
            FigPropertyType.Double => new DoubleSettingBusinessEntity(value as double? ?? 0),
            FigPropertyType.Int => new IntSettingBusinessEntity(value as int? ?? 0),
            FigPropertyType.Long => new LongSettingBusinessEntity(value as long? ?? 0),
            FigPropertyType.String => new StringSettingBusinessEntity(value as string),
            FigPropertyType.TimeSpan => new TimeSpanSettingBusinessEntity(value as TimeSpan?),
            _ => throw new ApplicationException($"{type.FullName} did not have a valid business entity conversion")
        };
    }
}