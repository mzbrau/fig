using System.Text;
using Fig.Api.Attributes;
using Fig.Api.Assistant;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Contracts.Assistant;
using Fig.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Fig.Api.Controllers;

[ApiController]
[Route("assistant")]
public class AssistantController : ControllerBase
{
    private static readonly JsonSerializerSettings StreamJsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    private readonly IAssistantChatService _assistantChatService;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IEncryptionService _encryptionService;

    public AssistantController(
        IAssistantChatService assistantChatService,
        IConfigurationRepository configurationRepository,
        IEncryptionService encryptionService)
    {
        _assistantChatService = assistantChatService;
        _configurationRepository = configurationRepository;
        _encryptionService = encryptionService;
    }

    [Authorize(Role.Administrator)]
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var configuration = await _configurationRepository.GetConfiguration();
        return Ok(new AssistantStatusDataContract { Enabled = configuration.EnableFigAssistant });
    }

    [Authorize(Role.Administrator)]
    [HttpPost("chat")]
    public async Task Chat([FromBody] AssistantChatRequestDataContract request, CancellationToken cancellationToken)
    {
        var configuration = await _configurationRepository.GetConfiguration();
        if (!configuration.EnableFigAssistant)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            await Response.WriteAsync("Fig Assistant is disabled.", cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(configuration.FigAssistantEndpoint) ||
            string.IsNullOrWhiteSpace(configuration.FigAssistantModel) ||
            string.IsNullOrWhiteSpace(configuration.FigAssistantAccessTokenEncrypted))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("Fig Assistant is not fully configured.", cancellationToken);
            return;
        }

        var token = _encryptionService.Decrypt(configuration.FigAssistantAccessTokenEncrypted, throwOnFailure: false);
        if (string.IsNullOrWhiteSpace(token) || token == configuration.FigAssistantAccessTokenEncrypted)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("Fig Assistant access token is invalid.", cancellationToken);
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            await foreach (var streamEvent in _assistantChatService.ChatAsync(request, cancellationToken))
            {
                var payload = JsonConvert.SerializeObject(streamEvent.Data, StreamJsonSettings);
                await Response.WriteAsync($"event: {streamEvent.Type}\n", cancellationToken);
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            var error = JsonConvert.SerializeObject(new { message = ex.Message }, StreamJsonSettings);
            await Response.WriteAsync($"event: {AssistantStreamEventTypes.Error}\n", CancellationToken.None);
            await Response.WriteAsync($"data: {error}\n\n", CancellationToken.None);
            await Response.Body.FlushAsync(CancellationToken.None);
        }
    }
}
