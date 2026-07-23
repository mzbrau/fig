using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Contracts.Assistant;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Api.Assistant;

public sealed class AssistantHistoryCompactor
{
    private const int MaximumCharacters = 48_000;

    public List<JObject> Compact(IEnumerable<JObject> source)
    {
        var messages = source.ToList();
        var total = messages.Sum(a => a.ToString(Formatting.None).Length);
        if (total <= MaximumCharacters)
            return messages;

        var system = messages.FirstOrDefault(a => a["role"]?.Value<string>() == "system");
        var retained = new List<JObject>();
        var retainedCharacters = 0;
        for (var index = messages.Count - 1; index >= 0; index--)
        {
            var message = messages[index];
            if (ReferenceEquals(message, system))
                continue;

            var size = message.ToString(Formatting.None).Length;
            if (retainedCharacters + size > MaximumCharacters - 4_000)
                break;
            retained.Insert(0, message);
            retainedCharacters += size;
        }

        var result = new List<JObject>();
        if (system is not null)
            result.Add(system);
        result.Add(new JObject
        {
            ["role"] = "system",
            ["content"] = "Earlier conversation messages were omitted to stay within the model context window."
        });
        result.AddRange(retained);
        return result;
    }
}

public sealed class AssistantChatService : AuthenticatedService, IAssistantChatService
{
    private readonly ILlmClient _llmClient;
    private readonly IAssistantToolRegistry _toolRegistry;
    private readonly AssistantHistoryCompactor _historyCompactor;
    private readonly IConfigurationRepository _configurationRepository;

    public AssistantChatService(
        ILlmClient llmClient,
        IAssistantToolRegistry toolRegistry,
        AssistantHistoryCompactor historyCompactor,
        IConfigurationRepository configurationRepository)
    {
        _llmClient = llmClient;
        _toolRegistry = toolRegistry;
        _historyCompactor = historyCompactor;
        _configurationRepository = configurationRepository;
    }

