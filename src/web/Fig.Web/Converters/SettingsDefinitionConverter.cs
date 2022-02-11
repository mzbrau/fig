using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Fig.Web.Events;
using Fig.Web.Models;

namespace Fig.Web.Converters;

public class SettingsDefinitionConverter : ISettingsDefinitionConverter
{
    public List<SettingsClientDataContract> Convert(IList<SettingClientConfigurationModel> settingModels)
    {
        return settingModels.Select(Convert).ToList();
    }

    public List<SettingClientConfigurationModel> Convert(
        IList<SettingsClientDefinitionDataContract> settingDataContracts)
    {
        return settingDataContracts.Select(Convert).ToList();
    }

    private SettingDataContract Convert(ISetting model)
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

    private SettingClientConfigurationModel Convert(SettingsClientDefinitionDataContract settingClientDataContract)
    {
        var model = new SettingClientConfigurationModel(settingClientDataContract.Name,
            settingClientDataContract.Instance);

        model.Settings = settingClientDataContract.Settings.Select(x => Convert(x, model)).ToList();
        model.Verifications = ConvertVerifications(settingClientDataContract, model.SettingEvent);
        model.UpdateDisplayName();
        model.CalculateSettingVerificationRelationship();
        return model;
    }

    private List<SettingVerificationModel> ConvertVerifications(
        SettingsClientDefinitionDataContract settingClientDataContract,
        Func<SettingEventModel, Task<object>> settingEvent)
    {
        var verifications = settingClientDataContract.PluginVerifications.Select(a => Convert(a, settingEvent));
        verifications.Union(settingClientDataContract.DynamicVerifications.Select(a => Convert(a, settingEvent)));
        return verifications.ToList();
    }

    private SettingVerificationModel Convert(SettingDynamicVerificationDefinitionDataContract dynamicVerification,
        Func<SettingEventModel, Task<object>> settingEvent)
    {
        return new SettingVerificationModel(settingEvent)
        {
            Name = dynamicVerification.Name,
            Description = dynamicVerification.Description,
            VerificationType = "Dynamic",
            SettingsVerified = dynamicVerification.SettingsVerified
        };
    }

    private SettingVerificationModel Convert(SettingPluginVerificationDefinitionDataContract pluginVerification,
        Func<SettingEventModel, Task<object>> settingEvent)
    {
        return new SettingVerificationModel(settingEvent)
        {
            Name = pluginVerification.Name,
            Description = pluginVerification.Description,
            VerificationType = "Plugin",
            SettingsVerified = pluginVerification.PropertyArguments
        };
    }

    private ISetting Convert(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
    {
        return dataContract.Value switch
        {
            string when dataContract.ValidValues != null => new DropDownSettingConfigurationModel(dataContract,
                parent),
            string => new StringSettingConfigurationModel(dataContract, parent),
            int => new IntSettingConfigurationModel(dataContract, parent),
            bool => new BoolSettingConfigurationModel(dataContract, parent),
            _ => new UnknownConfigurationModel(dataContract,
                parent) // TODO: In the future, this should throw an exception
        };
    }
}