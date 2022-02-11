using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Models;

namespace Fig.Web.Converters;

public interface ISettingsDefinitionConverter
{
    List<SettingsClientDataContract> Convert(IList<SettingClientConfigurationModel> settingModels);

    List<SettingClientConfigurationModel> Convert(IList<SettingsClientDefinitionDataContract> settingDataContracts);
}