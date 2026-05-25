using System.Diagnostics;
using Fig.Web.Facades;
using Fig.Web.Models.Configuration;
using Fig.Web.Notifications;
using Fig.Web.Pages.Dialogs;
using Fig.Web.ReleaseHighlights;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Fig.Web.Pages
{
    public partial class Configuration : IDisposable
    {
        private const string MigrationInProgressStatus = "MigrationInProgress";
        private const string PreviousSecretReminder =
            "Remove PreviousSecret after confirming all API hosts use the new secret. Users who logged in before the API secret change will be logged out when PreviousSecret is removed.";
        private bool _isMigrationInProgress;
        private bool _isTestingAzureKeyVault;
        private bool _timeMachineCleanupEnabled;
        private bool _eventLogsCleanupEnabled;
        private bool _apiStatusCleanupEnabled;
        private bool _settingHistoryCleanupEnabled;
        private CancellationTokenSource? _migrationPollingCancellation;
        private Task? _migrationPollingTask;

        [Inject]
        private IConfigurationFacade ConfigurationFacade { get; set; } = null!;

        [Inject]
        private NotificationService NotificationService { get; set; } = null!;

        [Inject]
        private INotificationFactory NotificationFactory { get; set; } = null!;

        [Inject]
        private DialogService DialogService { get; set; } = null!;

        [Inject]
        private IReleaseHighlightsCoordinator ReleaseHighlightsCoordinator { get; set; } = null!;

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

            if (IsMigrationStatusInProgress())
            {
                _isMigrationInProgress = true;
                StartMigrationStatusPolling();
            }
        }

        private void OnConfigurationValueChanged()
        {
            if (ConfigurationModel.PollIntervalOverride < 2000)
                ConfigurationModel.PollIntervalOverride = 2000;

            ConfigurationFacade.SaveConfiguration();
        }

        private async Task MigrateEncryptedData()
        {
            if (_isMigrationInProgress)
                return;

            _isMigrationInProgress = true;
            Task? migrationTask = null;
            try
            {
                var watch = Stopwatch.StartNew();
                migrationTask = ConfigurationFacade.MigrateEncryptedData();
                StartMigrationStatusPolling(migrationTask);
                await migrationTask;
                await ConfigurationFacade.RefreshApiSecretRotationStatus();
                NotificationService.Notify(NotificationFactory.Success("Migration Complete", $"Completed in {watch.ElapsedMilliseconds.Milliseconds()}. {PreviousSecretReminder}"));
            }
            catch (Exception ex)
            {
                await ConfigurationFacade.RefreshApiSecretRotationStatus();
                NotificationService.Notify(NotificationFactory.Failure("Migration Failed", ex.Message));
            }
            finally
            {
                _isMigrationInProgress = IsMigrationStatusInProgress() || migrationTask?.IsCompleted == false;
                if (!_isMigrationInProgress)
                    StopMigrationStatusPolling();

                StateHasChanged();
            }
        }

        private void StartMigrationStatusPolling(Task? migrationTask = null)
        {
            if (_migrationPollingTask?.IsCompleted == false)
                return;

            _migrationPollingCancellation = new CancellationTokenSource();
            _migrationPollingTask = PollMigrationStatus(migrationTask, _migrationPollingCancellation.Token);
        }

        private async Task PollMigrationStatus(Task? migrationTask, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await ConfigurationFacade.RefreshApiSecretRotationStatus();
                    _isMigrationInProgress = IsMigrationStatusInProgress() || migrationTask?.IsCompleted == false;
                    await InvokeAsync(StateHasChanged);

                    if (!_isMigrationInProgress)
                        return;

                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private bool IsMigrationStatusInProgress()
        {
            return string.Equals(
                ConfigurationFacade.ApiSecretRotationStatus?.Status,
                MigrationInProgressStatus,
                StringComparison.Ordinal);
        }

        private void StopMigrationStatusPolling()
        {
            _migrationPollingCancellation?.Cancel();
            _migrationPollingCancellation?.Dispose();
            _migrationPollingCancellation = null;
            _migrationPollingTask = null;
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

        private async Task ShowReleaseHighlights()
        {
            var dialogRequest = await ReleaseHighlightsCoordinator.GetManualRecallDialog();
            if (dialogRequest == null)
                return;
            await DialogService.OpenAsync<ReleaseHighlightsDialog>(
                "What's New",
                new Dictionary<string, object> { ["Request"] = dialogRequest },
                ReleaseHighlightsDialogOptionsFactory.Create());
        }

        public void Dispose()
        {
            StopMigrationStatusPolling();
        }
    }
}
