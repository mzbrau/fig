using Fig.Contracts.CustomActions;
using Fig.Web.Facades;
using Fig.Web.Models.Clients;
using Fig.Web.Models.CustomActions;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Fig.Web.Pages.Setting
{
    public partial class CustomActionCard
    {
        private bool _showExpandIcon;
        private string _selectedInstance = "Auto";
        private bool _isExecuting;
        private bool _isLoadingHistory;
        private DateTime _historyStartTime = DateTime.Now.AddHours(-1);
        private DateTime _historyEndTime = DateTime.Now;
        private List<CustomActionExecutionStatusDataContract>? _historyExecutions;
        
        [Parameter]
        public CustomActionModel CustomAction { get; set; } = null!;

        [Parameter]
        public string ClientName { get; set; } = null!;
        
        [Inject]
        private ICustomActionFacade CustomActionFacade { get; set; } = null!;
        
        [Inject]
        private IClientStatusFacade ClientStatusFacade { get; set; } = null!;
        
        [Inject]
        private NotificationService NotificationService { get; set; } = null!;

        [Inject]
        private INotificationFactory NotificationFactory { get; set; } = null!;

        [Inject]
        private DialogService DialogService { get; set; } = null!;

        private List<string> AvailableInstances =>
        [
            "Auto", ..ClientStatusFacade.ClientRunSessions.Where(a => a.Name == ClientName).Select(a => a.RunSessionId.ToString())
        ];
        
        private ClientRunSessionModel? SelectedRunSession =>
            ClientStatusFacade.ClientRunSessions.FirstOrDefault(a => a.Name == ClientName && a.RunSessionId.ToString() == _selectedInstance);

        private bool HasAvailableRunningSessions =>
            ClientStatusFacade.ClientRunSessions.Any(a => a.Name == ClientName);

        private async Task ExecuteCustomAction()
        {
            try
            {
                _isExecuting = true;
                Guid? instance = _selectedInstance == "Auto" ? null : Guid.Parse(_selectedInstance);
                
                // Check if the selected session is running the latest settings
                bool showWarning = false;
                if (SelectedRunSession != null)
                {
                    // Specific session selected - check only that session
                    showWarning = !SelectedRunSession.RunningLatestSettings;
                }
                else
                {
                    // Auto selected (SelectedRunSession is null) - check if any session is out of date
                    var clientSessions = ClientStatusFacade.ClientRunSessions.Where(a => a.Name == ClientName).ToList();
                    showWarning = clientSessions.Any() && clientSessions.Any(session => !session.RunningLatestSettings);
                }
                
                if (showWarning)
                {
                    var shouldContinue = await ShowStaleSettingsWarning();
                    if (!shouldContinue)
                    {
                        // User chose to wait, just close the dialog
                        return;
                    }
                    // User chose to continue, proceed with execution
                }
                
                var response = await CustomActionFacade.RequestExecution(ClientName,
                    new CustomActionExecutionRequestDataContract(CustomAction.Name, instance));

                if (response is not null && response.ExecutionPending)
                    await PollExecutionStatus(response.ExecutionId);
                else if (response is not null && response.ExecutionPending == false)
                    throw new Exception(response.Message);
            }
            catch (Exception e)
            {
                NotificationService.Notify(NotificationFactory.Failure("Execution Failed",
                    e.Message));
            }
            finally
            {
                _isExecuting = false;
            }
        }
        
        private void ShowExpandIcon()
        {
            _showExpandIcon = true;
            StateHasChanged();
        }

        private void HideExpandIcon()
        {
            _showExpandIcon = false;
            StateHasChanged();
        }
        
        private void ToggleSettingCompactView()
        {
            CustomAction.ToggleCompactView();
        }
    
    private async Task PollExecutionStatus(Guid executionId)
    {
        try
        {
            var timeout = DateTime.UtcNow.AddMinutes(2); // 2 minute timeout
            
            while (DateTime.UtcNow < timeout)
            {
                await Task.Delay(1000);
                
                var status = await CustomActionFacade.GetExecutionStatus(executionId);
                if (status != null)
                {
                    CustomAction.ExecutionStatus = status;
                    await InvokeAsync(StateHasChanged);
                    
                    if (status.Status == ExecutionStatus.Completed)
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to poll execution status for {CustomAction.Name}: {ex.Message}");
        }
    }

        private string FormatColumnName(string columnName)
        {
            return System.Text.RegularExpressions.Regex.Replace(columnName, "([a-z])([A-Z])", "$1 $2");
        }

        private string FormatTimeOnly(DateTime? dateTime)
        {
            return dateTime?.ToString("HH:mm:ss") ?? "N/A";
        }

        private string CalculateDuration(DateTime? requestedAt, DateTime? executedAt)
        {
            if (requestedAt == null || executedAt == null)
                return "N/A";

            var duration = executedAt.Value - requestedAt.Value;
            
            if (duration.TotalSeconds < 1)
                return $"{duration.TotalMilliseconds:F0}ms";
            if (duration.TotalMinutes < 1)
                return $"{duration.TotalSeconds:F1}s";
            if (duration.TotalHours < 1)
                return $"{duration.TotalMinutes:F1}m";
            return $"{duration.TotalHours:F1}h";
        }

        private async Task RefreshHistory()
        {
            if (!CustomAction.IsHistoryVisible)
                return;
                
            _isLoadingHistory = true;
            try
            {
                var history = await CustomActionFacade.GetExecutionHistory(ClientName, CustomAction.Name, _historyStartTime, _historyEndTime);
                _historyExecutions = history?.Executions ?? new List<CustomActionExecutionStatusDataContract>();
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationFactory.Failure("History Load Failed", ex.Message));
                _historyExecutions = new List<CustomActionExecutionStatusDataContract>();
            }
            finally
            {
                _isLoadingHistory = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task OnHistoryTimeChanged()
        {
            // Ensure start time is always before end time
            if (_historyStartTime >= _historyEndTime)
            {
                _historyEndTime = _historyStartTime.AddHours(1);
            }
            
            if (CustomAction.IsHistoryVisible)
            {
                await RefreshHistory();
            }
        }

        private void HistoryDateRender(DateRenderEventArgs args)
        {
            // Disable future dates for history
            args.Disabled = args.Date.Date > DateTime.Now.Date;
        }

        private string FormatDateTime(DateTime? dateTime)
        {
            return dateTime?.ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss") ?? "N/A";
        }

        private async Task ToggleHistory()
        {
            CustomAction.ShowHistory();
            
            if (CustomAction.IsHistoryVisible)
            {
                _historyEndTime = DateTime.Now;
                _historyStartTime = _historyEndTime.AddHours(-1);
                await RefreshHistory();
            }
        }

        private async Task<bool> ShowStaleSettingsWarning()
        {
            var clientSessions = ClientStatusFacade.ClientRunSessions.Where(a => a.Name == ClientName).ToList();
            
            var parameters = new Dictionary<string, object>()
            {
                { "ClientSessions", clientSessions }
            };
            
            if (SelectedRunSession != null)
            {
                parameters.Add("SelectedRunSession", SelectedRunSession);
            }
            
            return await DialogService.OpenAsync<StaleSettingsWarningDialog>("Session Running Stale Settings",
                parameters,
                new DialogOptions 
                { 
                    CloseDialogOnOverlayClick = false,
                    CloseDialogOnEsc = true,
                    Width = "500px",
                    Style = "border-radius: 12px; background: linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 100%);",
                    CssClass = "modern-dialog"
                }) ?? false;
        }
    }
}
