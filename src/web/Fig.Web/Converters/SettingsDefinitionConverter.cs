using Fig.Contracts;
using Fig.Contracts.Authentication;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingVerification;
using Fig.Web.Events;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Fig.Web.Services;

namespace Fig.Web.Converters;

public class SettingsDefinitionConverter : ISettingsDefinitionConverter
{
    private readonly IAccountService _accountService;

    public SettingsDefinitionConverter(IAccountService accountService)
    {
        _accountService = accountService;
    }
    
    public List<SettingClientConfigurationModel> Convert(
        IList<SettingsClientDefinitionDataContract> settingDataContracts)
    {
        return settingDataContracts.Select(Convert).ToList();
    }

    private SettingClientConfigurationModel Convert(SettingsClientDefinitionDataContract settingClientDataContract)
    {
        var model = new SettingClientConfigurationModel(settingClientDataContract.Name, 
            settingClientDataContract.Description,
            settingClientDataContract.Instance);

        model.Settings = settingClientDataContract.Settings.Select(x => Convert(x, model)).ToList();
        model.Verifications = ConvertVerifications(settingClientDataContract, model.SettingEvent);
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
        var isReadOnly = _accountService.AuthenticatedUser?.Role == Role.ReadOnly;
        
        return dataContract.ValueType.FigPropertyType() switch
        {
            FigPropertyType.String when dataContract.ValidValues != null => new DropDownSettingConfigurationModel(
                dataContract,
                parent, isReadOnly),
            FigPropertyType.String when dataContract.JsonSchema != null => new JsonSettingConfigurationModel(
                dataContract, parent, isReadOnly),
            FigPropertyType.String => new StringSettingConfigurationModel(dataContract, parent, isReadOnly),
            FigPropertyType.Int => new IntSettingConfigurationModel(dataContract, parent, isReadOnly),
            FigPropertyType.Long => new LongSettingConfigurationModel(dataContract, parent, isReadOnly),
            FigPropertyType.Double => new DoubleSettingConfigurationModel(dataContract, parent, isReadOnly),
            FigPropertyType.Bool => new BoolSettingConfigurationModel(dataContract, parent, isReadOnly),
            FigPropertyType.DataGrid => new DataGridSettingConfigurationModel(dataContract, parent, isReadOnly),
            FigPropertyType.DateTime => new DateTimeSettingConfigurationModel(dataContract, parent, isReadOnly),
            FigPropertyType.TimeSpan => new TimeSpanSettingConfigurationModel(dataContract, parent, isReadOnly),
            _ => new UnknownSettingTypeConfigurationModel(dataContract,
                parent, isReadOnly) // TODO: In the future, this should throw an exception
        };
    }
}