using Fig.Contracts.CustomActions;
using Fig.Web.Services;

namespace Fig.Web.Facades
{
    public class CustomActionFacade : ICustomActionFacade
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<CustomActionFacade> _logger;

        public CustomActionFacade(IHttpService httpService, ILogger<CustomActionFacade> logger)
        {
            _httpService = httpService;
            _logger = logger;
        }

        public async Task<CustomActionExecutionResponseDataContract?> RequestExecution(string clientName, CustomActionExecutionRequestDataContract request)
        {
            try
            {
                var response = await _httpService.Put<CustomActionExecutionResponseDataContract>(
                    $"customactions/execute/{Uri.EscapeDataString(clientName)}", request);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting execution for custom action ID {ActionId} on client {ClientName}", request.CustomActionName, clientName);
                return null;
            }
        }

        public async Task<CustomActionExecutionStatusDataContract?> GetExecutionStatus(Guid executionId)
        {
            try
            {
                return await _httpService.Get<CustomActionExecutionStatusDataContract?>($"customactions/status/{executionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution status for Execution ID {ExecutionId}", executionId);
                return null;
            }
        }

        public async Task<CustomActionExecutionHistoryDataContract?> GetExecutionHistory(string clientName, string customActionId, DateTime startTime, DateTime endTime)
        {
            try
            {
                var encodedClientName = Uri.EscapeDataString(clientName);
                var encodedCustomActionId = Uri.EscapeDataString(customActionId);
                var startTimeParam = Uri.EscapeDataString(startTime.ToString("O")); // ISO 8601 format
                var endTimeParam = Uri.EscapeDataString(endTime.ToString("O")); // ISO 8601 format
                
                return await _httpService.Get<CustomActionExecutionHistoryDataContract?>($"customactions/history/{encodedClientName}/{encodedCustomActionId}?startTime={startTimeParam}&endTime={endTimeParam}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting execution history for Custom Action ID {CustomActionId} on client {ClientName}", customActionId, clientName);
                return null;
            }
        }
    }
}
