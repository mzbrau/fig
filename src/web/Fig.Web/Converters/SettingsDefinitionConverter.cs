using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingVerification;
using Fig.Web.Events;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

namespace Fig.Web.Converters;

public class SettingsDefinitionConverter : ISettingsDefinitionConverter
{
    public List<SettingClientConfigurationModel> Convert(
        IList<SettingsClientDefinitionDataContract> settingDataContracts)
    {
        return settingDataContracts?.Select(Convert).ToList() ?? new List<SettingClientConfigurationModel>();
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
        var verifications = settingClientDataContract.PluginVerifications.Select(a => Convert(a, settingEvent))
            .ToList();
        return verifications.Union(settingClientDataContract.DynamicVerifications
                .Select(a => Convert(a, settingEvent)))
            .ToList();
    }

    private SettingVerificationModel Convert(SettingDynamicVerificationDefinitionDataContract dynamicVerification,
        Func<SettingEventModel, Task<object>> settingEvent)
    {
        return new SettingVerificationModel(settingEvent,
            dynamicVerification.Name,
            dynamicVerification.Description,
            "Dynamic",
            dynamicVerification.SettingsVerified);
    }

    private SettingVerificationModel Convert(SettingPluginVerificationDefinitionDataContract pluginVerification,
        Func<SettingEventModel, Task<object>> settingEvent)
    {
        return new SettingVerificationModel(settingEvent,
            pluginVerification.Name,
            pluginVerification.Description,
            "Plugin",
            pluginVerification.PropertyArguments);
    }

    private ISetting Convert(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
    {
        Console.WriteLine(dataContract.ValueType.FullName);
        return dataContract.ValueType.FigPropertyType() switch
        {
            FigPropertyType.String when dataContract.ValidValues != null => new DropDownSettingConfigurationModel(
                dataContract,
                parent),
            FigPropertyType.String => new StringSettingConfigurationModel(dataContract, parent),
            FigPropertyType.Int => new IntSettingConfigurationModel(dataContract, parent),
            FigPropertyType.Long => new LongSettingConfigurationModel(dataContract, parent),
            FigPropertyType.Double => new DoubleSettingConfigurationModel(dataContract, parent),
            FigPropertyType.Bool => new BoolSettingConfigurationModel(dataContract, parent),
            FigPropertyType.DataGrid => new DataGridSettingConfigurationModel(dataContract, parent),
            FigPropertyType.DateTime => new DateTimeSettingConfigurationModel(dataContract, parent),
            FigPropertyType.TimeSpan => new TimeSpanSettingConfigurationModel(dataContract, parent),
            _ => new UnknownSettingTypeConfigurationModel(dataContract,
                parent) // TODO: In the future, this should throw an exception
        };
    }
}