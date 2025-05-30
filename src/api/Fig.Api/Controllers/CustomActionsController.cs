using Fig.Api.Attributes;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.CustomActions;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers
{
    [ApiController]
    [Route("customactions")]
    public class CustomActionsController : ControllerBase
    {
        private readonly ICustomActionService _customActionService;
        private readonly ILogger<CustomActionsController> _logger;

        public CustomActionsController(ICustomActionService customActionService, ILogger<CustomActionsController> logger)
        {
            _customActionService = customActionService;
            _logger = logger;
        }
        
        [HttpPost("register")]
        public async Task<IActionResult> RegisterCustomActions(
            [FromHeader] string clientSecret, 
            [FromBody] CustomActionRegistrationRequestDataContract request)
        {
            await _customActionService.RegisterCustomActions(clientSecret, request);
            _logger.LogInformation("Client {ClientName} registered {CustomActionCount} custom actions", request.ClientName, request.CustomActions.Count);
            return Ok();
        }

        [Authorize(Role.User)]
        [HttpPost("execute/{clientName}")]
        public async Task<IActionResult> RequestExecution(string clientName, [FromBody] CustomActionExecutionRequestDataContract request)
        {
            var response = await _customActionService.RequestExecution(clientName, request);
            return Ok(response);
        }
        
        [HttpGet("poll/{clientName}/{runSessionId}")]
        public async Task<IActionResult> PollForExecutionRequests(
            [FromRoute]string clientName,
            [FromQuery]Guid runSessionId, 
            [FromHeader]string clientSecret)
        {
            var requests = await _customActionService.PollForExecutionRequests(clientName, runSessionId, clientSecret);
            if (!requests.Any())
                return NoContent(); // HTTP 204 if no pending actions
                
            return Ok(requests);
        }
        
        [HttpPost("results/{clientName}")]
        public async Task<IActionResult> SubmitExecutionResults(
            [FromRoute]string clientName, 
            [FromHeader]string clientSecret,
            [FromBody]CustomActionClientExecuteRequestDataContract request)
        {
            await _customActionService.SubmitExecutionResults(clientName, clientSecret, request);
            return Ok();
        }

        [Authorize(Role.User)]
        [HttpGet("status/{executionId}")]
        public async Task<IActionResult> GetExecutionStatus(Guid executionId)
        {
            var status = await _customActionService.GetExecutionStatus(executionId);
            return Ok(status);
        }

        [Authorize(Role.User)]
        [HttpGet("history/{clientName}/{customActionId}")]
        public async Task<IActionResult> GetExecutionHistory(
            [FromRoute]string clientName, 
            [FromRoute]string customActionId, 
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime)
        {
            var history = await _customActionService.GetExecutionHistory(clientName, customActionId, startTime, endTime);
            return Ok(history);
        }
    }
}
