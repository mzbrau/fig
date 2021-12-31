using Fig.Contracts.SettingConfiguration;
using Fig.Contracts.Settings;
using Fig.Web.Models;

namespace Fig.Web.Converters;

public class SettingsDefinitionConverter : ISettingDefinitionConverter
{
    public IList<SettingsDataContract> Convert(IList<SettingsConfigurationModel> settingModels)
    {
        return settingModels.Select(Convert).ToList();
    }

    public IList<SettingsConfigurationModel> Convert(IList<SettingsConfigurationDataContract> settingDataContracts)
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
    
    private SettingsDataContract Convert(SettingsConfigurationModel model)
    {
        return new SettingsDataContract
        {
            ServiceName = model.Name,
            Settings = model.Settings.Select(Convert).ToList()
        };
    }
    
    private SettingsConfigurationModel Convert(SettingsConfigurationDataContract settingDataContracts)
    {
        return new SettingsConfigurationModel
        {
            Name = settingDataContracts.ServiceName,
            Settings = settingDataContracts.Settings.Select(Convert).ToList()
        };
    }

    private SettingConfigurationModel Convert(SettingConfigurationDataContract dataContract)
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