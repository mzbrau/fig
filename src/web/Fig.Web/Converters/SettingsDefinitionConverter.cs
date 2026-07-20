using System.Diagnostics;
using Fig.Common.NetStandard.Scripting;
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
using Fig.Web.Services;
using Microsoft.Extensions.Options;
using Radzen;

namespace Fig.Web.Converters;

public class SettingsDefinitionConverter : ISettingsDefinitionConverter
{
    private long _modelBuildElapsedMs;

    private readonly IAccountService _accountService;
    private readonly IScriptRunner _scriptRunner;
    private readonly NotificationService _notificationService;
    private readonly INotificationFactory _notificationFactory;
    private readonly WebSettings _webSettings;
    private readonly IDisplayScriptStatusService _displayScriptStatusService;

    public SettingsDefinitionConverter(
        IAccountService accountService, 
        IScriptRunner scriptRunner,
        NotificationService notificationService,
        INotificationFactory notificationFactory,
        IOptions<WebSettings> webSettings,
        IDisplayScriptStatusService displayScriptStatusService)
    {
        _accountService = accountService;
        _scriptRunner = scriptRunner;
        _notificationService = notificationService;
        _notificationFactory = notificationFactory;
        _webSettings = webSettings.Value;
        _displayScriptStatusService = displayScriptStatusService;
    }

    public void ResetModelBuildTiming() =>
        Interlocked.Exchange(ref _modelBuildElapsedMs, 0);

    public long TakeModelBuildElapsedMs() =>
        Interlocked.Exchange(ref _modelBuildElapsedMs, 0);
    
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
                var buildWatch = Stopwatch.StartNew();
                result.Add(Convert(contract));
                Interlocked.Add(ref _modelBuildElapsedMs, buildWatch.ElapsedMilliseconds);
                loadedSettings += contract.Settings.Count;
                // Yield so Blazor can paint progress (keep Task.Yield, not Delay).
                await Task.Yield();
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
            "Loading...",
            settingClientDataContract.Instance,
            settingClientDataContract.HasDisplayScripts,
            _scriptRunner,
            displayScriptStatusService: _displayScriptStatusService)
        {
            Settings = []
        };

        foreach (var setting in settingClientDataContract.Settings)
        {
            try
            {
                var settingModel = Convert(setting, model);
                settingModel.IsCompactView = _webSettings.DefaultDisplayCollapsed;
                model.Settings.Add(settingModel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _notificationService.Notify(_notificationFactory.Warning("Setting Load Failed", $"Failed to load Setting {setting.Name} for client {model.Name}. {e.Message}"));
            }
        }
        
        model.CustomActions = ConvertCustomActions(settingClientDataContract, model.SettingEvent);
        foreach (var customAction in model.CustomActions)
        {
            customAction.IsCompactView = _webSettings.DefaultDisplayCollapsed;
        }
        
        model.MigrateFromSettingCount = settingClientDataContract.Settings
            .Count(s => !string.IsNullOrWhiteSpace(s.MigrateFrom));
        
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