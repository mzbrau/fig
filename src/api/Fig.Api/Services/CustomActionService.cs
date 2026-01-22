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
        private readonly IEventLogRepository _eventLogRepository;
        private readonly ILogger<CustomActionService> _logger;

        public CustomActionService(
            ICustomActionRepository customActionRepository,
            ICustomActionExecutionRepository customActionExecutionRepository,
            ISettingClientRepository settingClientRepository,
            IEventLogFactory eventLogFactory,
            IEventLogRepository eventLogRepository,
            ILogger<CustomActionService> logger)
        {
            _customActionRepository = customActionRepository;
            _customActionExecutionRepository = customActionExecutionRepository;
            _settingClientRepository = settingClientRepository;
            _eventLogFactory = eventLogFactory;
            _eventLogRepository = eventLogRepository;
            _logger = logger;
        }

        public async Task RegisterCustomActions(string clientSecret, CustomActionRegistrationRequestDataContract request)
        {
            var client = await GetAndValidateClient(request.ClientName, clientSecret);
            
            // Remove existing custom actions for this client that are not in the new request
            var existingActions = (await _customActionRepository.GetByClientName(request.ClientName)).ToList();
            var actionsToRemove = existingActions.Where(ea => request.CustomActions.All(a => a.Name != ea.Name)).ToList();
                
            // Remove actions from the client's collection to let cascade handle deletion
            foreach (var actionToRemove in actionsToRemove)
            {
                client.CustomActions.Remove(actionToRemove);
                _logger.LogInformation("Removed outdated custom action '{ActionName}' for client {ClientName}",
                    actionToRemove.Name.Sanitize(), request.ClientName.Sanitize());
            }

            if (actionsToRemove.Any())
            {
                await _eventLogRepository.Add(
                    _eventLogFactory.CustomActionsRemoved(client.Name, actionsToRemove.Select(a => a.Name)));
            }

            foreach (var actionContract in request.CustomActions)
            {
                var existingAction = existingActions.FirstOrDefault(a => a.Name == actionContract.Name && a.ClientName == client.Name);
                if (existingAction != null)
                {
                    var wasUpdated = existingAction.Update(actionContract);
                    if (wasUpdated)
                    {
                        await _eventLogRepository.Add(
                            _eventLogFactory.CustomActionUpdated(client.Name, existingAction.Name));
                    }
                }
                else
                {
                    var newAction = new CustomActionBusinessEntity
                    {
                        Name = actionContract.Name,
                        ClientName = client.Name,
                        ClientReference = client.Id
                    };
                    newAction.Update(actionContract);
                    client.CustomActions.Add(newAction);
                    await _eventLogRepository.Add(
                        _eventLogFactory.CustomActionAdded(client.Name, newAction.Name));
                }
            }
                
            // Update the client to persist all changes through cascade
            await _settingClientRepository.UpdateClient(client);
        }

        public async Task<CustomActionExecutionResponseDataContract> RequestExecution(string clientName, CustomActionExecutionRequestDataContract request)
        {
            ThrowIfNoAccess(clientName);

            var customAction = await _customActionRepository.GetByName(clientName, request.CustomActionName);

            if (customAction is null)
            {
                return new CustomActionExecutionResponseDataContract(Guid.Empty, $"Custom Action {request.CustomActionName} did not exist for client {clientName}", false);
            }

            var execution = new CustomActionExecutionBusinessEntity
            {
                ClientName = clientName,
                CustomActionName = customAction.Name,
                RunSessionId = request.RunSessionId,
                RequestedAt = DateTime.UtcNow,
            };

            await _customActionExecutionRepository.AddExecutionRequest(execution);
            _logger.LogInformation("Requested execution for custom action '{ActionName}' for client {ClientName}",
                execution.CustomActionName.Sanitize(), execution.ClientName.Sanitize());

            await _eventLogRepository.Add(_eventLogFactory.CustomActionExecutionRequested(clientName, customAction.Name,
                AuthenticatedUser, request.RunSessionId));

            return new CustomActionExecutionResponseDataContract(execution.Id, "Execution requested successfully.", true);
        }

        public async Task<IEnumerable<CustomActionPollResponseDataContract>> PollForExecutionRequests(string clientName, Guid runSessionId, string clientSecret)
        {
            var pendingExecutions = (await _customActionExecutionRepository.GetAllPending(clientName)).ToList();
            if (!pendingExecutions.Any())
            {
                return [];
            }

            // Getting a client is expensive so we don't do it unless there are pending executions.
            // Use read-only validation since we only need to verify credentials
            await ValidateClientReadOnly(clientName, clientSecret);

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

            // Use read-only validation since we only need to verify credentials
            await ValidateClientReadOnly(clientName, clientSecret);
            
            execution.ResultsAsJson = JsonConvert.SerializeObject(result.Results, JsonSettings.FigDefault);
            execution.ExecutedAt = DateTime.UtcNow;
            execution.Succeeded = result.Success;
            execution.ExecutedByRunSessionId = result.RunSessionId;

            await _customActionExecutionRepository.UpdateExecution(execution);

            await _eventLogRepository.Add(_eventLogFactory.CustomActionExecutionCompleted(clientName,
                execution.CustomActionName, execution.Succeeded));
        }

        public async Task<CustomActionExecutionStatusDataContract?> GetExecutionStatus(Guid executionId)
        {
            var execution = await _customActionExecutionRepository.GetById(executionId);
            if (execution == null)
            {
                _logger.LogWarning("Execution status requested for unknown Execution ID {ExecutionId}", executionId);
                throw new ActionExecutionNotFoundException();
            }
            
            ThrowIfNoAccess(execution.ClientName);

            var status = execution.GetStatus();
            List<CustomActionResultDataContract>? results = null;
            if (status == ExecutionStatus.Completed && execution.ResultsAsJson is not null)
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
            // Ensure DateTime parameters have UTC kind for NHibernate UtcTicks type
            var startTimeUtc = startTime.Kind == DateTimeKind.Utc ? startTime : DateTime.SpecifyKind(startTime, DateTimeKind.Utc);
            var endTimeUtc = endTime.Kind == DateTimeKind.Utc ? endTime : DateTime.SpecifyKind(endTime, DateTimeKind.Utc);
            
            var executions = await _customActionExecutionRepository.GetHistory(clientName, customActionName, startTimeUtc, endTimeUtc);

            var results = executions.Select(e =>
            {
                List<CustomActionResultDataContract>? results = [];
                if (e.ResultsAsJson is not null)
                    results = JsonConvert.DeserializeObject<List<CustomActionResultDataContract>>(e.ResultsAsJson, JsonSettings.FigDefault);
                
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

        private async Task<SettingClientBusinessEntity> GetAndValidateClient(string clientName, string clientSecret)
        {
            var client = await _settingClientRepository.GetClient(clientName)
                         ?? throw new UnknownClientException(clientName);

            var registrationStatus = RegistrationStatusValidator.GetStatus(client, clientSecret);
            if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
                throw new UnauthorizedAccessException("Invalid Secret");

            return client;
        }
        
        /// <summary>
        /// Validates client credentials without acquiring database locks.
        /// Use this when you only need to verify the client exists and has valid credentials.
        /// </summary>
        private async Task ValidateClientReadOnly(string clientName, string clientSecret)
        {
            var client = await _settingClientRepository.GetClientReadOnly(clientName)
                         ?? throw new UnknownClientException(clientName);

            var registrationStatus = RegistrationStatusValidator.GetStatus(client, clientSecret);
            if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
                throw new UnauthorizedAccessException("Invalid Secret");
        }
    }
}
