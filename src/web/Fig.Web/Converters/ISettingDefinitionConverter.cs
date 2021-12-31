using Fig.Contracts.SettingConfiguration;
using Fig.Contracts.Settings;
using Fig.Web.Models;

namespace Fig.Web.Converters;

public interface ISettingDefinitionConverter
{
    IList<SettingsDataContract> Convert(IList<SettingsConfigurationModel> settingModels);
    
    IList<SettingsConfigurationModel> Convert(IList<SettingsConfigurationDataContract> settingDataContracts);
}