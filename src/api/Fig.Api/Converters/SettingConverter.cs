using Fig.Api.BusinessEntities;
using Fig.Contracts.Settings;

namespace Fig.Api.Converters;

public class SettingConverter : ISettingConverter
{
    public SettingDataContract Convert(SettingBusinessEntity setting)
    {
        return new SettingDataContract
        {
            Name = setting.Name,
            Value = setting.Value
        };
    }
    
    public SettingBusinessEntity Convert(SettingDataContract setting)
    {
        return new SettingBusinessEntity()
        {
            Name = setting.Name,
            Value = setting.Value
        };
    }
}