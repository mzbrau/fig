using Fig.Contracts.ApiSecret;
using Fig.Contracts.Configuration;
using Fig.Contracts.EventHistory;
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

    public long EventLogCount { get; private set; }

    public ApiSecretRotationStatusDataContract? ApiSecretRotationStatus { get; private set; }

    public bool WebFeaturesLoaded { get; private set; }

    public bool AllowDisplayScripts { get; private set; }

    public event Action? WebFeaturesChanged;

    public async Task LoadConfiguration()
    {
        var result = await _httpService.Get<FigConfigurationDataContract>("configuration");

        if (result == null)
            return;

        ConfigurationModel = _figConfigurationConverter.Convert(result);
        _lastSavedModel = ConfigurationModel.Clone();
        SetAllowDisplayScripts(ConfigurationModel.AllowDisplayScripts);

        EventLogCount = (await _httpService.Get<EventLogCountDataContract>("events/count"))?.EventLogCount ?? 0;
        await RefreshApiSecretRotationStatus();
    }

    public async Task LoadWebFeatures()
    {
        var result = await _httpService.Get<FigWebFeaturesDataContract>("configuration/features", false);
        if (result == null)
            return;

        SetAllowDisplayScripts(result.AllowDisplayScripts);
    }

    public async Task SaveConfiguration()
    {
        var dataContract = _figConfigurationConverter.Convert(ConfigurationModel);

        try
        {
            await _httpService.Put<FigConfigurationDataContract>("configuration", dataContract);
            _lastSavedModel = ConfigurationModel.Clone();
            SetAllowDisplayScripts(ConfigurationModel.AllowDisplayScripts);
            _notificationService.Notify(_notificationFactory.Success("Success", "Configuration Updated Successfully"));
        }
        catch (Exception e)
        {
            RevertChange();
            _notificationService.Notify(_notificationFactory.Failure("Failure", $"Failed to update configuration: {e.Message}"));
        }
    }

    public async Task EnableDisplayScripts()
    {
        await LoadConfiguration();
        ConfigurationModel.AllowDisplayScripts = true;
        await SaveConfiguration();
        await LoadWebFeatures();
    }

    public async Task MigrateEncryptedData()
    {
        await _httpService.PutOrThrow("encryptionmigration", null, 3600);
        await RefreshApiSecretRotationStatus();
    }

    public async Task RefreshApiSecretRotationStatus()
    {
        ApiSecretRotationStatus = await _httpService.Get<ApiSecretRotationStatusDataContract>("encryptionmigration/status", false);
    }

    public async Task<SecretStoreTestResultDataContract> TestKeyVault()
    {
        return await _httpService.Put<SecretStoreTestResultDataContract>("configuration/KeyVault", null) ?? new SecretStoreTestResultDataContract(false, "No response received");
    }

    public async Task<SecretStoreTestResultDataContract> TestFigAssistant()
    {
        return await _httpService.Put<SecretStoreTestResultDataContract>("configuration/Assistant", null)
               ?? new SecretStoreTestResultDataContract(false, "No response received");
    }

    private void RevertChange()
    {
        ConfigurationModel.Revert(_lastSavedModel);
    }

    private void SetAllowDisplayScripts(bool allowDisplayScripts)
    {
        var changed = !WebFeaturesLoaded || AllowDisplayScripts != allowDisplayScripts;
        AllowDisplayScripts = allowDisplayScripts;
        WebFeaturesLoaded = true;
        if (changed)
            WebFeaturesChanged?.Invoke();
    }
}