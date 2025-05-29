using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.CustomActions;
using Fig.Contracts.Status; // For ClientRunSessionDataContract
using Fig.Web.Services; // For IHttpService
using Microsoft.Extensions.Logging;

namespace Fig.Web.Facades
{
    public class CustomActionsFacade : ICustomActionsFacade
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<CustomActionsFacade> _logger;

        public CustomActionsFacade(IHttpService httpService, ILogger<CustomActionsFacade> logger)
        {
            _httpService = httpService;
            _logger = logger;
        }

        public async Task<List<CustomActionDefinitionDataContract>> GetCustomActions(string clientName, string? instance, CancellationToken cancellationToken)
        {
            try
            {
                var encodedClientName = Uri.EscapeDataString(clientName);
                var uri = $"api/customactions/{encodedClientName}";
                if (!string.IsNullOrWhiteSpace(instance))
                {
                    uri += $"/{Uri.EscapeDataString(instance)}";
                }

                var actions = await _httpService.Get<List<CustomActionDefinitionDataContract>>(uri, cancellationToken);
                return actions ?? new List<CustomActionDefinitionDataContract>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting custom actions for client {ClientName} ({Instance})", clientName, instance ?? "N/A");
                return new List<CustomActionDefinitionDataContract>(); // Return empty list on error
            }
        }

        public async Task<CustomActionExecutionResponseDataContract> RequestExecution(string clientName, CustomActionExecutionRequestDataContract request, CancellationToken cancellationToken)
        {
            try
            {
                var encodedClientName = Uri.EscapeDataString(clientName);
                var response = await _httpService.Post<CustomActionExecutionRequestDataContract, CustomActionExecutionResponseDataContract>(
                    $"api/customactions/execute/{encodedClientName}", request, cancellationToken);
                
                // Ensure response is not null, though Post should ideally throw or return a structured error.
                // For robustness, if Post can return null on http error handled by HttpService:
                if (response == null)
                {
                    _logger.LogError("RequestExecution returned null for client {ClientName}, Action ID {ActionId}", clientName, request.CustomActionId);
                    // Depending on IHttpService behavior, this might be an exceptional case.
                    // Returning a default error response or throwing a specific exception might be better.
                    return new CustomActionExecutionResponseDataContract { ExecutionId = Guid.Empty, Message = "Execution request failed." };
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting execution for custom action ID {ActionId} on client {ClientName}", request.CustomActionId, clientName);
                return new CustomActionExecutionResponseDataContract { ExecutionId = Guid.Empty, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<CustomActionExecutionStatusDataContract?> GetExecutionStatus(Guid executionId, CancellationToken cancellationToken)
        {
            try
            {
                return await _httpService.Get<CustomActionExecutionStatusDataContract?>($"api/customactions/status/{executionId}", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution status for Execution ID {ExecutionId}", executionId);
                return null;
            }
        }

        public async Task<CustomActionExecutionHistoryDataContract?> GetExecutionHistory(Guid customActionId, int limit, int offset, CancellationToken cancellationToken)
        {
            try
            {
                return await _httpService.Get<CustomActionExecutionHistoryDataContract?>($"api/customactions/history/{customActionId}?limit={limit}&offset={offset}", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution history for Custom Action ID {CustomActionId}", customActionId);
                return null;
            }
        }
        
        public async Task<List<ClientRunSessionDataContract>> GetRunSessions(string clientName, CancellationToken cancellationToken)
        {
            // This method is specified to use an existing facade or implement here.
            // Assuming IClientStatusFacade is not injected for this specific task structure,
            // and direct implementation is requested if not.
            // The API endpoint for run sessions is typically under /api/clients/ or /api/status/.
            // Using /api/clients/{clientName}/runsessions as a placeholder, adjust if different.
            try
            {
                var encodedClientName = Uri.EscapeDataString(clientName);
                var sessions = await _httpService.Get<List<ClientRunSessionDataContract>>($"api/clients/{encodedClientName}/runsessions", cancellationToken);
                return sessions ?? new List<ClientRunSessionDataContract>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting run sessions for client {ClientName}", clientName);
                return new List<ClientRunSessionDataContract>();
            }
        }
    }
}
