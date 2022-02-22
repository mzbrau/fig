using Fig.Contracts.SettingDefinitions;
using Fig.Web.Models.Setting;

namespace Fig.Web.Converters;

public interface ISettingsDefinitionConverter
{
    List<SettingClientConfigurationModel> Convert(IList<SettingsClientDefinitionDataContract> settingDataContracts);
}