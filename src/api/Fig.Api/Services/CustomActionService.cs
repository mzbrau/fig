using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Datalayer.Repositories.CustomActions;
using Fig.Api.Exceptions;
using Fig.Contracts.CustomActions;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities.CustomActions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Api.Services
{
    public class CustomActionService : AuthenticatedService, ICustomActionService
    {
        private readonly ICustomActionRepository _customActionRepository;
        private readonly ICustomActionExecutionRepository _customActionExecutionRepository;
        private readonly ICustomActionExecutionResultRepository _customActionExecutionResultRepository;
        private readonly ISettingClientRepository _settingClientRepository;
        private readonly IEventLogFactory _eventLogFactory;
        private readonly IFigConfigurationConverter _figConfigurationConverter; // Assuming this can handle SettingDefinitionDataContract
        private readonly IValueToStringConverter _valueToStringConverter; // For converting setting values if needed, though maybe not directly here.
        private readonly ILogger<CustomActionService> _logger;

        public CustomActionService(
            ICustomActionRepository customActionRepository,
            ICustomActionExecutionRepository customActionExecutionRepository,
            ICustomActionExecutionResultRepository customActionExecutionResultRepository,
            ISettingClientRepository settingClientRepository,
            IEventLogFactory eventLogFactory,
            IFigConfigurationConverter figConfigurationConverter,
            IValueToStringConverter valueToStringConverter,
            ILogger<CustomActionService> logger,
            IAuthenticatedServiceRequests authenticatedServiceRequests) : base(authenticatedServiceRequests)
        {
            _customActionRepository = customActionRepository;
            _customActionExecutionRepository = customActionExecutionRepository;
            _customActionExecutionResultRepository = customActionExecutionResultRepository;
            _settingClientRepository = settingClientRepository;
            _eventLogFactory = eventLogFactory;
            _figConfigurationConverter = figConfigurationConverter;
            _valueToStringConverter = valueToStringConverter;
            _logger = logger;
        }

        public async Task RegisterCustomActions(CustomActionRegistrationRequestDataContract request, CancellationToken cancellationToken)
        {
            var client = await _settingClientRepository.GetClient(request.ClientName, request.Instance, cancellationToken)
                         ?? throw new UnknownClientException(request.ClientName, request.Instance);

            // Remove existing custom actions for this client that are not in the new request
            var existingActions = _customActionRepository.GetByClientId(client.Id).ToList();
            var actionsToRemove = existingActions.Where(ea => request.CustomActions.All(a => a.Name != ea.Name)).ToList();
            foreach (var actionToRemove in actionsToRemove)
            {
                _customActionRepository.Delete(actionToRemove); // NHibernate cascade should handle children
                _logger.LogInformation("Removed outdated custom action '{ActionName}' for client {ClientName} ({Instance})",
                    actionToRemove.Name, client.Name, client.Instance);
            }

            foreach (var actionContract in request.CustomActions)
            {
                var existingAction = existingActions.FirstOrDefault(a => a.Name == actionContract.Name && a.SettingClientId == client.Id);
                if (existingAction != null)
                {
                    // Update existing action
                    existingAction.ButtonName = actionContract.ButtonName;
                    existingAction.Description = actionContract.Description;
                    existingAction.SettingsUsedJson = actionContract.SettingsUsed != null && actionContract.SettingsUsed.Any()
                        ? JsonConvert.SerializeObject(actionContract.SettingsUsed)
                        : null;
                    _customActionRepository.Update(existingAction);
                    _logger.LogInformation("Updated custom action '{ActionName}' for client {ClientName} ({Instance})",
                        existingAction.Name, client.Name, client.Instance);
                }
                else
                {
                    // Add new action
                    var newAction = new CustomActionBusinessEntity
                    {
                        Id = Guid.NewGuid(),
                        Name = actionContract.Name,
                        ButtonName = actionContract.ButtonName,
                        Description = actionContract.Description,
                        SettingsUsedJson = actionContract.SettingsUsed != null && actionContract.SettingsUsed.Any()
                            ? JsonConvert.SerializeObject(actionContract.SettingsUsed)
                            : null,
                        SettingClientId = client.Id
                    };
                    _customActionRepository.Add(newAction);
                    _logger.LogInformation("Registered new custom action '{ActionName}' for client {ClientName} ({Instance})",
                        newAction.Name, client.Name, client.Instance);
                }
            }
            
            await _eventLogFactory.Create(client.Id, $"Registered/Updated {request.CustomActions.Count} custom actions.", GetCurrentUser(), cancellationToken);
        }

        public async Task<IEnumerable<CustomActionDefinitionDataContract>> GetCustomActions(string clientName, string? instance, CancellationToken cancellationToken)
        {
            var client = await _settingClientRepository.GetClient(clientName, instance, cancellationToken)
                         ?? throw new UnknownClientException(clientName, instance);

            // CustomActions are loaded with the client due to mapping, or fetch explicitly if needed
            var actions = client.CustomActions ?? _customActionRepository.GetByClientId(client.Id);

            return actions.Select(a => new CustomActionDefinitionDataContract
            {
                Name = a.Name,
                ButtonName = a.ButtonName,
                Description = a.Description,
                SettingsUsed = !string.IsNullOrWhiteSpace(a.SettingsUsedJson)
                    ? JsonConvert.DeserializeObject<List<SettingDefinitionDataContract>>(a.SettingsUsedJson)
                    : new List<SettingDefinitionDataContract>()
            }).ToList();
        }

        public async Task<CustomActionExecutionResponseDataContract> RequestExecution(string clientName, CustomActionExecutionRequestDataContract request, CancellationToken cancellationToken)
        {
            var customAction = _customActionRepository.GetById(request.CustomActionId)
                               ?? throw new InvalidOperationException($"Custom Action with ID {request.CustomActionId} not found.");

            // Potentially validate clientName matches the client associated with customAction.SettingClientId
            // For now, assuming request.CustomActionId is the source of truth.

            var execution = new CustomActionExecutionBusinessEntity
            {
                Id = Guid.NewGuid(),
                CustomActionId = request.CustomActionId,
                Instance = request.Instance, // Instance on which to execute
                SettingsJson = request.Settings != null && request.Settings.Any()
                    ? JsonConvert.SerializeObject(request.Settings)
                    : null,
                RequestedAt = DateTime.UtcNow,
                Status = "Pending" // Initial status
            };

            _customActionExecutionRepository.Add(execution);
            _logger.LogInformation("Requested execution for custom action '{ActionName}' (ID: {ActionId}), Execution ID: {ExecutionId}",
                customAction.Name, customAction.Id, execution.Id);
            
            // Find the client associated with the custom action to log against it.
            var client = _settingClientRepository.GetClient(customAction.SettingClientId);
            if (client != null)
            {
                 await _eventLogFactory.Create(client.Id, $"Custom action '{customAction.Name}' requested by user {GetCurrentUser()?.Username}.", GetCurrentUser(), cancellationToken);
            }


            return new CustomActionExecutionResponseDataContract
            {
                ExecutionId = execution.Id,
                Message = "Execution requested successfully."
            };
        }

        public Task<IEnumerable<CustomActionClientPollResponseDataContract>> PollForExecutionRequests(string clientName, string? instance, CancellationToken cancellationToken)
        {
            var pendingExecutions = _customActionRepository.GetAllPending(clientName, instance).ToList();
            var responseList = new List<CustomActionClientPollResponseDataContract>();

            if (!pendingExecutions.Any())
            {
                _logger.LogDebug("No pending custom action executions for client {ClientName}, instance {Instance}", clientName, instance ?? "N/A");
                return Task.FromResult(Enumerable.Empty<CustomActionClientPollResponseDataContract>());
            }

            foreach (var execution in pendingExecutions)
            {
                execution.Status = "Executing"; // Mark as executing
                _customActionExecutionRepository.Update(execution); // Persist status change

                responseList.Add(new CustomActionClientPollResponseDataContract
                {
                    ExecutionId = execution.Id,
                    CustomActionId = execution.CustomActionId, // Client might not need this if it has ExecutionId
                    ActionName = execution.CustomAction.Name, // Helpful for client logging
                    Settings = !string.IsNullOrWhiteSpace(execution.SettingsJson)
                        ? JsonConvert.DeserializeObject<List<SettingDataContract>>(execution.SettingsJson)
                        : null
                });
                _logger.LogInformation("Client {ClientName} ({Instance}) polled and received execution request {ExecutionId} for action '{ActionName}'",
                    clientName, instance ?? "N/A", execution.Id, execution.CustomAction.Name);
            }
            
            return Task.FromResult<IEnumerable<CustomActionClientPollResponseDataContract>>(responseList);
        }

        public async Task SubmitExecutionResults(CustomActionClientExecuteRequestDataContract request, CancellationToken cancellationToken)
        {
            var execution = _customActionExecutionRepository.GetById(request.ExecutionId)
                            ?? throw new InvalidOperationException($"Execution with ID {request.ExecutionId} not found.");

            execution.ExecutedAt = request.ExecutedAt;
            execution.CompletedAt = DateTime.UtcNow;
            execution.Status = request.Success ? "Completed" : "Failed";
            execution.ErrorMessage = request.ErrorMessage;

            if (request.Results != null)
            {
                foreach (var resultContract in request.Results)
                {
                    execution.Results.Add(new CustomActionExecutionResultBusinessEntity
                    {
                        Id = Guid.NewGuid(),
                        CustomActionExecutionId = execution.Id,
                        Name = resultContract.Name,
                        ResultType = resultContract.ResultType.ToString(),
                        TextResult = resultContract.TextResult,
                        DataGridResultJson = resultContract.DataGridResult != null
                            ? JsonConvert.SerializeObject(resultContract.DataGridResult)
                            : null
                    });
                }
            }

            _customActionExecutionRepository.Update(execution);
            _logger.LogInformation("Received execution results for action '{ActionName}', Execution ID {ExecutionId}. Success: {Success}",
                execution.CustomAction.Name, execution.Id, request.Success);

            var client = _settingClientRepository.GetClient(execution.CustomAction.SettingClientId);
             if (client != null)
             {
                 await _eventLogFactory.Create(client.Id, $"Custom action '{execution.CustomAction.Name}' execution completed. Success: {request.Success}.", GetCurrentUser(), cancellationToken);
             }
        }

        public Task<CustomActionExecutionStatusDataContract?> GetExecutionStatus(Guid executionId, CancellationToken cancellationToken)
        {
            var execution = _customActionExecutionRepository.GetById(executionId);
            if (execution == null)
            {
                _logger.LogWarning("Execution status requested for unknown Execution ID {ExecutionId}", executionId);
                return Task.FromResult<CustomActionExecutionStatusDataContract?>(null);
            }

            // Explicitly load results if they are lazy loaded (though current repo setup for execution might eager load)
            var results = execution.Results.Any() ? execution.Results : _customActionExecutionResultRepository.GetByExecutionId(executionId);

            var statusContract = new CustomActionExecutionStatusDataContract
            {
                ExecutionId = execution.Id,
                Status = execution.Status,
                RequestedAt = execution.RequestedAt,
                ExecutedAt = execution.ExecutedAt,
                CompletedAt = execution.CompletedAt,
                ErrorMessage = execution.ErrorMessage,
                Results = results.Select(r => new CustomActionResultDataContract
                {
                    Name = r.Name,
                    ResultType = Enum.TryParse<CustomActionResultTypeDataContract>(r.ResultType, out var type) ? type : CustomActionResultTypeDataContract.Text, // Default or handle error
                    TextResult = r.TextResult,
                    DataGridResult = !string.IsNullOrWhiteSpace(r.DataGridResultJson)
                        ? JsonConvert.DeserializeObject<DataGridSettingDataContract>(r.DataGridResultJson)
                        : null
                }).ToList()
            };
            return Task.FromResult<CustomActionExecutionStatusDataContract?>(statusContract);
        }

        public Task<CustomActionExecutionHistoryDataContract?> GetExecutionHistory(Guid customActionId, int limit, int offset, CancellationToken cancellationToken)
        {
            var customAction = _customActionRepository.GetById(customActionId);
            if (customAction == null)
            {
                _logger.LogWarning("Execution history requested for unknown Custom Action ID {CustomActionId}", customActionId);
                return Task.FromResult<CustomActionExecutionHistoryDataContract?>(null);
            }

            var executions = _customActionExecutionRepository.GetHistory(customActionId, limit, offset);

            var historyContract = new CustomActionExecutionHistoryDataContract
            {
                CustomActionId = customAction.Id,
                CustomActionName = customAction.Name,
                Executions = executions.Select(e => new CustomActionExecutionStatusDataContract // Simplified, might need full result details per execution
                {
                    ExecutionId = e.Id,
                    Status = e.Status,
                    RequestedAt = e.RequestedAt,
                    ExecutedAt = e.ExecutedAt,
                    CompletedAt = e.CompletedAt,
                    ErrorMessage = e.ErrorMessage,
                    // Results are not typically included in history summary to keep it light.
                    // If needed, GetExecutionStatus would be called for a specific one.
                }).ToList()
            };
            return Task.FromResult<CustomActionExecutionHistoryDataContract?>(historyContract);
        }
        
        public async Task DeleteClientCustomActions(Guid settingClientId, CancellationToken cancellationToken)
        {
            var actions = _customActionRepository.GetByClientId(settingClientId).ToList();
            if (!actions.Any())
            {
                _logger.LogInformation("No custom actions found for client ID {SettingClientId} to delete.", settingClientId);
                return;
            }

            foreach (var action in actions)
            {
                _customActionRepository.Delete(action); // NHibernate cascade should handle child execution/result deletion
                _logger.LogInformation("Deleted custom action '{ActionName}' (ID: {ActionId}) for client ID {SettingClientId}",
                    action.Name, action.Id, settingClientId);
            }
            await _eventLogFactory.Create(settingClientId, $"Deleted {actions.Count} custom actions.", GetCurrentUser(), cancellationToken);
        }
    }
}
