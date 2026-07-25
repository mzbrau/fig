using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Api.Assistant;

public sealed class AssistantBackgroundRunner : AuthenticatedService, IAssistantBackgroundRunner
{
    private readonly ILlmClient _llmClient;
    private readonly AssistantHistoryCompactor _historyCompactor;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IEncryptionService _encryptionService;

    public AssistantBackgroundRunner(
        ILlmClient llmClient,
        AssistantHistoryCompactor historyCompactor,
        IConfigurationRepository configurationRepository,
        IEncryptionService encryptionService)
    {
        _llmClient = llmClient;
        _historyCompactor = historyCompactor;
        _configurationRepository = configurationRepository;
        _encryptionService = encryptionService;
    }

    public async Task<AssistantBackgroundRunResult> RunAsync(
        string activityName,
        string systemPrompt,
        string userMessage,
        IReadOnlyCollection<IAssistantTool> tools,
        CancellationToken cancellationToken,
        double? temperature = null)
    {
        var user = RequireAuthenticatedUser();
        var configuration = await _configurationRepository.GetConfiguration();
        if (!FigAssistantAvailability.IsReady(configuration, _encryptionService))
            throw new InvalidOperationException("Fig Assistant is disabled or not fully configured.");

        var timeout = Math.Clamp(configuration.FigAssistantRequestTimeoutSeconds, 10, 600);
        var maxIterations = Math.Clamp(configuration.FigAssistantMaxToolIterations, 1, 50);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeout));
        var token = timeoutSource.Token;

        using var rootActivity = AssistantTrace.StartBackground(activityName, user.Username, maxIterations);

        var messages = new List<JObject>
        {
            new() { ["role"] = "system", ["content"] = systemPrompt },
            new() { ["role"] = "user", ["content"] = userMessage }
        };

        var executedTools = new List<AssistantBackgroundToolCall>();
        string lastAssistantText = string.Empty;
        var hasTerminalSubmitTool = tools.Any(t =>
            string.Equals(t.Name, "submit_ai_report", StringComparison.Ordinal));
        var terminalSubmitSucceeded = false;
        var invalidSubmitCount = 0;

        try
        {
            for (var iteration = 0; iteration < maxIterations; iteration++)
            {
                token.ThrowIfCancellationRequested();

                var messageCountBeforeCompact = messages.Count;
                messages = _historyCompactor.Compact(messages);
                var compacted = messages.Count != messageCountBeforeCompact;

                if (hasTerminalSubmitTool &&
                    !terminalSubmitSucceeded &&
                    maxIterations - iteration <= 2)
                {
                    messages.Add(new JObject
                    {
                        ["role"] = "system",
                        ["content"] =
                            "You must call submit_ai_report now with the best grounded document from tool results so far."
                    });
                }

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
                        tools,
                        configuration.FigAssistantModel,
                        iteration,
                        user.Username);

                    await foreach (var chunk in _llmClient.StreamChatAsync(messages, tools, token, temperature)
                                       .WithCancellation(token))
                    {
                        if (!string.IsNullOrEmpty(chunk.Text))
                            assistantText.Append(chunk.Text);

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

                    lastAssistantText = assistantText.ToString();
                    AssistantTrace.RecordLlmResponse(
                        llmActivity,
                        lastAssistantText,
                        calls.Values,
                        finishReason,
                        user.Username);
                    AssistantTrace.SetOk(llmActivity);
                }

                if (calls.Count == 0)
                {
                    if (hasTerminalSubmitTool && !terminalSubmitSucceeded)
                    {
                        messages.Add(new JObject
                        {
                            ["role"] = "assistant",
                            ["content"] = lastAssistantText
                        });
                        messages.Add(new JObject
                        {
                            ["role"] = "system",
                            ["content"] =
                                "You must call submit_ai_report now with the best grounded document from tool results so far. " +
                                "If data is sparse or incomplete, say so in markdown sections and still submit. " +
                                "Never reply in prose and never ask the user clarifying questions."
                        });
                        continue;
                    }

                    AssistantTrace.SetOk(rootActivity);
                    return new AssistantBackgroundRunResult
                    {
                        AssistantText = lastAssistantText,
                        ToolCalls = executedTools
                    };
                }

                messages.Add(BuildAssistantToolCallMessage(lastAssistantText, calls.Values));
                foreach (var call in calls.OrderBy(a => a.Key).Select(a => a.Value))
                {
                    string result;
                    using (var toolActivity = AssistantTrace.StartTool(call.Name))
                    {
                        var arguments = string.IsNullOrWhiteSpace(call.Arguments) ? "{}" : call.Arguments;
                        var tool = tools.FirstOrDefault(t =>
                            string.Equals(t.Name, call.Name, StringComparison.Ordinal));
                        if (tool is null)
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

                        AssistantTrace.RecordToolExchange(toolActivity, arguments, result, user.Username);
                    }

                    executedTools.Add(new AssistantBackgroundToolCall
                    {
                        Name = call.Name,
                        Arguments = string.IsNullOrWhiteSpace(call.Arguments) ? "{}" : call.Arguments,
                        Result = result
                    });

                    if (string.Equals(call.Name, "submit_ai_report", StringComparison.Ordinal) &&
                        result.Contains("\"ok\":true", StringComparison.Ordinal))
                    {
                        terminalSubmitSucceeded = true;
                    }

                    messages.Add(new JObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = call.Id,
                        ["content"] = result
                    });

                    if (string.Equals(call.Name, "submit_ai_report", StringComparison.Ordinal) &&
                        !terminalSubmitSucceeded &&
                        result.Contains("\"error\"", StringComparison.Ordinal))
                    {
                        invalidSubmitCount++;
                        if (invalidSubmitCount == 1)
                        {
                            messages.Add(new JObject
                            {
                                ["role"] = "system",
                                ["content"] =
                                    "submit_ai_report validation failed. Call submit_ai_report again immediately with corrected JSON " +
                                    "that matches the tool schema (title, sections with typed objects). " +
                                    "Do not call more read tools and do not reply in prose."
                            });
                        }
                    }
                }
            }

            const string limitMessage = "The assistant reached the configured tool iteration limit.";
            AssistantTrace.SetError(rootActivity, limitMessage);
            throw new InvalidOperationException(limitMessage);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AssistantTrace.SetError(rootActivity, ex.Message);
            throw;
        }
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
}
