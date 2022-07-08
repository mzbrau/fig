using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;

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
        return new SettingDataContract(setting.Name, setting.Value);
    }

    public SettingBusinessEntity Convert(SettingDataContract setting)
    {
        return new SettingBusinessEntity
        {
            Name = setting.Name,
            Value = setting.Value
        };
    }

    public SettingValueDataContract Convert(SettingValueBusinessEntity businessEntity)
    {
        return new SettingValueDataContract(businessEntity.SettingName,
            _valueToStringConverter.Convert(businessEntity.Value),
            businessEntity.ChangedAt,
            businessEntity.ChangedBy);
    }
}