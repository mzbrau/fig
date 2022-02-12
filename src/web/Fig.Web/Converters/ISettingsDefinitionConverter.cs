using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Models;

namespace Fig.Web.Converters;

public interface ISettingsDefinitionConverter
{
    List<SettingClientConfigurationModel> Convert(IList<SettingsClientDefinitionDataContract> settingDataContracts);
}