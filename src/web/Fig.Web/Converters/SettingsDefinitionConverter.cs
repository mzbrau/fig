using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Events;
using Fig.Web.Models;

namespace Fig.Web.Converters;

public class SettingsDefinitionConverter : ISettingsDefinitionConverter
{
    public IList<SettingsClientDataContract> Convert(IList<SettingClientConfigurationModel> settingModels)
    {
        return settingModels.Select(Convert).ToList();
    }

    public IList<SettingClientConfigurationModel> Convert(IList<SettingsClientDefinitionDataContract> settingDataContracts)
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

    private SettingsClientDataContract Convert(SettingClientConfigurationModel model)
    {
        return new SettingsClientDataContract
        {
            Name = model.Name,
            Settings = model.Settings.Select(Convert).ToList()
        };
    }

    private SettingClientConfigurationModel Convert(SettingsClientDefinitionDataContract settingClientDataContracts)
    {
        var model = new SettingClientConfigurationModel
        {
            Name = settingClientDataContracts.Name,
            Instance = settingClientDataContracts.Instance,
        };

        model.Settings = settingClientDataContracts.Settings.Select(x => Convert(x, model.SettingStateChanged)).ToList();
        model.UpdateDisplayName();
        return model;
    }

    private SettingConfigurationModel Convert(SettingDefinitionDataContract dataContract, Action<SettingEventArgs> stateChanged)
    {
        return dataContract.Value switch
        {
            string when dataContract.ValidValues != null => new DropDownSettingConfigurationModel(dataContract, stateChanged),
            string => new StringSettingConfigurationModel(dataContract, stateChanged),
            int => new IntSettingConfigurationModel(dataContract, stateChanged),
            bool => new BoolSettingConfigurationModel(dataContract, stateChanged),
            _ => new UnknownConfigurationModel(dataContract, stateChanged) // TODO: In the future, this should throw an exception
        };
    }
}