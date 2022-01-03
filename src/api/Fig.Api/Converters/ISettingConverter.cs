using Fig.Api.BusinessEntities;
using Fig.Contracts.Settings;

namespace Fig.Api.Converters;

public interface ISettingConverter
{
    SettingDataContract Convert(SettingBusinessEntity setting);

    SettingBusinessEntity Convert(SettingDataContract setting);
}