    public async IAsyncEnumerable<AssistantStreamEventDataContract> ChatAsync(
        AssistantChatRequestDataContract request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var user = RequireAuthenticatedUser();
        var configuration = await _configurationRepository.GetConfiguration();
        var timeout = Math.Clamp(configuration.FigAssistantRequestTimeoutSeconds, 10, 600);
        var maxIterations = Math.Clamp(configuration.FigAssistantMaxToolIterations, 1, 50);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeout));
        var token = timeoutSource.Token;

        using var chatActivity = AssistantTrace.StartChat(user.Username, request, maxIterations);

        var messages = new List<JObject>
        {
            new()
            {
                ["role"] = "system",
                ["content"] = BuildSystemPrompt(user.Username, request.UiContext)
            }
        };
        messages.AddRange(request.Messages
            .Where(a => a.Role is "user" or "assistant")
            .Where(a => !string.IsNullOrWhiteSpace(a.Content))
            .Select(a => new JObject { ["role"] = a.Role, ["content"] = a.Content }));

        yield return new AssistantStreamEventDataContract(
            AssistantStreamEventTypes.Progress,
            new { id = "understand", label = "Understanding request...", status = "running" });

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            token.ThrowIfCancellationRequested();
            if (iteration == 0)
            {
                yield return new AssistantStreamEventDataContract(
                    AssistantStreamEventTypes.Progress,
                    new { id = "understand", label = "Understanding request...", status = "done" });
            }

            var messageCountBeforeCompact = messages.Count;
            messages = _historyCompactor.Compact(messages);
            var compacted = messages.Count != messageCountBeforeCompact;
            var calls = new Dictionary<int, AccumulatedToolCall>();
            var assistantText = new System.Text.StringBuilder();
            var finishReason = "stop";

            using (var llmActivity = AssistantTrace.StartLlm(
                       iteration,
                       configuration.FigAssistantModel,
                       messages.Count,
                       compacted))
            {
                AssistantTrace.RecordLlmRequest(
                    llmActivity,
                    messages,
                    _toolRegistry.Tools,
                    configuration.FigAssistantModel,
                    iteration);

                // try/finally only — yield return is illegal inside try/catch.
                var llmCompleted = false;
                try
                {
                    await foreach (var chunk in _llmClient.StreamChatAsync(messages, _toolRegistry.Tools, token)
                                       .WithCancellation(token))
                    {
                        if (!string.IsNullOrEmpty(chunk.Text))
                        {
                            assistantText.Append(chunk.Text);
                            yield return new AssistantStreamEventDataContract(
                                AssistantStreamEventTypes.Token,
                                new { text = chunk.Text });
                        }

                        if (chunk.ToolCallIndex is int callIndex)
                        {
                            if (!calls.TryGetValue(callIndex, out var call))
                            {
                                call = new AccumulatedToolCall();
                                calls.Add(callIndex, call);
                            }

                            if (!string.IsNullOrEmpty(chunk.ToolCallId))
                                call.Id = chunk.ToolCallId;
                            if (!string.IsNullOrEmpty(chunk.ToolName))
                                call.Name += chunk.ToolName;
                            if (!string.IsNullOrEmpty(chunk.ToolArguments))
                                call.Arguments += chunk.ToolArguments;
                        }

                        if (!string.IsNullOrWhiteSpace(chunk.FinishReason))
                            finishReason = chunk.FinishReason;
                    }

                    AssistantTrace.RecordLlmResponse(
                        llmActivity,
                        assistantText.ToString(),
                        calls.Values,
                        finishReason);
                    AssistantTrace.SetOk(llmActivity);
                    llmCompleted = true;
                }
                finally
                {
                    if (!llmCompleted)
                    {
                        AssistantTrace.SetError(llmActivity, "LLM iteration failed or was cancelled.");
                        AssistantTrace.SetError(chatActivity, "LLM iteration failed or was cancelled.");
                    }
                }
            }

            if (calls.Count == 0)
            {
                AssistantTrace.SetOk(chatActivity);
                yield return new AssistantStreamEventDataContract(
                    AssistantStreamEventTypes.Done,
                    new { finishReason });
                yield break;
            }

            messages.Add(BuildAssistantToolCallMessage(assistantText.ToString(), calls.Values));
            foreach (var call in calls.OrderBy(a => a.Key).Select(a => a.Value))
            {
                var progressId = string.IsNullOrWhiteSpace(call.Id) ? Guid.NewGuid().ToString("N") : call.Id;
                yield return new AssistantStreamEventDataContract(
                    AssistantStreamEventTypes.Progress,
                    new { id = progressId, label = FriendlyName(call.Name), status = "running" });

                string result;
                using (var toolActivity = AssistantTrace.StartTool(call.Name))
                {
                    var arguments = string.IsNullOrWhiteSpace(call.Arguments) ? "{}" : call.Arguments;
                    if (!_toolRegistry.TryGet(call.Name, out var tool) || tool is null)
                    {
                        result = JsonConvert.SerializeObject(new { error = $"Unknown tool '{call.Name}'." });
                        AssistantTrace.SetError(toolActivity, $"Unknown tool '{call.Name}'.");
                    }
                    else
                    {
                        try
                        {
                            result = await tool.ExecuteAsync(arguments, token);
                            AssistantTrace.SetOk(toolActivity);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            result = JsonConvert.SerializeObject(new { error = ex.Message });
                            AssistantTrace.SetError(toolActivity, ex.Message);
                        }
                    }

                    AssistantTrace.RecordToolExchange(toolActivity, arguments, result);
                }

                if (call.Name == "propose_web_actions")
                {
                    foreach (var action in ParseActions(result))
                        yield return new AssistantStreamEventDataContract(AssistantStreamEventTypes.Action, action);
                }

                messages.Add(new JObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = call.Id,
                    ["content"] = result
                });
                yield return new AssistantStreamEventDataContract(
                    AssistantStreamEventTypes.Progress,
                    new { id = progressId, label = FriendlyName(call.Name), status = "done" });
            }
        }

        const string limitMessage = "The assistant reached the configured tool iteration limit.";
        AssistantTrace.SetError(chatActivity, limitMessage);
        yield return new AssistantStreamEventDataContract(
            AssistantStreamEventTypes.Error,
            new { message = limitMessage });
    }

    private static JObject BuildAssistantToolCallMessage(
        string content,
        IEnumerable<AccumulatedToolCall> calls)
    {
        return new JObject
        {
            ["role"] = "assistant",
            ["content"] = string.IsNullOrEmpty(content) ? null : content,
            ["tool_calls"] = new JArray(calls.Select(a => new JObject
            {
                ["id"] = a.Id,
                ["type"] = "function",
                ["function"] = new JObject { ["name"] = a.Name, ["arguments"] = a.Arguments }
            }))
        };
    }

    private static IEnumerable<AssistantProposedActionDataContract> ParseActions(string json)
    {
        try
        {
            var token = JToken.Parse(json);
            var actions = token.Type == JTokenType.Array ? token : token["actions"];
            return actions?.ToObject<List<AssistantProposedActionDataContract>>() ??
                   new List<AssistantProposedActionDataContract>();
        }
        catch (JsonException)
        {
            return Array.Empty<AssistantProposedActionDataContract>();
        }
    }

    private static string BuildSystemPrompt(string username, AssistantUiContextDataContract? context)
    {
        var contextJson = JsonConvert.SerializeObject(context, Formatting.None, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore
        });
        return $"""
                You are Fig Assistant, an administrator-facing assistant for the Fig configuration management system.
                Use read tools to verify facts before answering. Never claim to have changed Fig data.
                The only permitted write-like operation is propose_web_actions, which creates reviewable UI proposals.
                After propose_web_actions for updateSetting, createGroup, createLookupTable, or createInstance, say you proposed an unsaved draft the user must review and Save. Never say the change was created, saved, or persisted on the server.
                Never reveal secrets, credentials, tokens, passwords, or secret setting values.
                Keep answers concise and identify clients and instances precisely.
                When the user refers to a setting with approximate naming (extra spaces, different casing, or minor typos), match it to the closest registered setting from tool results.
                Always state the exact matched setting name in your reply. If several settings are equally plausible, ask a clarifying question instead of guessing.
                SelectedClientName and SelectedInstance in the UI context are the user's current selection; prefer that client unless the user names a different one.
                For data-grid updateSetting values, use exact column names from the setting definition. List<string> / single-column grids use Values (or a plain string array). Never invent keys or emit $type.
                Prefer the current UI client for createInstance and highlightSetting unless the user names a different one.
                When discussing or updating a specific setting, include highlightSetting so the UI scrolls to and briefly highlights it.
                Use searchSettings when the user asks to find settings by criteria; searchQuery supports the same prefixes as the UI search.
                get_api_status returns running Fig.Api server instances; get_run_sessions returns connected Fig client applications.
                For reports: call list_reports first, then propose generateReport with reportId and parameters.
                Infer ClientName/Instance from UI context or the conversation when possible.
                When a report has From/To and the user did not specify dates, default From to UTC today minus 7 days and To to UTC now.
                Ask the user for required parameters that cannot be inferred before proposing generateReport.
                Authenticated user: {username}
                Current UI context: {contextJson}
                """;
    }

    private static string FriendlyName(string toolName) =>
        toolName.Replace('_', ' ');
}
