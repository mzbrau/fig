using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Models;

namespace Fig.Web.Converters;

public class SettingsDefinitionConverter : ISettingDefinitionConverter
{
    public IList<SettingsClientDataContract> Convert(IList<SettingsConfigurationModel> settingModels)
    {
        return settingModels.Select(Convert).ToList();
    }

    public IList<SettingsConfigurationModel> Convert(IList<SettingsClientDefinitionDataContract> settingDataContracts)
    {
        return settingDataContracts.Select(Convert).ToList();
    }

    private SettingDataContract Convert(SettingConfigurationModel model)
    {
        return new SettingDataContract
        {
            Name = model.Name,
            Value = model.GetValue()
        };
    }
    
    private SettingsClientDataContract Convert(SettingsConfigurationModel model)
    {
        return new SettingsClientDataContract
        {
            Name = model.Name,
            Settings = model.Settings.Select(Convert).ToList()
        };
    }
    
    private SettingsConfigurationModel Convert(SettingsClientDefinitionDataContract settingClientDataContracts)
    {
        return new SettingsConfigurationModel
        {
            Name = settingClientDataContracts.Name,
            Settings = settingClientDataContracts.Settings.Select(Convert).ToList()
        };
    }

    private SettingConfigurationModel Convert(SettingDefinitionDataContract dataContract)
    {
        return dataContract.Value switch
        {
            string when dataContract.ValidValues != null => new EnumSettingConfigurationModel(dataContract),
            string => new StringSettingConfigurationModel(dataContract),
            int => new IntSettingConfigurationModel(dataContract),
            _ => throw new NotSupportedException()
        };
    }
}