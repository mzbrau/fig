using Fig.Api.ExtensionMethods;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Api.Converters;

public class SettingConverter : ISettingConverter
{
    private readonly IValueToStringConverter _valueToStringConverter;

    public SettingConverter(IValueToStringConverter valueToStringConverter)
    {
        _valueToStringConverter = valueToStringConverter;
    }

    public SettingDataContract Convert(SettingBusinessEntity setting)
    {
        return new SettingDataContract(setting.Name, Convert(setting.Value, setting.HasSchema()));
    }

    public SettingBusinessEntity Convert(SettingDataContract setting)
    {
        return new SettingBusinessEntity
        {
            Name = setting.Name,
            Value = Convert(setting.Value)
        };
    }

    public SettingValueDataContract Convert(SettingValueBusinessEntity businessEntity)
    {
        return new SettingValueDataContract(businessEntity.SettingName,
            _valueToStringConverter.Convert(businessEntity.Value?.GetValue()),
            businessEntity.ChangedAt,
            businessEntity.ChangedBy);
    }

    public SettingValueBaseDataContract? Convert(SettingValueBaseBusinessEntity? value, bool hasSchema)
    {
        if (value is null)
            return null;

        return value switch
        {
            StringSettingBusinessEntity s when hasSchema => new JsonSettingDataContract(s.Value),
            StringSettingBusinessEntity s => new StringSettingDataContract(s.Value),
            BoolSettingBusinessEntity s => new BoolSettingDataContract(s.Value),
            DataGridSettingBusinessEntity s => new DataGridSettingDataContract(s.Value),
            DateTimeSettingBusinessEntity s => new DateTimeSettingDataContract(s.Value),
            DoubleSettingBusinessEntity s => new DoubleSettingDataContract(s.Value),
            IntSettingBusinessEntity s => new IntSettingDataContract(s.Value),
            TimeSpanSettingBusinessEntity s => new TimeSpanSettingDataContract(s.Value),
            LongSettingBusinessEntity s => new LongSettingDataContract(s.Value),
            _ => throw new NotImplementedException($"'{value?.GetType()}' is not implemented.")
        };
    }
    
    public SettingValueBaseBusinessEntity? Convert(SettingValueBaseDataContract? value)
    {
        if (value is null)
            return null;
        
        return value switch
        {
            StringSettingDataContract s => new StringSettingBusinessEntity(s.Value),
            BoolSettingDataContract s => new BoolSettingBusinessEntity(s.Value),
            DataGridSettingDataContract s => new DataGridSettingBusinessEntity(s.Value),
            DateTimeSettingDataContract s => new DateTimeSettingBusinessEntity(s.Value),
            DoubleSettingDataContract s => new DoubleSettingBusinessEntity(s.Value),
            IntSettingDataContract s => new IntSettingBusinessEntity(s.Value),
            TimeSpanSettingDataContract s => new TimeSpanSettingBusinessEntity(s.Value),
            LongSettingDataContract s => new LongSettingBusinessEntity(s.Value),
            JsonSettingDataContract s => new StringSettingBusinessEntity(s.Value),
            _ => throw new NotImplementedException($"'{value.GetType()}' is not implemented.")
        };
    }
}