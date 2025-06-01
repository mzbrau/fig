using Fig.Api.Datalayer.Repositories;
using Fig.Api.Enums;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Api.Validators;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.CustomActions;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Services
{
    public class CustomActionService : AuthenticatedService, ICustomActionService
    {
        private readonly string _instanceId = Guid.NewGuid().ToString();
        private readonly ICustomActionRepository _customActionRepository;
        private readonly ICustomActionExecutionRepository _customActionExecutionRepository;
        private readonly ISettingClientRepository _settingClientRepository;
        private readonly IEventLogFactory _eventLogFactory;
        private readonly ILogger<CustomActionService> _logger;

        public CustomActionService(
            ICustomActionRepository customActionRepository,
            ICustomActionExecutionRepository customActionExecutionRepository,
            ISettingClientRepository settingClientRepository,
            IEventLogFactory eventLogFactory,
            ILogger<CustomActionService> logger)
        {
            _customActionRepository = customActionRepository;
            _customActionExecutionRepository = customActionExecutionRepository;
            _settingClientRepository = settingClientRepository;
            _eventLogFactory = eventLogFactory;
            _logger = logger;
        }

        public async Task RegisterCustomActions(string clientSecret, CustomActionRegistrationRequestDataContract request)
        {
            try
            {
                var client = await ValidateClient(request.ClientName, clientSecret);
            
                // Remove existing custom actions for this client that are not in the new request
                var existingActions = (await _customActionRepository.GetByClientName(request.ClientName)).ToList();
                var actionsToRemove = existingActions.Where(ea => request.CustomActions.All(a => a.Name != ea.Name)).ToList();
                foreach (var actionToRemove in actionsToRemove)
                {
                    await _customActionRepository.DeleteCustomAction(actionToRemove);
                    _logger.LogInformation("Removed outdated custom action '{ActionName}' for client {ClientName}",
                        actionToRemove.Name, request.ClientName);
                }

                foreach (var actionContract in request.CustomActions)
                {
                    var existingAction = existingActions.FirstOrDefault(a => a.Name == actionContract.Name && a.ClientName == client.Name);
                    if (existingAction != null)
                    {
                        // Update existing action
                        existingAction.ButtonName = actionContract.ButtonName;
                        existingAction.Description = actionContract.Description;
                        existingAction.SettingsUsed = actionContract.SettingsUsed;
                        await _customActionRepository.UpdateCustomAction(existingAction);
                    }
                    else
                    {
                        var newAction = new CustomActionBusinessEntity
                        {
                            Name = actionContract.Name,
                            ButtonName = actionContract.ButtonName,
                            Description = actionContract.Description,
                            SettingsUsed = actionContract.SettingsUsed,
                            ClientName = client.Name,
                            ClientReference = client.Id
                        };
                        await _customActionRepository.AddCustomAction(newAction);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            
            // TODO: Event logs
            //await _eventLogFactory.Create(client.Id, $"Registered/Updated {request.CustomActions.Count} custom actions.", GetCurrentUser(), cancellationToken);
        }

        public async Task<CustomActionExecutionResponseDataContract> RequestExecution(string clientName, CustomActionExecutionRequestDataContract request)
        {
            // TODO: Validate they have access to the client.
            
            var customAction = await _customActionRepository.GetByName(clientName, request.CustomActionName)
                               ?? throw new InvalidOperationException($"Custom Action {request.CustomActionName} did not exist for client {clientName}");

            var execution = new CustomActionExecutionBusinessEntity
            {
                ClientName = clientName,
                CustomActionName = customAction.Name,
                RunSessionId = request.RunSessionId,
                RequestedAt = DateTime.UtcNow,
            };

            await _customActionExecutionRepository.AddExecutionRequest(execution);
            _logger.LogInformation("Requested execution for custom action '{ActionName}' for client {ClientName}",
                execution.CustomActionName, execution.ClientName);
            
            // TODO: Event log

            return new CustomActionExecutionResponseDataContract(execution.Id, "Execution requested successfully.");
        }

        public async Task<IEnumerable<CustomActionPollResponseDataContract>> PollForExecutionRequests(string clientName, Guid runSessionId, string clientSecret)
        {
            var pendingExecutions = (await _customActionExecutionRepository.GetAllPending(clientName)).ToList();
            if (!pendingExecutions.Any())
            {
                return [];
            }

            // Getting a client is expensive so we don't do it unless there are pending executions.
            await ValidateClient(clientName, clientSecret);

            var actionsToExecute = new List<CustomActionPollResponseDataContract>();
            foreach (var execution in pendingExecutions)
            {
                if (execution.RunSessionId == runSessionId || execution.RunSessionId is null)
                {
                    actionsToExecute.Add(new CustomActionPollResponseDataContract(execution.Id, execution.CustomActionName));
                    execution.HandlingInstance = _instanceId;
                    await _customActionExecutionRepository.UpdateExecution(execution);
                }
            }

            return actionsToExecute;
        }

        public async Task SubmitExecutionResults(string clientName, string clientSecret, CustomActionExecutionResultsDataContract result)
        {
            var execution = await _customActionExecutionRepository.GetById(result.ExecutionId)
                            ?? throw new InvalidOperationException($"Execution with ID {result.ExecutionId} not found.");

            await ValidateClient(clientName, clientSecret);
            
            execution.ResultsAsJson = JsonConvert.SerializeObject(result.Results, JsonSettings.FigDefault);
            execution.ExecutedAt = DateTime.UtcNow;
            execution.Succeeded = result.Success;
            execution.ExecutedByRunSessionId = result.RunSessionId;

            await _customActionExecutionRepository.UpdateExecution(execution);
            
            // TODO: Event log
        }

        public async Task<CustomActionExecutionStatusDataContract?> GetExecutionStatus(Guid executionId)
        {
            var execution = await _customActionExecutionRepository.GetById(executionId);
            if (execution == null)
            {
                _logger.LogWarning("Execution status requested for unknown Execution ID {ExecutionId}", executionId);
                throw new ActionExecutionNotFoundException();
            }

            var status = execution.GetStatus();
            List<CustomActionResultDataContract>? results = null;
            if (status == ExecutionStatus.Completed)
            {
                results = JsonConvert.DeserializeObject<List<CustomActionResultDataContract>>(execution.ResultsAsJson, JsonSettings.FigDefault);
            }

            var statusContract = new CustomActionExecutionStatusDataContract(executionId,
                status,
                execution.RequestedAt,
                execution.ExecutedAt,
                results,
                execution.Succeeded,
                execution.ExecutedByRunSessionId);

            return statusContract;
        }

        public async Task<CustomActionExecutionHistoryDataContract?> GetExecutionHistory(string clientName, string customActionName, DateTime startTime, DateTime endTime)
        {
            var executions = await _customActionExecutionRepository.GetHistory(clientName, customActionName, startTime, endTime);

            var results = executions.Select(e =>
            {
                var results = JsonConvert.DeserializeObject<List<CustomActionResultDataContract>>(e.ResultsAsJson, JsonSettings.FigDefault);
                return new CustomActionExecutionStatusDataContract(
                    e.Id,
                    e.GetStatus(),
                    e.RequestedAt,
                    e.ExecutedAt,
                    results,
                    e.Succeeded,
                    e.ExecutedByRunSessionId);
            }).ToList();
            
            var result = new CustomActionExecutionHistoryDataContract(clientName, customActionName, results);

            return result;
        }

        private async Task<SettingClientBusinessEntity> ValidateClient(string clientName, string clientSecret)
        {
            var client = await _settingClientRepository.GetClient(clientName)
                         ?? throw new UnknownClientException(clientName);

            var registrationStatus = RegistrationStatusValidator.GetStatus(client, clientSecret);
            if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
                throw new UnauthorizedAccessException("Invalid Secret");

            return client;
        }
    }
}
