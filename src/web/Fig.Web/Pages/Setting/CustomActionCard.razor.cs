using Fig.Common.Events;
using Fig.Contracts.CustomActions;
using Fig.Web.Events;
using Fig.Web.Facades;
using Fig.Web.Models.Setting;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Pages.Setting
{
    public partial class CustomActionCard
    {
        private bool _showExpandIcon;
        private string _selectedInstance = "Auto";
        private bool _isExecuting = false;
        
        [Parameter]
        public CustomActionModel CustomAction { get; set; } = null!;

        [Parameter]
        public string ClientName { get; set; } = null!;
        
        [Inject]
        private ICustomActionFacade CustomActionFacade { get; set; } = null!;
        
        [Inject]
        private IClientStatusFacade ClientStatusFacade { get; set; } = null!;

        private List<string> AvailableInstances =>
        [
            "Auto", ..ClientStatusFacade.ClientRunSessions.Where(a => a.Name == ClientName).Select(a => a.RunSessionId.ToString())
        ];

        private async Task ExecuteCustomAction()
        {
            try
            {
                _isExecuting = true;
                Guid? instance = _selectedInstance == "Auto" ? null : Guid.Parse(_selectedInstance);
                var response = await CustomActionFacade.RequestExecution(ClientName,
                    new CustomActionExecutionRequestDataContract(CustomAction.Name, instance));

                if (response is not null)
                    await PollExecutionStatus(response!.ExecutionId);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to execute" + e);
                throw;
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
                await Task.Delay(5000);
                
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
    }
}
