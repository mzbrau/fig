using System.Net.Http.Headers;
using System.Text;
using Fig.Common.Events;
using Fig.Common.NetStandard.Constants;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Assistant;
using Fig.Web.Events;
using Fig.Web.Models.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Web.Services.Assistant;

public sealed class AssistantChatClient : IAssistantChatClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorageService;
    private readonly IAssistantContextService _contextService;
    private readonly IAssistantActionApplier _actionApplier;
    private readonly IEventDistributor _eventDistributor;
    private readonly List<AssistantChatMessageDataContract> _messages = [];

    public AssistantChatClient(
        IHttpClientFactory httpClientFactory,
        ILocalStorageService localStorageService,
        IAssistantContextService contextService,
        IAssistantActionApplier actionApplier,
        IEventDistributor eventDistributor)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.FigApi);
        _localStorageService = localStorageService;
        _contextService = contextService;
        _actionApplier = actionApplier;
        _eventDistributor = eventDistributor;
        _eventDistributor.Subscribe(EventConstants.LogoutEvent, Clear);
    }

    public IReadOnlyList<AssistantChatMessageDataContract> Messages => _messages;

    public event Action? Changed;

    public async Task<AssistantStatusDataContract> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "assistant/status");
        await AddJwtHeader(request);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new AssistantStatusDataContract();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonConvert.DeserializeObject<AssistantStatusDataContract>(json, JsonSettings.FigDefault)
               ?? new AssistantStatusDataContract();
    }

    public async Task SendAsync(
        string message,
        AssistantStreamCallbacks callbacks,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _messages.Add(new AssistantChatMessageDataContract { Role = "user", Content = message.Trim() });
        Changed?.Invoke();

        var requestContract = new AssistantChatRequestDataContract
        {
            Messages = _messages.ToList(),
            UiContext = _contextService.BuildContext()
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, "assistant/chat")
        {
            Content = new StringContent(
                JsonConvert.SerializeObject(requestContract, JsonSettings.FigDefault),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.SetBrowserResponseStreamingEnabled(true);
        await AddJwtHeader(request);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var assistantText = new StringBuilder();
        var actions = new List<AssistantProposedActionDataContract>();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var eventName = string.Empty;
        var data = new StringBuilder();

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.Length == 0)
            {
                await DispatchAsync(eventName, data.ToString(), assistantText, actions, callbacks, cancellationToken);
                eventName = string.Empty;
                data.Clear();
                continue;
            }

            if (line.StartsWith("event:", StringComparison.Ordinal))
                eventName = line[6..].Trim();
            else if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                if (data.Length > 0)
                    data.Append('\n');
                data.Append(line[5..].TrimStart());
            }
        }

        if (data.Length > 0)
            await DispatchAsync(eventName, data.ToString(), assistantText, actions, callbacks, cancellationToken);
    }

    private async Task DispatchAsync(
        string eventName,
        string json,
        StringBuilder assistantText,
        List<AssistantProposedActionDataContract> actions,
        AssistantStreamCallbacks callbacks,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        var data = JToken.Parse(json);
        switch (eventName)
        {
            case AssistantStreamEventTypes.Progress:
                var progress = new AssistantProgressUpdate(
                    data["id"]?.Value<string>() ?? Guid.NewGuid().ToString("N"),
                    data["label"]?.Value<string>() ?? "Working",
                    data["status"]?.Value<string>() ?? "running");
                if (callbacks.Progress is not null)
                    await callbacks.Progress(progress);
                break;

            case AssistantStreamEventTypes.Token:
                var text = data["text"]?.Value<string>() ?? string.Empty;
                assistantText.Append(text);
                if (callbacks.Token is not null)
                    await callbacks.Token(text);
                break;

            case AssistantStreamEventTypes.Action:
                var action = data.ToObject<AssistantProposedActionDataContract>();
                if (action is not null)
                {
                    actions.Add(action);
                    if (callbacks.Action is not null)
                        await callbacks.Action(action);
                }
                break;

            case AssistantStreamEventTypes.Done:
                if (assistantText.Length > 0)
                {
                    _messages.Add(new AssistantChatMessageDataContract
                    {
                        Role = "assistant",
                        Content = assistantText.ToString()
                    });
                    Changed?.Invoke();
                }
                await _actionApplier.ApplyAsync(actions, cancellationToken);
                if (callbacks.Done is not null)
                    await callbacks.Done();
                break;

            case AssistantStreamEventTypes.Error:
                var error = data["message"]?.Value<string>() ?? data.ToString(Formatting.None);
                if (callbacks.Error is not null)
                    await callbacks.Error(error);
                break;
        }
    }

    private async Task AddJwtHeader(HttpRequestMessage request)
    {
        var user = await _localStorageService.GetItem<AuthenticatedUserModel>("user");
        if (user?.Token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
    }

    public void Clear()
    {
        _messages.Clear();
        Changed?.Invoke();
    }

    public void Dispose()
    {
        _eventDistributor.Unsubscribe(EventConstants.LogoutEvent, Clear);
    }
}
