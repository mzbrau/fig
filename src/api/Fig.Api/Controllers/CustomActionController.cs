using Fig.Api.Attributes;
using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Contracts.Authentication;
using Fig.Contracts.CustomActions;
using Microsoft.AspNetCore.Mvc;

namespace Fig.Api.Controllers
{
    [ApiController]
    [Route("customactions")]
    public class CustomActionController : ControllerBase
    {
        private readonly ICustomActionService _customActionService;
        private readonly ILogger<CustomActionController> _logger;

        public CustomActionController(ICustomActionService customActionService, ILogger<CustomActionController> logger)
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
            _logger.LogInformation("Client {ClientName} registered {CustomActionCount} custom actions", request.ClientName.Sanitize(), request.CustomActions.Count);
            return Ok();
        }

        [HttpGet("poll/{clientName}")]
        public async Task<IActionResult> PollForExecutionRequests(
            [FromRoute]string clientName,
            [FromQuery]Guid runSessionId, 
            [FromHeader]string clientSecret)
        {
            var requests = await _customActionService.PollForExecutionRequests(clientName, runSessionId, clientSecret);
            return Ok(requests);
        }
        
        [HttpPost("results/{clientName}")]
        public async Task<IActionResult> SubmitExecutionResults(
            [FromRoute]string clientName, 
            [FromHeader]string clientSecret,
            [FromBody]CustomActionExecutionResultsDataContract result)
        {
            await _customActionService.SubmitExecutionResults(clientName, clientSecret, result);
            return Ok();
        }
        
        [Authorize(Role.User, Role.Administrator)]
        [HttpPut("execute/{clientName}")]
        public async Task<IActionResult> RequestExecution(
            [FromRoute]string clientName, 
            [FromBody]CustomActionExecutionRequestDataContract request)
        {
            var response = await _customActionService.RequestExecution(clientName, request);
            return Ok(response);
        }

        [Authorize(Role.User, Role.Administrator)]
        [HttpGet("status/{executionId}")]
        public async Task<IActionResult> GetExecutionStatus(Guid executionId)
        {
            var status = await _customActionService.GetExecutionStatus(executionId);
            return Ok(status);
        }

        [Authorize(Role.User, Role.Administrator)]
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
