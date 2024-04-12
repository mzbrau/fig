using Fig.Contracts;
using Fig.Contracts.Authentication;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingVerification;
using Fig.Web.Events;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Fig.Web.Notifications;
using Fig.Web.Scripting;
using Fig.Web.Services;
using Radzen;

namespace Fig.Web.Converters;

public class SettingsDefinitionConverter : ISettingsDefinitionConverter
{
    private readonly IAccountService _accountService;
    private readonly IScriptRunner _scriptRunner;
    private readonly NotificationService _notificationService;
    private readonly INotificationFactory _notificationFactory;

    public SettingsDefinitionConverter(
        IAccountService accountService, 
        IScriptRunner scriptRunner,
        NotificationService notificationService,
        INotificationFactory notificationFactory)
    {
        _accountService = accountService;
        _scriptRunner = scriptRunner;
        _notificationService = notificationService;
        _notificationFactory = notificationFactory;
    }
    
    public async Task<List<SettingClientConfigurationModel>> Convert(
        IList<SettingsClientDefinitionDataContract> settingDataContracts)
    {
        var result = new List<SettingClientConfigurationModel>();

        foreach (var contract in settingDataContracts)
        {
            try
            {
                result.Add(await Convert(contract));
                await Task.Delay(20); // Allow loading animation to spin
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _notificationService.Notify(_notificationFactory.Warning("Client Load Failed", $"Failed to load Client {contract.Name}. {e.Message}"));
            }
        }

        return result;
    }

    private async Task<SettingClientConfigurationModel> Convert(SettingsClientDefinitionDataContract settingClientDataContract)
    {
        var model = new SettingClientConfigurationModel(settingClientDataContract.Name, 
            settingClientDataContract.Description,
            settingClientDataContract.Instance,
            settingClientDataContract.HasDisplayScripts,
            _scriptRunner);

        model.Settings = new List<ISetting>();

        foreach (var setting in settingClientDataContract.Settings)
        {
            try
            {
                model.Settings.Add(Convert(setting, model));
                await Task.Delay(20); // Allow loading animation to spin
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _notificationService.Notify(_notificationFactory.Warning("Setting Load Failed", $"Failed to load Setting {setting.Name} for client {model.Name}. {e.Message}"));
            }
        }

        model.Verifications = ConvertVerifications(settingClientDataContract, model.SettingEvent);
        return model;
    }

    private List<SettingVerificationModel> ConvertVerifications(
        SettingsClientDefinitionDataContract settingClientDataContract,
        Func<SettingEventModel, Task<object>> settingEvent)
    {
        return settingClientDataContract.Verifications
            .Select(a => Convert(a, settingEvent))
            .ToList();
    }

    private SettingVerificationModel Convert(SettingVerificationDefinitionDataContract verification,
        Func<SettingEventModel, Task<object>> settingEvent)
    {
        return new SettingVerificationModel(settingEvent,
            verification.Name,
            verification.Description,
            verification.PropertyArguments);
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