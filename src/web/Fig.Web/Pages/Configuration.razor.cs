using System.Diagnostics;
using Fig.Web.Facades;
using Fig.Web.Models.Configuration;
using Fig.Web.Notifications;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Fig.Web.Pages
{
    public partial class Configuration
    {
        private bool _isMigrationInProgress;
        private bool _isTestingAzureKeyVault;
        private bool _timeMachineCleanupEnabled;
        private bool _eventLogsCleanupEnabled;
        private bool _apiStatusCleanupEnabled;
        private bool _settingHistoryCleanupEnabled;
        
        [Inject]
        private IConfigurationFacade ConfigurationFacade { get; set; } = null!;
        
        [Inject]
        private NotificationService NotificationService { get; set; } = null!;

        [Inject]
        private INotificationFactory NotificationFactory { get; set; } = null!;

        private FigConfigurationModel ConfigurationModel => ConfigurationFacade.ConfigurationModel;

        protected override async Task OnInitializedAsync()
        {
            await ConfigurationFacade.LoadConfiguration();
            
            // Initialize checkbox states based on whether cleanup days have values
            _timeMachineCleanupEnabled = ConfigurationModel.TimeMachineCleanupDays is > 0;
            _eventLogsCleanupEnabled = ConfigurationModel.EventLogsCleanupDays is > 0;
            _apiStatusCleanupEnabled = ConfigurationModel.ApiStatusCleanupDays is > 0;
            _settingHistoryCleanupEnabled = ConfigurationModel.SettingHistoryCleanupDays is > 0;
            
            await base.OnInitializedAsync();
        }

        private void OnConfigurationValueChanged()
        {
            if (ConfigurationModel.PollIntervalOverride < 2000)
                ConfigurationModel.PollIntervalOverride = 2000;
            
            ConfigurationFacade.SaveConfiguration();
        }

        private async Task MigrateEncryptedData()
        {
            _isMigrationInProgress = true;
            try
            {
                var watch = Stopwatch.StartNew();
                await ConfigurationFacade.MigrateEncryptedData();
                NotificationService.Notify(NotificationFactory.Success("Migration Complete", $"Completed in {watch.ElapsedMilliseconds.Milliseconds()}"));
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationFactory.Failure("Migration Failed", ex.Message));
            }
            finally
            {
                _isMigrationInProgress = false;
            }
        }

        private async Task TestKeyVault()
        {
            _isTestingAzureKeyVault = true;
            try
            {
                var result = await ConfigurationFacade.TestKeyVault();
                NotificationService.Notify(result.Success
                    ? NotificationFactory.Success("Test Succeeded", "Successfully connected to Azure Key Vault")
                    : NotificationFactory.Failure("Test Failed", result.Message));
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationFactory.Failure("Test Failed", ex.Message));
            }
            finally
            {
                _isTestingAzureKeyVault = false;
            }
        }
        
        private void OnTimeMachineCleanupEnabledChanged(bool enabled)
        {
            if (!enabled)
            {
                ConfigurationModel.TimeMachineCleanupDays = null;
            }
            else if (!ConfigurationModel.TimeMachineCleanupDays.HasValue)
            {
                ConfigurationModel.TimeMachineCleanupDays = 90;
            }
            OnConfigurationValueChanged();
        }
        
        private void OnEventLogsCleanupEnabledChanged(bool enabled)
        {
            if (!enabled)
            {
                ConfigurationModel.EventLogsCleanupDays = null;
            }
            else if (!ConfigurationModel.EventLogsCleanupDays.HasValue)
            {
                ConfigurationModel.EventLogsCleanupDays = 90;
            }
            OnConfigurationValueChanged();
        }
        
        private void OnApiStatusCleanupEnabledChanged(bool enabled)
        {
            if (!enabled)
            {
                ConfigurationModel.ApiStatusCleanupDays = null;
            }
            else if (!ConfigurationModel.ApiStatusCleanupDays.HasValue)
            {
                ConfigurationModel.ApiStatusCleanupDays = 90;
            }
            OnConfigurationValueChanged();
        }
        
        private void OnSettingHistoryCleanupEnabledChanged(bool enabled)
        {
            if (!enabled)
            {
                ConfigurationModel.SettingHistoryCleanupDays = null;
            }
            else if (!ConfigurationModel.SettingHistoryCleanupDays.HasValue)
            {
                ConfigurationModel.SettingHistoryCleanupDays = 90;
            }
            OnConfigurationValueChanged();
        }
    }
}
