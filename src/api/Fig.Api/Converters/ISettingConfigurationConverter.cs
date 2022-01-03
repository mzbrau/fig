using Fig.Api.BusinessEntities;
using Fig.Contracts.SettingConfiguration;

namespace Fig.Api.Converters;

public interface ISettingConfigurationConverter
{
    SettingsClientConfigurationDataContract Convert(SettingsClientBusinessEntity businessEntity);
}