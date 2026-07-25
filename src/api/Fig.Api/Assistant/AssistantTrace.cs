using System.Diagnostics;
using System.Text.RegularExpressions;
using Fig.Api.Observability;
using Fig.Contracts.Assistant;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Api.Assistant;

internal static class AssistantTrace
{
    public const string ChatActivityName = "Assistant.Chat";
    public const string LlmActivityName = "Assistant.Llm";
    public const string ToolActivityName = "Assistant.Tool";

    public const string RequestEventName = "assistant.request";
    public const string LlmRequestEventName = "llm.request";
    public const string LlmResponseEventName = "llm.response";
    public const string ToolArgsEventName = "tool.arguments";
    public const string ToolResultEventName = "tool.result";

    private const int MaximumAttributeCharacters = 200_000;

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented
    };

    public const string BackgroundActivityName = "Assistant.Background";

    public static Activity? StartChat(
        string username,
        AssistantChatRequestDataContract request,
        int maxIterations)
    {
        var activity = ApiActivitySource.Instance.StartActivity(ChatActivityName);
        if (activity is null)
            return null;

        activity.SetTag("fig.assistant.username", RedactUsername(username));
        activity.SetTag("fig.assistant.page", request.UiContext?.CurrentPage);
        activity.SetTag("fig.assistant.history_count", request.Messages.Count);
        activity.SetTag("fig.assistant.max_iterations", maxIterations);

        var payload = JsonConvert.SerializeObject(new
        {
            uiContext = request.UiContext,
            messages = request.Messages
        }, JsonSettings);
        AddChunkedEvent(activity, RequestEventName, "fig.assistant.request",
            RedactUsernameInText(payload, username));

        return activity;
    }

    public static Activity? StartBackground(string activityName, string username, int maxIterations)
    {
        var activity = ApiActivitySource.Instance.StartActivity(BackgroundActivityName);
        if (activity is null)
            return null;

        activity.SetTag("fig.assistant.background", activityName);
        activity.SetTag("fig.assistant.username", RedactUsername(username));
        activity.SetTag("fig.assistant.max_iterations", maxIterations);
        return activity;
    }

    public static Activity? StartLlm(int iteration, string? model, int messageCount, bool compacted)
    {
        var activity = ApiActivitySource.Instance.StartActivity(LlmActivityName);
        if (activity is null)
            return null;

        activity.SetTag("fig.assistant.iteration", iteration);
        activity.SetTag("fig.assistant.model", model);
        activity.SetTag("fig.assistant.message_count", messageCount);
        activity.SetTag("fig.assistant.compacted", compacted);
        return activity;
    }

    public static Activity? StartTool(string toolName)
    {
        var activity = ApiActivitySource.Instance.StartActivity(ToolActivityName);
        activity?.SetTag("fig.assistant.tool", toolName);
        return activity;
    }

    public static void RecordLlmRequest(
        Activity? activity,
        IReadOnlyList<JObject> messages,
        IReadOnlyCollection<IAssistantTool> tools,
        string? model,
        int iteration,
        string? username)
    {
        if (activity is null)
            return;

        var toolNames = string.Join(",", tools.Select(a => a.Name));
        var messagesJson = RedactUsernameInText(
            JsonConvert.SerializeObject(messages, JsonSettings),
            username);
        AddChunkedEvent(activity, LlmRequestEventName, "fig.assistant.messages", messagesJson, new ActivityTagsCollection
        {
            ["fig.assistant.tools"] = toolNames,
            ["fig.assistant.model"] = model ?? string.Empty,
            ["fig.assistant.iteration"] = iteration
        });

        if (iteration == 0)
        {
            var schemas = tools.Select(tool => new JObject
            {
                ["type"] = "function",
                ["function"] = new JObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["parameters"] = SafeParseSchema(tool.ParameterJsonSchema)
                }
            });
            var schemasJson = JsonConvert.SerializeObject(schemas, JsonSettings);
            AddChunkedEvent(activity, "llm.tools", "fig.assistant.tool_schemas", schemasJson);
        }
    }

    public static void RecordLlmResponse(
        Activity? activity,
        string assistantText,
        IEnumerable<AccumulatedToolCall> toolCalls,
        string finishReason,
        string? username)
    {
        if (activity is null)
            return;

        var calls = toolCalls.ToList();
        activity.SetTag("fig.assistant.finish_reason", finishReason);
        activity.SetTag("fig.assistant.tool_call_count", calls.Count);
        activity.SetTag("fig.assistant.response_chars", assistantText.Length);

        var responseJson = RedactUsernameInText(
            JsonConvert.SerializeObject(new
            {
                content = assistantText,
                finishReason,
                toolCalls = calls.Select(a => new { a.Id, a.Name, a.Arguments })
            }, JsonSettings),
            username);
        AddChunkedEvent(activity, LlmResponseEventName, "fig.assistant.response", responseJson);
    }

    public static void RecordToolExchange(
        Activity? activity,
        string arguments,
        string result,
        string? username)
    {
        if (activity is null)
            return;

        AddChunkedEvent(activity, ToolArgsEventName, "fig.assistant.tool_arguments",
            RedactUsernameInText(arguments, username));
        AddChunkedEvent(activity, ToolResultEventName, "fig.assistant.tool_result",
            RedactUsernameInText(result, username));
    }

    public static void SetOk(Activity? activity) =>
        activity?.SetStatus(ActivityStatusCode.Ok);

    public static void SetError(Activity? activity, string message)
    {
        if (activity is null)
            return;

        activity.SetStatus(ActivityStatusCode.Error, message);
        activity.SetTag("error.message", message);
    }

    public static void TagLlmHttp(string? model, string? endpoint)
    {
        var activity = Activity.Current;
        if (activity is null)
            return;

        activity.SetTag("fig.assistant.model", model);
        if (!string.IsNullOrWhiteSpace(endpoint) &&
            Uri.TryCreate(endpoint.TrimEnd('/') + "/chat/completions", UriKind.Absolute, out var uri))
        {
            activity.SetTag("fig.assistant.llm_host", uri.Host);
            activity.SetTag("fig.assistant.llm_path", uri.AbsolutePath);
        }
    }

    internal static string RedactUsername(string? username)
    {
        if (string.IsNullOrEmpty(username))
            return string.Empty;

        if (username.Length <= 2)
            return username;

        return username[0] + new string('*', username.Length - 2) + username[^1];
    }

    internal static string RedactUsernameInText(string text, string? username)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(text))
            return text;

        return Regex.Replace(
            text,
            $@"\b{Regex.Escape(username)}\b",
            RedactUsername(username));
    }

    private static void AddChunkedEvent(
        Activity activity,
        string eventName,
        string payloadKey,
        string payload,
        ActivityTagsCollection? extraTags = null)
    {
        if (payload.Length <= MaximumAttributeCharacters)
        {
            var tags = extraTags ?? new ActivityTagsCollection();
            tags[payloadKey] = payload;
            activity.AddEvent(new ActivityEvent(eventName, tags: tags));
            return;
        }

        var partCount = (payload.Length + MaximumAttributeCharacters - 1) / MaximumAttributeCharacters;
        for (var part = 0; part < partCount; part++)
        {
            var start = part * MaximumAttributeCharacters;
            var length = Math.Min(MaximumAttributeCharacters, payload.Length - start);
            var tags = new ActivityTagsCollection
            {
                [$"{payloadKey}.part"] = part,
                [$"{payloadKey}.parts"] = partCount,
                [payloadKey] = payload.Substring(start, length)
            };
            if (extraTags is not null && part == 0)
            {
                foreach (var tag in extraTags)
                    tags[tag.Key] = tag.Value;
            }

            activity.AddEvent(new ActivityEvent($"{eventName}.part.{part}", tags: tags));
        }
    }

    private static JToken SafeParseSchema(string schema)
    {
        try
        {
            return JObject.Parse(schema);
        }
        catch (JsonException)
        {
            return new JObject { ["type"] = "object" };
        }
    }
}
