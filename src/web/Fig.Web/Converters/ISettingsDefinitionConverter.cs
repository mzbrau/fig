using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Models;

namespace Fig.Web.Converters;

public interface ISettingsDefinitionConverter
{
    IList<SettingsClientDataContract> Convert(IList<SettingClientConfigurationModel> settingModels);

    IList<SettingClientConfigurationModel> Convert(IList<SettingsClientDefinitionDataContract> settingDataContracts);
}