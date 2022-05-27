using Fig.Contracts.Configuration;
using Fig.Web.Converters;
using Fig.Web.Models.Configuration;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Radzen;

namespace Fig.Web.Facades;

public class ConfigurationFacade : IConfigurationFacade
{
    private readonly IHttpService _httpService;
    private readonly IFigConfigurationConverter _figConfigurationConverter;
    private readonly NotificationService _notificationService;
    private readonly INotificationFactory _notificationFactory;
    private FigConfigurationModel _lastSavedModel = new();

    public ConfigurationFacade(IHttpService httpService, IFigConfigurationConverter figConfigurationConverter, NotificationService notificationService, INotificationFactory notificationFactory)
    {
        _httpService = httpService;
        _figConfigurationConverter = figConfigurationConverter;
        _notificationService = notificationService;
        _notificationFactory = notificationFactory;
    }

    public FigConfigurationModel ConfigurationModel { get; private set; } = new();
    
    public async Task LoadConfiguration()
    {
        var result = await _httpService.Get<FigConfigurationDataContract>("configuration");

        if (result == null)
            return;

        ConfigurationModel = _figConfigurationConverter.Convert(result);
        _lastSavedModel = ConfigurationModel.Clone();
    }

    public async Task SaveConfiguration()
    {
        var dataContract = _figConfigurationConverter.Convert(ConfigurationModel);

        try
        {
            await _httpService.Put<FigConfigurationDataContract>("configuration", dataContract);
            _lastSavedModel = ConfigurationModel.Clone();
            _notificationService.Notify(_notificationFactory.Success("Success", "Configuration Updated Successfully"));
        }
        catch (Exception e)
        {
            RevertChange();
            _notificationService.Notify(_notificationFactory.Failure("Failure", $"Failed to update configuration: {e.Message}"));
        }
    }

    private void RevertChange()
    {
        ConfigurationModel.Revert(_lastSavedModel);
    }
}