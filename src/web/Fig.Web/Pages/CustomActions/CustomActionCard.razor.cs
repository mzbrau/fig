using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Web.Facades;
using Fig.Web.Models.CustomActions;
using Fig.Web.Models.Setting;
using Fig.Web.Utils; // For Modal
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Pages.CustomActions
{
    public partial class CustomActionCard
    {
        private bool _isMounted;
        private CancellationTokenSource? _cts;
        private bool ShowLastExecutionResults { get; set; }
        private Modal? HistoryModal { get; set; }
        private CustomActionDefinitionModel? _selectedActionForHistory;

        [Parameter]
        public CustomActionDefinitionModel Action { get; set; } = null!;

        [Parameter]
        public string ClientName { get; set; } = null!; // Needed for setting input components

        protected override async Task OnInitializedAsync()
        {
            _isMounted = true;
            await RestoreActionState(); // Restore state when component is initialized
        }

        protected override async Task OnParametersSetAsync()
        {
            // If the action has an ongoing execution from a previous state/navigation, continue polling
            if (Action.IsExecuting && Action.LastExecution?.ExecutionId != Guid.Empty)
            {
                await PollForStatus(Action.LastExecution.ExecutionId);
            }
        }

        private string GetCardHeaderClass()
        {
            var baseClass = "card-header";
            if (Action.LastExecution == null || !Action.LastExecution.IsCompleted) return baseClass;
            return Action.LastExecution.IsFailed ? $"{baseClass} bg-danger text-white" : $"{baseClass} bg-success text-white";
        }

        private async Task SettingValueChanged(SettingConfigurationModel setting, object? newValue)
        {
            // This method is called by the SettingInput component when a value changes.
            // The 'setting.Value' is already bound and updated by the SettingInput component.
            // We just need to trigger a state change if necessary, or perform validation.
            setting.Value = newValue; // Ensure our model is updated
            await SaveActionState(); // Save state whenever a setting changes
            StateHasChanged();
        }
        
        private async Task ExecuteAction()
        {
            Action.IsExecuting = true;
            ShowLastExecutionResults = false; // Hide previous results
            Action.LastExecution = null; // Clear previous execution details
            await SaveActionState(); // Save state before execution
            StateHasChanged();

            var settingsContracts = Action.SettingsUsed
                .Select(s => new SettingDataContract(s.Name, s.GetValueDataContract(), s.ParentName))
                .ToList();

            var request = new CustomActionExecutionRequestModel(
                Action.OriginalContract.Id, // Assuming OriginalContract.Id is the CustomActionId
                Action.SelectedInstance == "auto" ? null : Action.SelectedInstance,
                settingsContracts.Any() ? settingsContracts : null
            );

            var response = await CustomActionsFacade.RequestExecution(ClientName, request.ToDataContract(), CancellationToken.None);

            if (response.ExecutionId != Guid.Empty)
            {
                Action.LastExecution = new CustomActionExecutionStatusModel(response.ExecutionId, "Executing");
                await SaveActionState(); // Save new execution state
                await PollForStatus(response.ExecutionId);
            }
            else
            {
                Action.IsExecuting = false;
                Action.LastExecution = new CustomActionExecutionStatusModel(Guid.Empty, "Failed") // Placeholder for failed request
                {
                    ErrorMessage = response.Message ?? "Execution request failed. No Execution ID returned."
                };
                await SaveActionState(); // Save failed state
            }
            StateHasChanged();
        }

        private async Task PollForStatus(Guid executionId)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            while (!token.IsCancellationRequested && Action.IsExecuting && _isMounted)
            {
                var status = await CustomActionsFacade.GetExecutionStatus(executionId, token);
                if (status != null)
                {
                    Action.LastExecution = new CustomActionExecutionStatusModel(status);
                    if (!Action.LastExecution.IsInProgress)
                    {
                        Action.IsExecuting = false;
                        ShowLastExecutionResults = Action.LastExecution.IsCompleted || Action.LastExecution.IsFailed;
                        await SaveActionState(); // Save final state
                        StateHasChanged();
                        break;
                    }
                }
                else // Status is null, could mean an error or not found
                {
                    Action.IsExecuting = false;
                    Action.LastExecution = new CustomActionExecutionStatusModel(executionId, "Failed")
                    {
                        ErrorMessage = "Failed to retrieve execution status."
                    };
                    await SaveActionState(); // Save error state
                    StateHasChanged();
                    break;
                }
                
                await Task.Delay(1000, token); // Poll every second
                if (!_isMounted) break; // Stop polling if component is unmounted
                StateHasChanged(); // Update UI with current status
            }
        }
        
        private void ToggleLastExecutionResults()
        {
            ShowLastExecutionResults = !ShowLastExecutionResults;
        }

        private async Task ViewHistory()
        {
            _selectedActionForHistory = Action;
            if (HistoryModal != null)
            {
                await HistoryModal.Show();
            }
        }

        // State Persistence using LocalStorage
        private string GetLocalStorageKey() => $"customActionState_{ClientName}_{Action.Name}";

        private async Task SaveActionState()
        {
            if (LocalStorage != null)
            {
                var state = new CustomActionCardState
                {
                    SelectedInstance = Action.SelectedInstance,
                    SettingsValues = Action.SettingsUsed.ToDictionary(s => s.Name, s => s.Value),
                    IsExecuting = Action.IsExecuting,
                    LastExecution = Action.LastExecution != null ? new CustomActionExecutionStatusModel(Action.LastExecution.ExecutionId, Action.LastExecution.Status) // Simplified for storage
                    {
                        RequestedAt = Action.LastExecution.RequestedAt, // Persist times for context
                        ExecutedAt = Action.LastExecution.ExecutedAt,
                        CompletedAt = Action.LastExecution.CompletedAt,
                        ErrorMessage = Action.LastExecution.ErrorMessage,
                        // Results are not persisted to keep state light, they are fetched on demand or if status shows completion
                    } : null,
                    ShowLastExecutionResults = ShowLastExecutionResults
                };
                await LocalStorage.SetItemAsync(GetLocalStorageKey(), state);
            }
        }

        private async Task RestoreActionState()
        {
            if (LocalStorage != null)
            {
                try
                {
                    var state = await LocalStorage.GetItemAsync<CustomActionCardState>(GetLocalStorageKey());
                    if (state != null)
                    {
                        Action.SelectedInstance = state.SelectedInstance;
                        foreach (var settingModel in Action.SettingsUsed)
                        {
                            if (state.SettingsValues.TryGetValue(settingModel.Name, out var value))
                            {
                                settingModel.Value = value;
                            }
                        }
                        Action.IsExecuting = state.IsExecuting;
                        Action.LastExecution = state.LastExecution; // Restore simplified status
                        ShowLastExecutionResults = state.ShowLastExecutionResults;

                        // If was executing, immediately fetch full status and potentially repoll
                        if (Action.IsExecuting && Action.LastExecution?.ExecutionId != Guid.Empty)
                        {
                            var currentStatus = await CustomActionsFacade.GetExecutionStatus(Action.LastExecution.ExecutionId, CancellationToken.None);
                            if (currentStatus != null)
                            {
                                Action.LastExecution = new CustomActionExecutionStatusModel(currentStatus);
                                if (!Action.LastExecution.IsInProgress) Action.IsExecuting = false; // Update IsExecuting based on fresh status
                            }
                            else // Status couldn't be fetched, assume it's no longer executing
                            {
                                Action.IsExecuting = false;
                            }
                            await SaveActionState(); // Re-save potentially updated IsExecuting state
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error restoring state for {Action.Name}: {ex.Message}");
                    // Optionally clear invalid state
                    await LocalStorage.RemoveItemAsync(GetLocalStorageKey());
                }
            }
        }
        
        public void Dispose()
        {
            _isMounted = false;
            _cts?.Cancel();
            _cts?.Dispose();
        }

        // Helper class for state persistence
        private class CustomActionCardState
        {
            public string? SelectedInstance { get; set; }
            public Dictionary<string, object?> SettingsValues { get; set; } = new();
            public bool IsExecuting { get; set; }
            public CustomActionExecutionStatusModel? LastExecution { get; set; }
            public bool ShowLastExecutionResults { get; set; }
        }
    }
}
