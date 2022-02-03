using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Models;

namespace Fig.Web.Converters;

public class SettingsDefinitionConverter : ISettingsDefinitionConverter
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
        var model = new SettingsConfigurationModel
        {
            Name = settingClientDataContracts.Name,
            Instance = settingClientDataContracts.Instance,
        };

        model.Settings = settingClientDataContracts.Settings.Select(x => Convert(x, model.SettingValueChanged)).ToList();
        model.UpdateDisplayName();
        return model;
    }

    private SettingConfigurationModel Convert(SettingDefinitionDataContract dataContract, Action<bool, string> valueChanged)
    {
        return dataContract.Value switch
        {
            string when dataContract.ValidValues != null => new DropDownSettingConfigurationModel(dataContract, valueChanged),
            string => new StringSettingConfigurationModel(dataContract, valueChanged),
            int => new IntSettingConfigurationModel(dataContract, valueChanged),
            bool => new BoolSettingConfigurationModel(dataContract, valueChanged),
            _ => new UnknownConfigurationModel(dataContract, valueChanged) // TODO: In the future, this should throw an exception
        };
    }
}