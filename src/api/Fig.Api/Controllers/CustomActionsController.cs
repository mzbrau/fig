using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fig.Api.Attributes; // For AuthorizeAttribute
using Fig.Api.Services;
using Fig.Contracts; // For Role
using Fig.Contracts.CustomActions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Fig.Api.Controllers
{
    [ApiController]
    [Route("api/customactions")]
    public class CustomActionsController : ControllerBase
    {
        private readonly ICustomActionService _customActionService;
        private readonly ILogger<CustomActionsController> _logger;

        public CustomActionsController(ICustomActionService customActionService, ILogger<CustomActionsController> logger)
        {
            _customActionService = customActionService;
            _logger = logger;
        }

        [Authorize(Role.Client)]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterCustomActions([FromBody] CustomActionRegistrationRequestDataContract request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _customActionService.RegisterCustomActions(request, cancellationToken);
                _logger.LogInformation("Client {ClientName} registered custom actions.", request.ClientName);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering custom actions for client {ClientName}.", request.ClientName);
                return StatusCode(500, "An internal server error occurred while registering custom actions.");
            }
        }

        [Authorize(Role.User)]
        [HttpGet("{clientName}/{instance?}")]
        public async Task<IActionResult> GetCustomActions(string clientName, string? instance, CancellationToken cancellationToken)
        {
            try
            {
                var actions = await _customActionService.GetCustomActions(clientName, instance, cancellationToken);
                return Ok(actions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving custom actions for client {ClientName} ({Instance}).", clientName, instance ?? "N/A");
                return StatusCode(500, "An internal server error occurred.");
            }
        }
        
        [Authorize(Role.User)]
        [HttpPost("execute/{clientName}")] // clientName here is for context/logging, actual client derived from CustomActionId in service
        public async Task<IActionResult> RequestExecution(string clientName, [FromBody] CustomActionExecutionRequestDataContract request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("User requested execution for custom action ID {CustomActionId} for client context {ClientName}.", request.CustomActionId, clientName);
                var response = await _customActionService.RequestExecution(clientName, request, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting execution for custom action ID {CustomActionId}.", request.CustomActionId);
                return StatusCode(500, "An internal server error occurred while requesting execution.");
            }
        }

        [Authorize(Role.Client)]
        [HttpGet("poll/{clientName}/{instance?}")]
        public async Task<IActionResult> PollForExecutionRequests(string clientName, string? instance, CancellationToken cancellationToken)
        {
            try
            {
                var requests = await _customActionService.PollForExecutionRequests(clientName, instance, cancellationToken);
                if (!requests.Any())
                    return NoContent(); // HTTP 204 if no pending actions
                
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling for execution requests for client {ClientName} ({Instance}).", clientName, instance ?? "N/A");
                return StatusCode(500, "An internal server error occurred while polling for requests.");
            }
        }

        [Authorize(Role.Client)]
        [HttpPost("results")]
        public async Task<IActionResult> SubmitExecutionResults([FromBody] CustomActionClientExecuteRequestDataContract request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _customActionService.SubmitExecutionResults(request, cancellationToken);
                _logger.LogInformation("Client submitted execution results for Execution ID {ExecutionId}.", request.ExecutionId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting execution results for Execution ID {ExecutionId}.", request.ExecutionId);
                return StatusCode(500, "An internal server error occurred while submitting results.");
            }
        }

        [Authorize(Role.User)]
        [HttpGet("status/{executionId}")]
        public async Task<IActionResult> GetExecutionStatus(Guid executionId, CancellationToken cancellationToken)
        {
            try
            {
                var status = await _customActionService.GetExecutionStatus(executionId, cancellationToken);
                if (status == null)
                {
                    _logger.LogInformation("Execution status requested for unknown Execution ID {ExecutionId}.", executionId);
                    return NotFound($"No execution found with ID {executionId}.");
                }
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving execution status for Execution ID {ExecutionId}.", executionId);
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [Authorize(Role.User)]
        [HttpGet("history/{customActionId}")]
        public async Task<IActionResult> GetExecutionHistory(Guid customActionId, [FromQuery] int limit = 20, [FromQuery] int offset = 0, CancellationToken cancellationToken)
        {
            try
            {
                var history = await _customActionService.GetExecutionHistory(customActionId, limit, offset, cancellationToken);
                if (history == null)
                {
                     _logger.LogInformation("Execution history requested for unknown Custom Action ID {CustomActionId}.", customActionId);
                    return NotFound($"No custom action found with ID {customActionId} to retrieve history.");
                }
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving execution history for Custom Action ID {CustomActionId}.", customActionId);
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}
