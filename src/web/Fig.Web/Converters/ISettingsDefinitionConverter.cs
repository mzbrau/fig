using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Models;

namespace Fig.Web.Converters;

public interface ISettingsDefinitionConverter
{
    IList<SettingsClientDataContract> Convert(IList<SettingsConfigurationModel> settingModels);

    IList<SettingsConfigurationModel> Convert(IList<SettingsClientDefinitionDataContract> settingDataContracts);
}