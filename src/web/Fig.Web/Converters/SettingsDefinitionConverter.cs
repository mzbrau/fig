using Fig.Contracts;
using Fig.Contracts.Authentication;
using Fig.Contracts.CustomActions;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;
using Fig.Web.Models.CustomActions;
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
        IList<SettingsClientDefinitionDataContract> settingDataContracts,
        Action<(string, double)> reportProgress)
    {
        var result = new List<SettingClientConfigurationModel>();

        var totalSettings = (double)settingDataContracts.Sum(a => a.Settings.Count);
        double loadedSettings = 0;
        foreach (var contract in settingDataContracts)
        {
            try
            {
                reportProgress((contract.Name, 100 / totalSettings * loadedSettings));
                result.Add(Convert(contract));
                loadedSettings += contract.Settings.Count;
                await Task.Delay(20); // Required for the UI to update as Blazor is single threaded. https://github.com/dotnet/aspnetcore/issues/14253
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _notificationService.Notify(_notificationFactory.Warning("Client Load Failed", $"Failed to load Client {contract.Name}. {e.Message}"));
            }
        }

        return result;
    }

    private SettingClientConfigurationModel Convert(SettingsClientDefinitionDataContract settingClientDataContract)
    {
        var model = new SettingClientConfigurationModel(settingClientDataContract.Name, 
            settingClientDataContract.Instance,
            settingClientDataContract.HasDisplayScripts,
            _scriptRunner);

        model.Settings = new List<ISetting>();

        foreach (var setting in settingClientDataContract.Settings)
        {
            try
            {
                model.Settings.Add(Convert(setting, model));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _notificationService.Notify(_notificationFactory.Warning("Setting Load Failed", $"Failed to load Setting {setting.Name} for client {model.Name}. {e.Message}"));
            }
        }
        
        model.CustomActions = ConvertCustomActions(settingClientDataContract, model.SettingEvent);
        return model;
    }

    private List<CustomActionModel> ConvertCustomActions(SettingsClientDefinitionDataContract settingClientDataContract, Func<SettingEventModel, Task<object>> settingEvent)
    {
        return settingClientDataContract.CustomActions
            .Select(Convert)
            .ToList();
    }
    
    private CustomActionModel Convert(CustomActionDefinitionDataContract customAction)
    {
        return new CustomActionModel(customAction);
    }

    private ISetting Convert(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
    {
        var isReadOnly = _accountService.AuthenticatedUser?.Role == Role.ReadOnly;
        var presentation = new SettingPresentation(isReadOnly);
        
        return dataContract.ValueType.FigPropertyType() switch
        {
            FigPropertyType.String when dataContract.ValidValues != null => new DropDownSettingConfigurationModel(
                dataContract,
                parent, presentation),
            FigPropertyType.String when dataContract.JsonSchema != null => new JsonSettingConfigurationModel(
                dataContract, parent, presentation),
            FigPropertyType.String => new StringSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.Int => new IntSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.Long => new LongSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.Double => new DoubleSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.Bool => new BoolSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.DataGrid => new DataGridSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.DateTime => new DateTimeSettingConfigurationModel(dataContract, parent, presentation),
            FigPropertyType.TimeSpan => new TimeSpanSettingConfigurationModel(dataContract, parent, presentation),
            _ => new UnknownSettingTypeConfigurationModel(dataContract,
                parent, presentation) // TODO: In the future, this should throw an exception
        };
    }
}