using Fig.Api.Services;
using Fig.Contracts.Assistant;
using Newtonsoft.Json.Linq;

namespace Fig.Api.Assistant;

public interface IAssistantChatService : IAuthenticatedService
{
    IAsyncEnumerable<AssistantStreamEventDataContract> ChatAsync(
        AssistantChatRequestDataContract request,
        CancellationToken cancellationToken);
}

public interface IAssistantTool
{
    string Name { get; }
    string Description { get; }
    string ParameterJsonSchema { get; }
    Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken);
}

public interface IAssistantToolRegistry
{
    IReadOnlyCollection<IAssistantTool> Tools { get; }

    IReadOnlyCollection<IAssistantTool> GetReadOnlyTools();

    bool TryGet(string name, out IAssistantTool? tool);
}

public interface ILlmClient
{
    IAsyncEnumerable<LlmStreamChunk> StreamChatAsync(
        IReadOnlyList<JObject> messages,
        IReadOnlyCollection<IAssistantTool> tools,
        CancellationToken cancellationToken,
        double? temperature = null);
}

public interface IAssistantBackgroundRunner : IAuthenticatedService
{
    Task<AssistantBackgroundRunResult> RunAsync(
        string activityName,
        string systemPrompt,
        string userMessage,
        IReadOnlyCollection<IAssistantTool> tools,
        CancellationToken cancellationToken,
        double? temperature = null);
}

public sealed class AssistantBackgroundRunResult
{
    public required string AssistantText { get; init; }

    public required IReadOnlyList<AssistantBackgroundToolCall> ToolCalls { get; init; }
}

public sealed class AssistantBackgroundToolCall
{
    public required string Name { get; init; }

    public required string Arguments { get; init; }

    public required string Result { get; init; }
}

public sealed class LlmStreamChunk
{
    public string? Text { get; set; }
    public string? FinishReason { get; set; }
    public int? ToolCallIndex { get; set; }
    public string? ToolCallId { get; set; }
    public string? ToolName { get; set; }
    public string? ToolArguments { get; set; }
}

internal sealed class AccumulatedToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
}
